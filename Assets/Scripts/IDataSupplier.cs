using System.Collections.Generic;

namespace Assets.Scripts
{
    public interface IDataSupplier
    {
        List<string> GetData();
    }
}