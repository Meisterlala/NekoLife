using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiNET;

namespace Neko.Sources
{
    public class NekosLife : IImageSource
    {
#pragma warning disable
        class NekosLifeJson
        {
            public string url { get; set; }
        }
#pragma warning restore

        public async Task<NekoImage> Next(CancellationToken ct = default)
        {
            var url = "https://nekos.life/api/v2/img/neko";
            // Get a random image URL
            NekosLifeJson response = await Common.ParseJson<NekosLifeJson>(url, ct);
            // Download  image
            return await Common.DownloadImage(response.url, ct); ;
        }
    }

}