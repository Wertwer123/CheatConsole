using System.Collections;
using System.Collections.Generic;
using System;


namespace Cheating
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [System.AttributeUsage(AttributeTargets.Method)]
    public sealed class CheatCommand : Attribute
    {

        string CheatConsoleCommand;
        string CommandDescription;

        #region Properties

        public string GetCheatConsoleCommand() => CheatConsoleCommand;
        public string GetCommandDescription() => CommandDescription;

        #endregion

        public CheatCommand(string CheatConsoleCommand,string CommandDescription)
        {
            this.CheatConsoleCommand = CheatConsoleCommand;
            this.CommandDescription = CommandDescription;
        }

    }
#endif
}


