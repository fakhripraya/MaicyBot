using System;
using System.Threading.Tasks;

namespace maicy_bot_core
{
    class Program
    {
        static async Task Main(string[] args)
            => await new MaicyClientClass().InitializeAsync();
    }
}
