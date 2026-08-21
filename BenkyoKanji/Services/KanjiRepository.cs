using BenkyoKanji.Models;

namespace BenkyoKanji.Services;

public interface IKanjiRepository
{
    Task InitializeAsync();
    IReadOnlyList<KanjiItem> GetAll();
    KanjiItem? GetById(string id);
    Task AddOrUpdateAsync(KanjiItem item);
    Task DeleteAsync(string id);
    IReadOnlyList<KanjiItem> Search(string query, JlptLevel level = JlptLevel.All, string? tag = null);
    IReadOnlyList<KanjiItem> GetByLevel(JlptLevel level);
}

public class KanjiRepository : IKanjiRepository
{
    private readonly IJsonStorageService _storageService;
    private readonly List<KanjiItem> _items = [];
    private bool _initialized;

    public KanjiRepository(IJsonStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        var loaded = await _storageService.LoadKanjiLibraryAsync();
        _items.Clear();
        _items.AddRange(loaded);
        _initialized = true;
    }

    public IReadOnlyList<KanjiItem> GetAll() => _items.AsReadOnly();

    public KanjiItem? GetById(string id) => _items.FirstOrDefault(k => k.Id == id);

    public async Task AddOrUpdateAsync(KanjiItem item)
    {
        var existingIdx = _items.FindIndex(k => k.Id == item.Id);
        if (existingIdx >= 0)
        {
            _items[existingIdx] = item;
        }
        else
        {
            _items.Add(item);
        }

        await _storageService.SaveKanjiLibraryAsync(_items);
    }

    public async Task DeleteAsync(string id)
    {
        _items.RemoveAll(k => k.Id == id);
        await _storageService.SaveKanjiLibraryAsync(_items);
    }

    public IReadOnlyList<KanjiItem> Search(string query, JlptLevel level = JlptLevel.All, string? tag = null)
    {
        var result = _items.AsEnumerable();

        if (level != JlptLevel.All)
        {
            result = result.Where(k => k.Level == level);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            result = result.Where(k => k.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLowerInvariant();
            result = result.Where(k =>
                k.Kanji.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                k.Onyomi.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                k.Kunyomi.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                k.MeaningKo.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                k.MeaningEn.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                k.Examples.Any(e => 
                    e.Word.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.Reading.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.Meaning.Contains(q, StringComparison.OrdinalIgnoreCase))
            );
        }

        return result.ToList();
    }

    public IReadOnlyList<KanjiItem> GetByLevel(JlptLevel level)
    {
        return level == JlptLevel.All 
            ? _items.AsReadOnly() 
            : _items.Where(k => k.Level == level).ToList();
    }
}
