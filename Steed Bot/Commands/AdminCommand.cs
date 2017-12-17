namespace Steed.Bot.Commands
{
    using System;
    using System.Text.RegularExpressions;

    internal class AdminCommand : BaseCommand
    {
        private readonly Regex regex;

        private readonly Action action;

        internal AdminCommand(string regex, Action action) : base(regex) => this.action = action;

        internal bool IsMatch(string command) => regex.IsMatch(command);

        internal bool Execute()
        {
            action();
            return true;
        }
    }
}