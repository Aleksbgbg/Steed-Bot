namespace SteedBot.Commands
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    internal class CommandActions
    {
        private static readonly Random Random = new Random();

        [Command("help")]
        public async Task Help(CommandContext context) => await context.RespondAsync($"**Commands:**\n{string.Join("\n\n", context.Bot.Commands.Select(command => command.HelpText))}");

        [Command("steed")]
        public async Task Steed(CommandContext context)
        {
            string[] matchGroups = context.Match.Groups.Cast<Group>().Skip(1).Select(group => group.ToString()).ToArray();

            if (matchGroups.All(match => match == string.Empty)) await context.RespondAsync(Random.Next(2) == 1 ? "Steed Bot is online." : "Yes?");

            foreach (string match in matchGroups)
            {
                switch (match)
                {
                    case "changelog":
                        await context.RespondAsync($"**Change Log:**\n{context.Bot.DownloadFile("updatelog.txt")}");
                        break;

                    case "version":
                        // Arbitrarily formatting the DateTime string downloaded from steedservers in order to be parsed correctly - most likely will break (again) in the future
                        await context.RespondAsync($"The latest Steed version was released on {DateTime.Parse(string.Concat(context.Bot.DownloadFile("version.txt").Insert(2, " ").Insert(5, " ").TakeWhile(character => character != '#'))).ToLongDateString()}.");
                        break;

                    case "update":
                        await context.RespondWithFileAsync("steedapp.zip");
                        break;

                    case "screenshot":
                        await context.RespondWithFileAsync("screenshot.png");
                        break;
                }
            }
        }
    }
}