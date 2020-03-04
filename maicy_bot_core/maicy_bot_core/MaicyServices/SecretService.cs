using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using maicy_bot_core.MiscData;

namespace maicy_bot_core.MaicyServices
{
    public class SecretService
    {
        private DiscordSocketClient maicy_client;

        public SecretService(DiscordSocketClient client)
        {
            maicy_client = client;
        }

        public string om_ai_async()
        {
            return "I Love You cy , its only been 3 month but im already in love with you\n" +
                "I know this is fast but that's what i felt now...";
        }
    }
}
