namespace LMS.Borrowing.Storage;

public class StorageAggregateNotFoundException : Exception
{
    private StorageAggregateNotFoundException(string typeName, string id): base($"{typeName} with id '{id}' was not found.")
    {
    }

    public static StorageAggregateNotFoundException For<T>(Guid id) => For<T>(id.ToString());

    public static StorageAggregateNotFoundException For<T>(string id) => new (typeof(T).Name, id);
}