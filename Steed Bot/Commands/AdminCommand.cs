namespace Steed.Bot.Commands
{
    using System;

    internal class AdminCommand : BaseCommand
    {
        private readonly Action action;

        internal AdminCommand(string regex, Action action) : base(regex) => this.action = action;

        internal bool IsMatch(string command) => regex.IsMatch(command);

        internal void Execute() => action();
    }
}