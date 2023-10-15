using TFB.Models;

namespace TFB.Services;

public interface IPersonService
{
    Task<IEnumerable<Person>> GetAllPeople();
    Task<Person?> GetPersonByUsername(string username);
    Task<Person?> CreateOrUpdate(Person person);
}