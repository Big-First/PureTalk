namespace Data;

public class InicializaBD
{
    public InicializaBD() { }
    public void InitializeMongo(DBContext _data)
        => _data.GetOrCreateDatabase();
}