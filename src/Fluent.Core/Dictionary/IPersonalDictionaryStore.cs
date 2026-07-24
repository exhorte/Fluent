namespace Fluent.Core.Dictionary;

public interface IPersonalDictionaryStore
{
    Task<IReadOnlyList<PersonalDictionaryStorageEntry>> InitializeAndLoadAsync(
        CancellationToken cancellationToken);

    Task UpsertAsync(
        PersonalDictionaryStorageEntry entry,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        string spokenForm,
        CancellationToken cancellationToken);
}
