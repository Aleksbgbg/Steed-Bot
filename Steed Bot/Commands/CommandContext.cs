namespace Steed.Bot.Commands
{
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using Steed.Bot.Bot;

    internal class CommandContext
    {
        internal CommandContext(SteedBot bot, DiscordMessage message, Match match)
        {
            Bot = bot;
            Message = message;
            Match = match;
        }

        internal SteedBot Bot { get; }

        internal DiscordMessage Message { get; }

        internal DiscordClient DiscordClient => Bot.DiscordClient;

        internal Match Match { get; }

        internal Task RespondAsync(string response) => Bot.RespondAsync(Message, response);

        internal Task RespondWithFileAsync(string filename) => Bot.RespondWithFileAsync(Message, filename);
    }
}