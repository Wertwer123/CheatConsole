using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Reflection;
using UnityEngine.UI;
using Cheating.UIElements;


namespace Cheating
{
    public sealed class CheatConsoleDisplay : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        [Header("ConsoleUI")]
        [SerializeField] GameObject ConsoleUIRoot;
        [SerializeField] TMP_InputField CheatPromptInputField;
        [SerializeField] DropDownList AllAvailableCheatCommandsContainingInput;
        [SerializeField] TMP_Dropdown AllAvailableCommandsDropDown;
        [SerializeField] TMP_Text CheatMessageDisplayWindow;
        [SerializeField] Scrollbar MessageWindowScrolBarVertical;

        CheatConsole CheatConsole;
       
        string AllMessages = "";

        //Need a second counter for when we press the up arrow that always has the index of the last element in the usedCommands and if we press up we decrement it

        void Start()
        {
            CheatConsole = GetComponent<CheatConsole>();

            SceneManager.activeSceneChanged += (Scene CurrentScene, Scene NextScene) => GetAllAvailableCommands(CheatCommandCollector.GetAllCheatCommands());
            CheatConsole.OnCommandPromptEnterd += OnCheatPromptRecieved;
            CheatConsole.OnCommandPromptFailed += DisplayMessage;
            CheatConsole.OnCommandWithReturnFunctionExecuted += DisplayMessage;
            CheatConsole.OnSelectedCommandSwitched += PasteSelectedCommandFromLastUsedCommands;
            CheatConsole.OnConsoleActiveStateSwitched += (bool Active) => ConsoleUIRoot.SetActive(Active);

            CheatPromptInputField.onSubmit.AddListener(ExecuteCheatPrompt);
            CheatPromptInputField.onValueChanged.AddListener(DisplayAllAvailableCheatPrompts);
            AllAvailableCommandsDropDown.onValueChanged.AddListener(PasteSelectedCheatCommandFromAvailableCommands);
            AllAvailableCommandsDropDown.onValueChanged.AddListener(SelectCheatPromptField);

            GetAllAvailableCommands(CheatCommandCollector.GetAllCheatCommands());

        }
         void OnDestroy()
        {
            CheatConsole.OnCommandPromptEnterd -= OnCheatPromptRecieved;
            CheatConsole.OnCommandPromptFailed -= DisplayMessage;
            CheatConsole.OnSelectedCommandSwitched -= PasteSelectedCommandFromLastUsedCommands;
            CheatPromptInputField.onSubmit.RemoveListener(ExecuteCheatPrompt);
            CheatPromptInputField.onValueChanged.RemoveListener(DisplayAllAvailableCheatPrompts);
            AllAvailableCommandsDropDown.onValueChanged.RemoveListener(PasteSelectedCheatCommandFromAvailableCommands);
            AllAvailableCommandsDropDown.onValueChanged.RemoveListener(SelectCheatPromptField);
        }
        void PasteSelectedCommandFromLastUsedCommands(string NewSelectedCommand)
        {
            CheatPromptInputField.text = NewSelectedCommand;

        }
        void ExecuteCheatPrompt(string EnterdCommandPrompt)
        {
            CheatConsole.TryExecuteCommand(EnterdCommandPrompt);
            CheatPromptInputField.text = string.Empty;

            const int FullyScrolled = 1;
            MessageWindowScrolBarVertical.value = FullyScrolled;
            //Indicating wed dont do anything with the value 
            SelectCheatPromptField(0);
           
        }
        void SelectCheatPromptField(int _)
        {
            CheatPromptInputField.Select();
            CheatPromptInputField.ActivateInputField();
        }
        /// <summary>
        /// Displays a default message
        /// </summary>
        /// <param name="Message"></param>
        void DisplayMessage(string Message)
        {
            AllMessages += Message + "\n";
            CheatMessageDisplayWindow.text = AllMessages;
        }
        /// <summary>
        /// Displays a message that contains a return type
        /// </summary>
        /// <param name="ReturnValue"></param>
        /// <param name="CheatMethod"></param>
        /// <param name="ReturnType"></param>
        void DisplayMessage(object ReturnValue, MethodInfo CheatMethod, Type ReturnType)
        {
            DisplayReturnValue(ReturnValue, ReturnType);
        }
        void DisplayAllAvailableCheatPrompts(string CheatPromptFieldInput)
        {
            List<CheatCommand> MostLikelyCommands = CheatCommandCollector.GetAllCommandsContainingString(CheatPromptFieldInput);

            if (MostLikelyCommands == null) return;

            AllAvailableCheatCommandsContainingInput.Clear();
            
            foreach (var CheatCommand in MostLikelyCommands)
            {
                AllAvailableCheatCommandsContainingInput += new DropDownList.DropDownListElement(CheatCommand.GetCheatConsoleCommand());
            }

            AllAvailableCheatCommandsContainingInput.Refresh();

        }
        void OnCheatPromptRecieved(MethodInfo CheatMethod)
        {
            DisplayMessage(GetCheatCommandName(CheatMethod));
        }
        void PasteSelectedCheatCommandFromAvailableCommands(int SelectedCommand)
        {
            CheatPromptInputField.text = AllAvailableCommandsDropDown.options[SelectedCommand].text;
        }
        void GetAllAvailableCommands(List<CheatCommand> AllCommands)
        {
            List<string> AllCommandsByPrompt = new();
    
            foreach (var CheatCommand in AllCommands)
            {
                AllCommandsByPrompt.Add(CheatCommand.GetCheatConsoleCommand());
            }

            AllAvailableCommandsDropDown.AddOptions(AllCommandsByPrompt);
        }
        string GetCheatCommandName(MethodInfo CheatMethod)
        {
            return CheatMethod.GetCustomAttribute<CheatCommand>().GetCheatConsoleCommand();
        }
        string GetCheatCommandDescription(MethodInfo CheatMethod)
        {
            return CheatMethod.GetCustomAttribute<CheatCommand>().GetCommandDescription();
        }
        /// <summary>
        /// Displays a return value if its a IEnumerable display all elements in it
        /// </summary>
        /// <param name="Return"></param>
        /// <param name="ReturnType"></param>
        void DisplayReturnValue(object Return,Type ReturnType)
        {
            //If the return value is a IEnumerable display all elements in it
            if (ReturnType.GetInterface(nameof(IEnumerable)) != null)
            {
                IEnumerable Enumerable = (IEnumerable)Return;

                foreach (var Value in Enumerable)
                {
                    
                    DisplayMessage(Value.ToString());
                }
            }
            else
            {
                DisplayMessage(Return.ToString());
            }
           
        }
      
        #region Cheats

        [CheatCommand("Clear","Clears the console")]
        void ClearConsoleDisplay()
        {
            AllMessages = string.Empty;
            CheatMessageDisplayWindow.text = AllMessages;
        }
        [CheatCommand("Help", "Shows all commands and their description")]
        void ShowHelp()
        {
            AllMessages += "\n";

            foreach (var Cheat in CheatCommandCollector.GetAllCheatCommands())
            {
                DisplayMessage($"{Cheat.GetCheatConsoleCommand()} : {Cheat.GetCommandDescription()} \n");
            }
        }

        #endregion
#endif
    }

}


