using System.Data;

namespace PetShop.Api.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
