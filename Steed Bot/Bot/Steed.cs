namespace SteedBot.Bot
{
    using System;
#if DEBUG
    using System.Diagnostics;
#endif // DEBUG
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Timers;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using SteedBot.Commands;
    using SteedBot.Commands.Admin;

    internal class Steed
    {
        private const string SteedServerUrl = "http://steedservers.000webhostapp.com/steedbuild";

#if !DEBUG
        private const ulong SteedGuildId = 389407914366074901;
#else
        private const ulong SteedGuildId = 370513777122082816; // Bot Testing guild ID, for testing and debugging
#endif // !DEBUG

        private static readonly WebClient WebClient = new WebClient();

        private ulong[] adminIds;

        internal Steed(string token, string commands, string adminCommands)
        {
            DiscordClient = new DiscordClient(new DiscordConfiguration { Token = token });

            Commands = Regex.Matches(commands, "(?<Name>.+)\n(?<Syntax>!.+)\n(?<Regex>.+)\n(?<Description>.+)").Cast<Match>().Select(match => new Command(match.Groups["Name"].Value, match.Groups["Syntax"].Value, match.Groups["Description"].Value, match.Groups["Regex"].Value)).ToArray();

            AdminCommands = Regex.Matches(adminCommands, "(?<Name>.+)\n(?<Regex>.+)").Cast<Match>().Select(match => new AdminCommand(match.Groups["Name"].Value, match.Groups["Regex"].Value)).ToArray();

            DiscordClient.MessageCreated += async e =>
            {
                {
                    bool isAdmin = IsAdmin(e.Author);

                    if (!e.Message.Content.StartsWith("!") || e.Author.IsBot || e.Message.Content == "!confirm" && isAdmin) return;

                    if (IsAdmin(e.Author) && isAdmin)
                    {
                        var commandMatch = AdminCommands.Select(command => new { Command = command, Match = command.Match(e.Message.Content) }).FirstOrDefault(command => command.Match.Success);

                        if (commandMatch != null)
                        {
                            Timer confirmTimer = new Timer(30_000);

                            void OnElapsed(object sender, ElapsedEventArgs args)
                            {
                                confirmTimer.Elapsed -= OnElapsed;
                                DiscordClient.MessageCreated -= ProcessConfirmAsync;
                            }

                            confirmTimer.Elapsed += OnElapsed;
                            confirmTimer.Start();

                            async Task ProcessConfirmAsync(MessageCreateEventArgs args)
                            {
                                if (args.Author != e.Author || args.Message.Content != "!confirm") return;

                                DiscordClient.MessageCreated -= ProcessConfirmAsync;

                                confirmTimer.Stop();
                                confirmTimer.Elapsed -= OnElapsed;

                                await RespondAsync(args.Message, "You gaat it bro.");
                                await CommandInvoked(new CommandInvokedEventArgs(commandMatch.Command, e.Message, commandMatch.Match));
                            }

                            DiscordClient.MessageCreated += ProcessConfirmAsync;
                            await RespondAsync(e.Message, "`!confirm` please. 30 seconds left.");

                            return;
                        }
                    }
                }

                var matchedCommand = Commands.Select((command, index) => new { Command = command, Index = index, Match = command.Match(e.Message.Content) }).FirstOrDefault(command => command.Match.Success);

                if (matchedCommand == null)
                {
                    var closestMatch = Commands.Select(command => new { Command = command, Distance = command.ComputeLevensteinDistance(e.Message.Content.Split().First().TrimStart('!')) }).OrderBy(command => command.Distance).First();

                    await RespondAsync(e.Message, closestMatch.Distance <= 3 ? $"Invalid syntax.\nClosest match:\n{closestMatch.Command.HelpText}" : "Invalid syntax. Type `!help` for a syntax prompt.");

                    return;
                }

                await CommandInvoked(new CommandInvokedEventArgs(matchedCommand.Command, e.Message, matchedCommand.Match));
            };
        }

        internal DiscordClient DiscordClient { get; }

        internal Command[] Commands { get; }

        internal AdminCommand[] AdminCommands { get; }

        // ReSharper disable once TypeParameterCanBeVariant
        private delegate Task AsyncEventHandler<T>(T e);

        private event AsyncEventHandler<CommandInvokedEventArgs> CommandInvoked;

        private static string GetUrl(string filename) => $"{SteedServerUrl}/{filename}";

        internal async Task StartAsync()
        {
            adminIds = (await (await DiscordClient.GetGuildAsync(SteedGuildId)).GetAllMembersAsync()).Where(member => member.Roles.OrderByDescending(role => role.Position).FirstOrDefault()?.Permissions.HasPermission(Permissions.Administrator) ?? default).Select(member => member.Id).ToArray();

            await DiscordClient.ConnectAsync();
        }

        internal void RegisterCommandClass<T>() where T : class, new()
        {
            T tInstance = new T();

            CommandInvoked += e =>
            {
                MethodInfo commandMethod = typeof(T).GetMethods().FirstOrDefault(method => method.GetCustomAttributes(default).OfType<CommandAttribute>().FirstOrDefault()?.CommandName == e.CommandName);

                if (commandMethod == null) return Task.CompletedTask;

                commandMethod.Invoke(tInstance, new object[] { new CommandContext(this, e.Message, e.Match) });

                return Task.CompletedTask;
            };
        }

#if DEBUG
        internal void RegisterDebugCommandClass<T>() where T : class, new()
        {
            T tInstance = new T();

            CommandInvoked += e =>
            {
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();

                MethodInfo commandMethod = typeof(T).GetMethods().FirstOrDefault(method => method.GetCustomAttributes(default).OfType<CommandAttribute>().FirstOrDefault()?.CommandName == e.CommandName);

                stopwatch.Stop();

                Console.WriteLine($"Command delegation took {stopwatch.ElapsedTicks:N0} ticks");

                if (commandMethod == null) return Task.CompletedTask;

                commandMethod.Invoke(tInstance, new object[] { new CommandContext(this, e.Message, e.Match) });

                return Task.CompletedTask;
            };
        }
#endif // DEBUG

        internal string DownloadFile(string filename) => WebClient.DownloadString(GetUrl(filename));

        internal Task RespondAsync(DiscordMessage message, string response) => message.RespondAsync(string.Join("\n", message.Author.Mention, response));

        internal Task RespondWithFileAsync(DiscordMessage message, string filename) => message.RespondWithFileAsync(WebRequest.Create(GetUrl(filename)).GetResponse().GetResponseStream(), filename, message.Author.Mention);

        // ReSharper disable once SuggestBaseTypeForParameter
        private bool IsAdmin(DiscordUser discordUser) => IsAdmin(discordUser.Id);

        private bool IsAdmin(ulong discordUserId) => adminIds.Contains(discordUserId);

        private class CommandInvokedEventArgs : EventArgs
        {
            internal CommandInvokedEventArgs(BaseCommand command, DiscordMessage message, Match match)
            {
                Command = command;
                Message = message;
                Match = match;
            }

            internal BaseCommand Command { get; }

            internal string CommandName => Command.Name;

            internal DiscordMessage Message { get; }

            internal Match Match { get; }
        }
    }
}