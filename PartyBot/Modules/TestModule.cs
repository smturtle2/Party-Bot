using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PartyBot.Handlers;
using PartyBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace PartyBot.Modules
{
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        Random r;

        [Command("test")]
        public async Task test()
        {
            var sec = Context.Channel.Id.ToString();
            IniFile ini = new IniFile("Data.ini");
            ini.Write(sec, "끝말잇기", "ini read.");
            await Context.Channel.SendMessageAsync(ini.Read(sec, "끝말잇기"));
        }

        [Command("뽑기")]
        public async Task RandomCommand(params string[] text)
        {
            r = new Random();
            var prfx = GlobalData.Config.DefaultPrefix;
            if (text.Length > 0)
            {
                if (text[0].StartsWith(prfx))
                {
                    var N = int.Parse(text[0].Substring(1));
                    if (N <= text.Length - 1)
                    {
                        List<int> had = new List<int>();
                        for (int i = 0; i < N; i++)
                        {
                            var t = r.Next(1, text.Length);
                            while (had.Contains(t))
                            {
                                t = r.Next(1, text.Length);
                            }
                            had.Add(t);
                            var ck = text[t];
                            await Context.Channel.SendMessageAsync((i + 1) + "번째: " + ck);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("뽑힐 아이템보다 뽑을 개수가 더 크네");
                    }
                }
                else
                {
                    var ck = text[r.Next(0, text.Length)];
                    await Context.Channel.SendMessageAsync(ck + "(을)를 뽑았어");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("뭘 뽑으라는 거야..?");
            }
        }
        [Command("끝말잇기")]
        public async Task EndWordStart()
        {
            BotService.EndWordStart();
        }
    }
}
