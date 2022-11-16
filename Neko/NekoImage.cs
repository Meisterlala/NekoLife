using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiScene;
using Neko.Drawing;


namespace Neko;

public enum ImageStatus
{
    Successfull, Faulty, HasData, Unknown
}

/// <summary>
/// Handles loading an Image from a data[] to GPU.
/// <para>
/// The reason, that this class is so complicated is that calling <see cref="Dalamud.Interface.UiBuilder.LoadImageAsync"/>
/// sometimes causes a <see cref="AccessViolationException"/> when the Garbage Collector is collecting during the loading
/// of the Image. This is 'fixed' by pausing the GC before loading, and then restarting it afterwards. This will still
/// cause a <see cref="AccessViolationException"/> when the Ephemeral memory is to small.
/// </para>
/// </summary>
public class NekoImage
{

    private enum Architecture // Support for multi CPU computers
    {
        Workstation32Bit, Workstation64Bit, Server32Bit, Server64Bit
    }

    // Realistically only Workstation64Bit and Server64Bit will ever be used, since FFXIV is 64Bit only
    // see https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/workstation-server-gc
    // and https://mattwarren.org/2016/08/16/Preventing-dotNET-Garbage-Collections-with-the-TryStartNoGCRegion-API/
    private static long EphemeralMemorySizeTable(Architecture arch)
    {
        return arch switch
        {
            Architecture.Workstation32Bit => 1000 * 1000 * 15,  //  15 MB
            Architecture.Workstation64Bit => 1000 * 1000 * 243, // 243 MB
            Architecture.Server32Bit => 1000 * 1000 * 255,      // 255 MB
            Architecture.Server64Bit => 1000 * 1000 * 1000,     //   1 GB
            _ => throw new ArgumentOutOfRangeException(nameof(arch)),
        };
    }

    // see https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals#ephemeral-generations-and-segments
    private static long? _ephemeralMemorySize;
    private static long EphemeralMemorySize
    {
        get
        {
            if (_ephemeralMemorySize != null) return (long)_ephemeralMemorySize;
            // Get GC type. see https://docs.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector
            var gc_type = Environment.GetEnvironmentVariable("DOTNET_gcServer") ??
                          Environment.GetEnvironmentVariable("COMPlus_gcServer") ?? "0";
            var arch = Architecture.Server32Bit;
            if (Environment.Is64BitProcess && gc_type == "0")
                arch = Architecture.Workstation64Bit;
            else if (!Environment.Is64BitProcess && gc_type == "0")
                arch = Architecture.Workstation32Bit;
            else if (Environment.Is64BitProcess)
                arch = Architecture.Server64Bit;
            _ephemeralMemorySize = EphemeralMemorySizeTable(arch);
            PluginLog.LogDebug("Detectec GC architecture: " + arch);
            return (long)_ephemeralMemorySize;
        }
    }

    private byte[] _data;
    private TextureWrap? _texture;
    private static int inNoGCRegion;   // 1 = in NoGCRegon

    public ImageStatus ImageStatus { get; private set; }

    public TextureWrap Texture => _texture ?? throw new Exception("await LoadImage() before accessing the texture");

    public string? URLDownloadWebsite { get; }
    public string? URLOpenOnClick { get; set; }
    public string? Description { get; set; }
    public string? DebugInfo { get; set; }

    public Type? Creator { get; set; }

    public long RAMUsage => (_data?.Length) ?? 0;
    public long VRAMUsage => _texture != null ? _texture.Height * _texture.Width * 4 : 0;

    public NekoImage(byte[] data, string url)
    {
        _data = data;
        URLDownloadWebsite = url;
        URLOpenOnClick = url;
        ImageStatus = ImageStatus.HasData;
    }

    public NekoImage(byte[] data)
    {
        _data = data;
        ImageStatus = ImageStatus.HasData;
    }

    public NekoImage()
    {
        _data = Array.Empty<byte>();
        ImageStatus = ImageStatus.Faulty;
    }

    ~NekoImage()
    {
        Dispose();
    }

    public void Dispose()
    {
#if DEBUG
        if (_texture != null || (_data?.Length > 0))
            PluginLog.LogVerbose("Disposing Image  " + ToString());
#endif
        _texture?.Dispose();
        _texture = null;

        _data = Array.Empty<byte>();
    }

    public override string ToString()
    {
        // Check if Embedded Resource
        if (this == Embedded.ImageError.Image)
            return Embedded.ImageError.ToString();
        else if (this == Embedded.ImageLoading.Image)
            return Embedded.ImageLoading.ToString();

        var name = "";
        if (_data != null)
            name += $"Data: {Helper.SizeSuffix(_data.Length)}\t";
        if (_texture != null)
            name += $"Texture: {Helper.SizeSuffix(_texture.Height * _texture.Width * 4)}\t";
        if (URLDownloadWebsite != null)
            name += $"URL: {URLDownloadWebsite}";
        if (DebugInfo != null)
            name += $"\n└─DebugInfo: {DebugInfo}";

        return name.Length == 0 ? "Invalid Texture" : name;
    }

