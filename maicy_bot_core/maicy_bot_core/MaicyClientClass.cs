using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Victoria;
using maicy_bot_core.MaicyServices;
using System.Threading;
using System.Linq;

namespace maicy_bot_core
{
    public class MaicyClientClass
    {
        private DiscordSocketClient maicy_client;
        private CommandService maicy_cmd_serv;
        private IServiceProvider maicy_services;
        private Timer _timer;
        private static List<string> _statusList = new List<string>();
        private int _statusIndex = 0;

        public MaicyClientClass(DiscordSocketClient client = null, CommandService cmd = null)
        {
            maicy_client = client ?? new DiscordSocketClient(new DiscordSocketConfig {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50,
                LogLevel = LogSeverity.Debug
            });

            maicy_cmd_serv = cmd ?? new CommandService(new CommandServiceConfig{
                LogLevel = LogSeverity.Verbose,
                CaseSensitiveCommands = false
            });
        }

        public async Task InitializeAsync()
        {
            //Login
            await maicy_client.LoginAsync(TokenType.Bot, "<secret>); //maicy

            //Startin the bot
            await maicy_client.StartAsync();
            maicy_client.GuildAvailable += Maicy_client_GuildAvailable;
            maicy_client.Ready += Maicy_client_Ready;
            maicy_client.Log += Maicy_client_Log;
            maicy_services = SetupServices();

            //MaicyCommandClass
            var cmd_handler = new MaicyCommandClass(maicy_client, maicy_cmd_serv, maicy_services);
            await cmd_handler.InitializeAsync();

            //MusicService
            await maicy_services.GetRequiredService<MusicService>().InitializeAsync();

            //bot live forever
            await Task.Delay(-1);
        }

        private Task Maicy_client_GuildAvailable(SocketGuild guild)
        {
            //if (guild.Name == "English House")
            //{
            //    guild.Users.ToList().ForEach(x => _statusList.Add(x.Nickname));
            //    _statusList.RemoveAll(item => item == null);
            //}

            _statusList.Add("Maicy");

            return Task.CompletedTask;
        }

        private async Task Maicy_client_Ready()
        {
            try
            {
                _timer = new Timer(async _ =>
                {
                    await maicy_client.SetGameAsync(_statusList.ElementAtOrDefault(_statusIndex), type: ActivityType.Watching);
                    _statusIndex = _statusIndex + 1 == _statusList.Count ? 0 : _statusIndex + 1;
                },
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Task.CompletedTask;
                return;
            }
        }

        private Task Maicy_client_Log(LogMessage log_message)
        {
            Console.WriteLine(log_message.Message);
            return Task.CompletedTask;
        }

        //Dependency Injection
        private IServiceProvider SetupServices()
            => new ServiceCollection()
            .AddSingleton(maicy_client)
            .AddSingleton(maicy_cmd_serv)
            .AddSingleton<LavaRestClient>()
            .AddSingleton<LavaSocketClient>()
            .AddSingleton<MusicService>()
            .AddSingleton<UtilityService>()
            .AddSingleton<SecretService>()
            .BuildServiceProvider();
    }
}
