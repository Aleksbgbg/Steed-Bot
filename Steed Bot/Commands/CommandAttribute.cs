namespace SteedBot.Commands
{
    using System;

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(string commandName) => CommandName = commandName;

        public string CommandName { get; }
    }
}