    /// <summary>
    /// Load image from RAM to GPU VRAM
    /// </summar>
    public async Task<TextureWrap> LoadImage()
    {
        if (_texture != null) // If already loaded
            return _texture;

        if (_data == Array.Empty<byte>() || _data.Length == 0)
        {
            ImageStatus = ImageStatus.Faulty;
            throw new Exception("No Image data provided");
        }

        // Pause GC
        try
        {
            // try correct size
            if (Interlocked.Exchange(ref inNoGCRegion, 1) == 0)
            {
                GC.TryStartNoGCRegion(EphemeralMemorySize, true);
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            // This will catch, if:
            // - EphemeralMemorySize is to big: Fallback to 32bit GC size
            // - Already in NoGCRegion
            PluginLog.Log("Fallback to smaller GC bufffer " + Helper.SizeSuffix(EphemeralMemorySizeTable(Architecture.Workstation32Bit), 3));
            GC.TryStartNoGCRegion(EphemeralMemorySizeTable(Architecture.Workstation32Bit), true);
        }
        catch (InvalidOperationException)
        {
            // This will catch, if already in NoGCRegion
            PluginLog.Debug("Already in NoGCRegion");
        }

        try
        {
            // Load Image to GPU
            // This will cause a System.AccessViolationException if a GC colletion happens
            // _texture = await Plugin.PluginInterface.UiBuilder.LoadImageAsync(_data);
            
            // Quick and dirty refactor
            // TODO: Refactor class
            var img = new Drawing.NekoImage();
            img.LoadData(_data);
            img.Decode();
            img.LoadGPU();
            //_texture = img.Frames![0].Texture;
            _texture = Plugin.PluginInterface.UiBuilder.LoadImageRaw(img.Frames![0].Data, img.Width.Value, img.Height.Value, 4);

            if (_texture == null) // This should never happen
                throw new Exception("Image null");
        }
        catch (Exception ex)
        {
            // This will catch, if:
            // - the GC is collected while the image is loading
            // - there is not enough ram avalible
            // - there is not enough vram avalible
            // - the NoGCRegion totalSize was too small and a GC collection happend
            // - the image data is corrupt
            ImageStatus = ImageStatus.Faulty;
            throw new Exception("Could not load image", ex);
        }

        // Restart GC
        try
        {
            if (Interlocked.Exchange(ref inNoGCRegion, 0) == 1)
            {
                GC.EndNoGCRegion();
            }
        }
        catch (Exception ex)
        {
            // This will catch, if:
            // - NoGCRegion totalSize was too small
            // - NoGCRegion was never started
            PluginLog.LogDebug(ex, "Ephemeral memory to small to load image");
        }

        PluginLog.Verbose("Decompressed {0} to {1} and loaded into GPU VRAM",
                        _data != null ? Helper.SizeSuffix(_data.Length) : "???",
                        Helper.SizeSuffix(_texture.Width * _texture.Height * 4));

#if !DEBUG
        // we can delete the original image data to clear up ram, 
        // since the uncompressed image is now stored in vram
        _data = Array.Empty<Byte>();
#endif

        ImageStatus = ImageStatus.Successfull;
        return _texture;
    }

    public class Embedded
    {
        public static readonly Embedded ImageError = new("error.jpg");
        public static readonly Embedded ImageLoading = new("loading.jpg");

        public volatile NekoImage? Image;
        public TextureWrap Texture => Image?.Texture ?? throw new Exception("Load Embedded Image before accessing the texture");
        public bool Ready { get; private set; }
        private readonly string Filename;

        public Embedded(string filename) => Filename = filename;

        public async Task<NekoImage> Load()
        {
            // Only load texture if it was never loaded. 
            if (Image != null) return Image;

            // Load embedded error icon
            if (Ready)
                PluginLog.LogWarning("Reloading exsisting embedded image");
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Neko.resources.{Filename}";

            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var memoryStream = new MemoryStream();
                stream?.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                var img = new NekoImage(bytes);
                await img.LoadImage();
                Image = img;
            }
            catch (Exception)
            {
                PluginLog.LogFatal("Could not load embedded image: {0}", resourceName);
                throw;
            }

            PluginLog.LogVerbose("Loaded embedded image: {0}", resourceName);
            Ready = true;
            return Image;
        }

        public override string ToString() => $"Embedded Image: {Filename}";

        public static implicit operator NekoImage(Embedded embedded) => embedded.Image ?? throw new Exception("await Load() before accessing the texture");
        public static implicit operator Task<NekoImage>(Embedded embedded) => embedded.Load();
    }
}
