using SyncBrowser.Core.Models;

namespace SyncBrowser.Core.Interfaces;

/// <summary>
/// Repository pattern: abstracts CRUD operations for browser profiles.
/// </summary>
public interface IProfileRepository
{
    Task<IReadOnlyList<BrowserProfile>> GetAllAsync();
    Task<BrowserProfile?> GetByIdAsync(string id);
    Task SaveAsync(BrowserProfile profile);
    Task DeleteAsync(string id);
    Task SaveAllAsync(IEnumerable<BrowserProfile> profiles);
}
