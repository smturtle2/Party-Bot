using Discord;
using Discord.Commands;
using System;
using System.Text;
using System.Threading.Tasks;
using PartyBot.Handlers;
using System.Linq;
using System.Collections.Generic;
using Victoria;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Discord.WebSocket;

namespace PartyBot.Services
{
    public sealed class BotService
    {
        public LavaLinkAudio Audio { get; set; }
        public static List<ChromeDriver> Drivers; 
        protected DriverService = null;
        protected static ChromeOptions Options = null;
        protected static ChromeDriver Driver = null;

        public static void EndWordStart(SocketCommandContext context)
        {
            DriverService = ChromeDriverService.CreateDefaultService();
            DriverService.HideCommandPromptWindow = true;
            Options = new ChromeOptions();
            Options.AddArgument("--window-size=1280,800");
            Options.AddArgument("--disable-extensions");
            Options.AddArgument("--proxy-server='direct://'");
            Options.AddArgument("--proxy-bypass-list=*");
            Options.AddArgument("--start-maximized");
            Options.AddArgument("--headless");
            Options.AddArgument("--disable-gpu");
            Options.AddArgument("--disable-dev-shm-usage");
            Options.AddArgument("--no-sandbox");
            Options.AddArgument("--ignore-certificate-errors");
        }

        public async Task<Embed> DisplayInfoAsync(SocketCommandContext context)
        {
            var fields = new List<EmbedFieldBuilder>();
            fields.Add(new EmbedFieldBuilder {
                Name = "Client Info",
                Value = $"Current Server: {context.Guild.Name} - Prefix: {GlobalData.Config.DefaultPrefix}",
                IsInline = false
            });
            fields.Add(new EmbedFieldBuilder {
                Name = "Guild Info",
                Value = $"Current People: {context.Guild.Users.Count(x => !x.IsBot)} - Current Bots: {context.Guild.Users.Count(x => x.IsBot)} - Overall Users: {context.Guild.Users.Count}\n" +
                $"Text Channels: {context.Guild.TextChannels.Count} - Voice Channels: {context.Guild.VoiceChannels.Count}",
                IsInline = false
            });

            var embed = await Task.Run(() => new EmbedBuilder
            {
                Title = $"Info",
                ThumbnailUrl = context.Guild.IconUrl,
                Timestamp = DateTime.UtcNow,
                Color = Color.DarkOrange,
                Footer = new EmbedFooterBuilder { Text = "Powered By DraxCodes PartyBot & Victoria", IconUrl = context.Client.CurrentUser.GetAvatarUrl() },
                Fields = fields
            });

            return embed.Build();
        }

    }
}
