using System.Reflection;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Cheating
{
    [RequireComponent(typeof(CheatCommandCollector))]
    [RequireComponent(typeof(CheatConsoleDisplay))]
    public sealed class CheatConsole : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        [Header("Settings")]
        [SerializeField, Range(0, 50)] int CommandStackMaxSize;
        [SerializeField] char CommandDelimiter;
        [SerializeField] char CommandParameterDelimiter;

        [Header("Input")]
        [SerializeField] InputActionReference SwitchSelectedCommand;
        [SerializeField] InputActionReference EnOrDisableCheatConsole;


        CheatCommandCollector CheatCommandCollector;

        bool IsConsoleEnabled = false;

        string[] LastUsedCommands;
        int LastUsedCommandsUpperBound = 0;
        int CommandInputCounter = 0;
        int CommandsQueued = 0;
        int CurrentlySelectedCommand = 0;

        public delegate void OnCommandWithReturnFunctionExecutedDelegate(object ReturnValue, MethodInfo CheatMethod,Type ReturnType);
        public delegate void OnCommandPromptEnterdDelegate(MethodInfo CheatMethod);
        public delegate void OnCommandPromptFailedDelegate(string ErrorMessage);
        public delegate void OnSelectedCommandSwitchedDelegate(string NewSelectedCommand);
        public delegate void OnConsoleActiveStateSwitchedDelegate(bool Active);

        public event OnCommandWithReturnFunctionExecutedDelegate OnCommandWithReturnFunctionExecuted;
        public event OnCommandPromptEnterdDelegate OnCommandPromptEnterd;
        public event OnCommandPromptFailedDelegate OnCommandPromptFailed;
        public event OnSelectedCommandSwitchedDelegate OnSelectedCommandSwitched;
        public event OnConsoleActiveStateSwitchedDelegate OnConsoleActiveStateSwitched;

        static public char COMMAND_DELIMITER { get; private set;}

        #region Properties

        public string GetCommandPromptOutOfLastUsedPrompts(int PromptIndex) => LastUsedCommands[PromptIndex];

        #endregion

        // Start is called before the first frame update
        void Start()
        {
            CheatCommandCollector = GetComponent<CheatCommandCollector>();
            COMMAND_DELIMITER = CommandDelimiter;
            LastUsedCommands = new string[CommandStackMaxSize];
            LastUsedCommandsUpperBound = LastUsedCommands.GetUpperBound(0);

            EnOrDisableCheatConsole.action.Enable();

            EnableCheatConsole();
        }
        void Update()
        {
            //If we press tab we select the currently selected in the last used commands
            //then increment the Currently Selected command 
            if (SwitchSelectedCommand.action.WasPressedThisFrame())
            {
                SwitchComand();
            }
            if (EnOrDisableCheatConsole.action.WasPerformedThisFrame())
            {
                IsConsoleEnabled = !IsConsoleEnabled;
                OnConsoleActiveStateSwitched?.Invoke(IsConsoleEnabled);
            }
            
        }
        void SwitchComand()
        {
            if (CommandsQueued == 0) return;

            if(CurrentlySelectedCommand % CommandsQueued == 0)
            {
                OnSelectedCommandSwitched?.Invoke(LastUsedCommands[CurrentlySelectedCommand]);
                CurrentlySelectedCommand = 0;
            }

            OnSelectedCommandSwitched?.Invoke(LastUsedCommands[CurrentlySelectedCommand]);
            CurrentlySelectedCommand++;
        }
        void EnableCheatConsole()
        {
            SwitchSelectedCommand.action.Enable();
        }
        void DisableCheatConsole()
        {
            SwitchSelectedCommand.action.Disable();
        }
        string[] GetCommandParameters(string CommandPrompt,out string CommandToExecute)
        {
            string[] Command = CommandPrompt.Split(CommandDelimiter);
            string Parameters = string.Empty;
            CommandToExecute = Command[0];//The first input always has to be the command prompt

            //We then add the paramters to the parameter string
            for (int i = 1; i < Command.Length; i++)
            {
                Parameters += Command[i];
            }
            //We then split the parameters with our parameter delimiter
            return Parameters.Split(CommandParameterDelimiter);
        }
        /// <summary>
        /// Returns our list of parameters but as typed objects instead of strings 
        /// so that we can use them as input parameters
        /// </summary>
        /// <param name="Parameters"></param>
        /// <param name="Command"></param>
        /// <returns></returns>
        List<object> GetCommandParameters(string[] Parameters,MethodInfo Command)
        {
            ParameterInfo[] MethodParameters = Command.GetParameters();

            if(Parameters.Length != MethodParameters.Length)
            {
                OnCommandPromptFailed?.Invoke($"Wrong amount of parameters for command : Your amount of parameters was {Parameters.Length} but {Command.Name} has {Command.GetParameters().Length} \n");
                return null;
            }

            Type[] MethodParameterTypes = new Type[MethodParameters.Length];

            //Get the method parameter types
            for (int i = 0; i < MethodParameterTypes.Length; i++)
            {
                MethodParameterTypes[i] = MethodParameters[i].ParameterType;
            }

            List<object> MethodParametersTyped = new();

            //and then we try to convert the types to the parameter types and add it to the object paramter list
            for (int i = 0; i < Parameters.Length; i++)
            {
                try
                {
                    object ParameterValue = Convert.ChangeType(Parameters[i], MethodParameterTypes[i]);
                    MethodParametersTyped.Add(ParameterValue);
                }
                catch(Exception ex)
                {
                    OnCommandPromptFailed?.Invoke($"Your given parameter {Parameters[i]} couldn't be formatted \n ");
                }
            }

            return MethodParametersTyped;

        }
        /// <summary>
        /// Pushes a command to 
        /// </summary>
        /// <param name="EnterdCommand"></param>
        void PushCommandIntoLastUsedCommands(string EnterdCommand)
        {
            LastUsedCommands[CommandInputCounter] = EnterdCommand;
            CurrentlySelectedCommand = CommandInputCounter;

            CommandInputCounter++;

            if (CommandInputCounter % LastUsedCommandsUpperBound == 0)
            {
                CommandInputCounter = 0;
            }
            if (CommandsQueued != LastUsedCommandsUpperBound)
            {
                CommandsQueued++;
            }
           
        }
        public void TryExecuteCommand(string CommandPrompt)
        {
            if (!Application.isPlaying) return;

            PushCommandIntoLastUsedCommands(CommandPrompt);

            string[] CommandParameterUntyped = GetCommandParameters(CommandPrompt,out string CommandToCall);
            var CommandsToUse = CheatCommandCollector.TryGetCheatCommand(CommandToCall, out bool CommandExists);
            
            if (CommandExists)
            {
                foreach (var Command in CommandsToUse)
                {
                    MethodInfo CheatCommandMethod = Command.Item2;
                   
                    if (TryGetParameters(CheatCommandMethod, out ParameterInfo[] MethodParams))
                    {
                        List<object> CommandParametersTyped = GetCommandParameters(CommandParameterUntyped, Command.Item2);
              
                        if (CommandParametersTyped == null || CommandParametersTyped.Count == 0) return;


                        if (CheatCommandMethod.ReturnType != typeof(void))
                        {
                            object ReturnValue = CheatCommandMethod.Invoke(Command.Item1, CommandParametersTyped.ToArray());
                            Type ReturnValueType = CheatCommandMethod.ReturnType;
                            OnCommandWithReturnFunctionExecuted?.Invoke(ReturnValue, Command.Item2, ReturnValueType);
                            continue;
                        }
                        else
                        {
                            CheatCommandMethod.Invoke(Command.Item1, CommandParametersTyped.ToArray());
                            OnCommandPromptEnterd.Invoke(Command.Item2);
                            continue;
                        }

                    }
                    else
                    {
                        if (CheatCommandMethod.ReturnType != typeof(void))
                        {
                            object ReturnValue = CheatCommandMethod.Invoke(Command.Item1,null);
                            Type ReturnValueType = CheatCommandMethod.ReturnType;
                            OnCommandWithReturnFunctionExecuted?.Invoke(ReturnValue, Command.Item2, ReturnValueType);
                            continue;
                        }
                        else
                        {
                            CheatCommandMethod.Invoke(Command.Item1, null);
                            OnCommandPromptEnterd.Invoke(Command.Item2);
                            continue;
                        } 
                    }
                   
                }
            }
            else
            {
                OnCommandPromptFailed?.Invoke("The command you enterd did not match any existing command");
            }
        }
        bool TryGetParameters(MethodInfo Method,out ParameterInfo[] MethodParams)
        {
            MethodParams = Method.GetParameters();
            return MethodParams.Length > 0;
        }
#endif
    }
}


