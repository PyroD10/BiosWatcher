using BiosWatcher.Services.Sources;

namespace BiosWatcher.Services;

/// <summary>Holds all IBiosSource implementations; "Add board" loops CanHandle over the pasted URL.</summary>
public class VendorRegistry(IEnumerable<IBiosSource> sources)
{
    private readonly IReadOnlyList<IBiosSource> _sources = sources.ToList();

    public IReadOnlyList<IBiosSource> Sources => _sources;

    public IBiosSource? FindSource(Uri supportUrl) =>
        _sources.FirstOrDefault(s => s.CanHandle(supportUrl));

    public IBiosSource? FindByVendorId(string vendorId) =>
        _sources.FirstOrDefault(s => s.VendorId == vendorId);
}
