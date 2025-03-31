using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ChatBotAPI.Core;

public class ChatBotService
{
    // --- CAMPOS ---
    private readonly InferenceSession _session;
    private const int MaxSequenceLength = 128;
    private readonly bool _usesAttentionMask;
    private const string PythonExe = "python3";
    private const string TokenizeScript = "tokenizer.py";
    // Cache KV não será usado explicitamente nesta versão
    // private List<NamedOnnxValue> _pastKeyValues = null;
    // private int _currentCacheSequenceLength = 0;

    private const int BatchSize = 1;
    private const int NumHeads = 12;
    private const int EmbeddingSizePerHead = 64;
    private const int VocabSize = 50257;
    private const int NumLayers = 12;

    // --- CONSTRUTOR e METADATA ---
    public ChatBotService()
    {
        var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "ONN", "decoder_model_merged.onnx");
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Modelo ONNX não encontrado: {modelPath}");
        _session = new InferenceSession(modelPath);
        _usesAttentionMask = _session.InputMetadata.ContainsKey("attention_mask");
        Console.WriteLine("ChatBotService iniciado!");
        PrintMetadata();
    }

    private void PrintMetadata()
    {
        Console.WriteLine("--- Input Metadata ---");
        var inputMetadata = _session.InputMetadata;
        foreach (var item in inputMetadata.OrderBy(kvp => kvp.Key))
        {
             string shapeStr = item.Value.Dimensions != null ? string.Join(",", item.Value.Dimensions.Select(d => d.ToString())) : "N/A";
             Console.WriteLine($"Input Name: {item.Key,-25} | Type: {item.Value.OnnxValueType,-18} | ElementType: {item.Value.ElementDataType,-10} | Shape: [{shapeStr}]");
        }
        Console.WriteLine("--- Output Metadata ---");
        var outputMetadata = _session.OutputMetadata;
        foreach (var item in outputMetadata.OrderBy(kvp => kvp.Key))
        {
             string shapeStr = item.Value.Dimensions != null ? string.Join(",", item.Value.Dimensions.Select(d => d.ToString())) : "N/A";
             Console.WriteLine($"Output Name: {item.Key,-25} | Type: {item.Value.OnnxValueType,-18} | ElementType: {item.Value.ElementDataType,-10} | Shape: [{shapeStr}]");
        }
        Console.WriteLine("----------------------");
    }

    // --- ENCODE/DECODE/PYTHON --- (Sem alterações)
    private List<int> Encode(string message)
    {
        string escapedMessage = message.Replace("\"", "\\\"");
        string result = ExecutePythonScript(TokenizeScript, $"encode \"{escapedMessage}\"");
        if (string.IsNullOrEmpty(result)) { Console.WriteLine("Erro: Encode retornou vazio."); return new List<int>(); }
        try { return result.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList(); }
        catch (Exception ex) { Console.WriteLine($"Erro parse Encode: '{result}'. Err: {ex.Message}"); return new List<int>(); }
    }
    private string Decode(List<int> tokenIds)
    {
        if (tokenIds == null || tokenIds.Count == 0) return "";
        string idsArgument = string.Join(" ", tokenIds.Select(i => i.ToString()));
        string result = ExecutePythonScript(TokenizeScript, $"decode {idsArgument}");
        return result ?? "";
    }
    private string ExecutePythonScript(string scriptName, string arguments)
    {
        string scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Vocabularys", scriptName);
        if (!File.Exists(scriptPath)) { Console.WriteLine($"Erro: Script Python não encontrado: '{scriptPath}'"); return null; }
        Console.WriteLine($"Executando Python: '{PythonExe}' com Args: '{scriptPath} {arguments}'");
        ProcessStartInfo start = new ProcessStartInfo { FileName = PythonExe, Arguments = $"{scriptPath} {arguments}", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        try
        {
            using (Process process = Process.Start(start)!)
            {
                string output = process.StandardOutput.ReadToEnd(); string errors = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0 || !string.IsNullOrEmpty(errors)) { Console.WriteLine($"Erro script Python (Exit={process.ExitCode}): {errors}"); return null; }
                return output.Trim();
            }
        } catch (Exception ex) { Console.WriteLine($"Falha executar Python: {ex}"); return null; }
    }

    // --- CREATETENSOR/NORMALIZE --- (Sem alterações)
    private static DenseTensor<T> CreateTensor<T>(Array data, ReadOnlySpan<int> dimensions) where T : struct
    {
        T[] typedData;
        if (data is T[] alreadyTyped) { typedData = alreadyTyped; }
        else
        {
            typedData = new T[data.Length];
            try { Buffer.BlockCopy(data, 0, typedData, 0, Buffer.ByteLength(data)); }
            catch (Exception ex) { Console.WriteLine($"Erro conv. tensor tipo {typeof(T)}: {ex.Message}"); throw; }
        }
        try { return new DenseTensor<T>(typedData, dimensions); }
        catch (Exception ex) { Console.WriteLine($"Erro criar DenseTensor<{typeof(T)}> shape [{string.Join(",", dimensions.ToArray())}] data len {typedData.Length}: {ex}"); throw; }
    }
    private static List<long> NormalizeInput(List<long> tokens, int targetLength)
    {
        if (tokens.Count == targetLength) return tokens;
        if (tokens.Count > targetLength) return tokens.Take(targetLength).ToList();
        tokens.AddRange(Enumerable.Repeat(0L, targetLength - tokens.Count));
        return tokens;
    }

    // --- CACHE METHODS (Não usados nesta versão, podem ser removidos) ---
    // private List<NamedOnnxValue> InitializePastKeyValues() { ... }
    // private List<NamedOnnxValue> UpdatePastKeyValues(...) { ... }


    // --- GETRESPONSE (Com Cache Externo Desabilitado, mas use_cache_branch=false) ---
    public string GetResponse(string message, int maxNewTokens = 10)
    {
        List<int> inputTokens = Encode(message);
        if (inputTokens == null || inputTokens.Count == 0) return "Não foi possível processar a mensagem.";

        List<int> generatedTokenIds = new List<int>();
        List<long> currentSequenceForInput = inputTokens.Select(x => (long)x).ToList();

        for (int step = 0; step < maxNewTokens; step++)
        {
            Console.WriteLine($"\n--- Geração Passo {step + 1} ---");

            int currentRealLength = currentSequenceForInput.Count;
            Console.WriteLine($"Comprimento real da sequência para esta etapa: {currentRealLength}");

            var paddedTokens = NormalizeInput(new List<long>(currentSequenceForInput), MaxSequenceLength);
            long[] inputIdsArray = paddedTokens.ToArray();
            var inputTensorShape = new int[] { BatchSize, MaxSequenceLength };
            var inputTensor = CreateTensor<long>(inputIdsArray, inputTensorShape);

            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input_ids", inputTensor) };

            if (_usesAttentionMask)
            {
                var attentionMaskValues = Enumerable.Repeat(1L, currentRealLength)
                                                    .Concat(Enumerable.Repeat(0L, MaxSequenceLength - currentRealLength))
                                                    .ToArray();
                // Console.WriteLine($"Attention Mask (Passo {step+1}): Comprimento Real={currentRealLength}, Exemplo=[{string.Join(",", attentionMaskValues.Take(currentRealLength + 2).TakeLast(5))}]");
                var attentionMaskTensor = CreateTensor<long>(attentionMaskValues, inputTensorShape);
                inputs.Add(NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor));
            }

            // --- CORREÇÃO PRINCIPAL ---
            // Adiciona use_cache_branch, mas define como FALSE
            if (_session.InputMetadata.ContainsKey("use_cache_branch"))
            {
               // Define como FALSE para indicar que NÃO estamos fornecendo past_key_values
               var useCacheBranchTensor = CreateTensor<bool>(new bool[] { false }, new int[] { 1 });
               inputs.Add(NamedOnnxValue.CreateFromTensor("use_cache_branch", useCacheBranchTensor));
               Console.WriteLine("Adicionado 'use_cache_branch' = false");
            }
             else {
                 Console.WriteLine("Aviso: Modelo não tem input 'use_cache_branch'.");
                 // Se o modelo REALMENTE não tiver, talvez não precise se preocupar.
                 // Mas o erro anterior sugere que ele TEM sim.
             }
            // --- FIM DA CORREÇÃO ---

            // Cache KV externo NÃO é adicionado
            Console.WriteLine($"Número total de inputs para _session.Run: {inputs.Count}");

            IReadOnlyCollection<NamedOnnxValue> results = null;
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                results = _session.Run(inputs);
                sw.Stop();
                Console.WriteLine($"Inferência ONNX concluída em {sw.ElapsedMilliseconds} ms");
                if (results == null) { Console.WriteLine("Erro: results nulo!"); break; }
            }
            catch (Exception ex) { sw.Stop(); Console.WriteLine($"Erro inferência ONNX ({sw.ElapsedMilliseconds} ms): {ex}"); break; }

            // Processa logits (como antes)
            var logitsTensor = results.FirstOrDefault(r => r.Name == "logits")?.Value as DenseTensor<float>;
            if (logitsTensor == null) { Console.WriteLine("Output 'logits' não encontrado."); break; }
            var logitsDims = logitsTensor.Dimensions.ToArray();
            if (logitsDims.Length != 3 || logitsDims[0]!= BatchSize || logitsDims[1]!= MaxSequenceLength || logitsDims[2]!= VocabSize) { Console.WriteLine($"Erro: Dimensões inesperadas logits: [{string.Join(",", logitsDims)}]."); break; }
            int seqLenOutput = logitsDims[1]; int vocabSize = logitsDims[2];

            int lastRelevantLogitIndex = currentRealLength - 1;
            if (lastRelevantLogitIndex < 0) { lastRelevantLogitIndex = 0; }
            if (lastRelevantLogitIndex >= seqLenOutput) { lastRelevantLogitIndex = seqLenOutput - 1; }
            // Console.WriteLine($"Índice Último Token Relevante (Passo {step + 1}): {lastRelevantLogitIndex}");
            int startIdx = lastRelevantLogitIndex * vocabSize;

            if (startIdx < 0 || vocabSize <= 0 || startIdx + vocabSize > logitsTensor.Length) { Console.WriteLine($"Erro: Índice logits inválido (Passo {step + 1})."); break; }

            ReadOnlySpan<float> lastTokenLogitsSpan = logitsTensor.Buffer.Span.Slice(startIdx, vocabSize);
            int nextTokenId = -1; float maxValue = float.NegativeInfinity;
            for (int k = 0; k < lastTokenLogitsSpan.Length; k++) { if (lastTokenLogitsSpan[k] > maxValue) { maxValue = lastTokenLogitsSpan[k]; nextTokenId = k; } }

            if (nextTokenId == -1) { Console.WriteLine("Erro: Token máximo não encontrado."); break; }

            string decodedToken = Decode(new List<int> { nextTokenId });
            Console.WriteLine($"Token Previsto (Passo {step + 1}): ID={nextTokenId}, Logit={maxValue:F4}, Decoded='{decodedToken?.Replace("\n", "\\n").Replace("\r", "\\r")}'");

            if (nextTokenId == 50256) { Console.WriteLine("Token EOS gerado."); break; }

            generatedTokenIds.Add(nextTokenId);
            currentSequenceForInput.Add(nextTokenId);

            // Cache KV externo NÃO é atualizado

            if (currentSequenceForInput.Count >= MaxSequenceLength) { Console.WriteLine("Aviso: Comprimento máximo atingido."); break; }
        } // Fim loop

        Console.WriteLine($"\nDecodificando {generatedTokenIds.Count} tokens gerados...");
        string finalResponse = Decode(generatedTokenIds);
        string displayResponse = finalResponse?.Length > 100 ? finalResponse.Substring(0, 100) + "..." : finalResponse;
        Console.WriteLine($"Retornando resposta final: '{displayResponse?.Replace("\n", "\\n").Replace("\r", "\\r")}'");
        return finalResponse ?? "";
    }
}