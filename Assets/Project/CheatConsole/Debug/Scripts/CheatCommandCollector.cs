using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Cheating
{

    public sealed class CheatCommandCollector : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        static readonly Dictionary<string, List<Tuple<object,MethodInfo>>> CheatCommandMethods = new();
        static readonly Dictionary<Type,List<Tuple<CheatCommand,MethodInfo>>> AllTypesWithNonStaticCheatMethods = new();
        static readonly List<CheatCommand> AllCheatCommands = new();

        //Implement a function for getting all cheat commands that that are like the input
        #region Properties

        static public List<CheatCommand> GetAllCheatCommands() => AllCheatCommands;

        public List<Tuple<object,MethodInfo>> TryGetCheatCommand(string CommandPrompt,out bool CommandExists)
        {
            CommandExists = false;
            List<Tuple<object,MethodInfo>> ObjectsContainingCommand = null;

            if (CheatCommandMethods.ContainsKey(CommandPrompt))
            {
                ObjectsContainingCommand = CheatCommandMethods[CommandPrompt];
                CommandExists = true;
            }

            return ObjectsContainingCommand;
           
        }
        static public List<CheatCommand> GetAllCommandsContainingString(string GivenCommandInput)
        {
            string[] RawCommandPrompt = GivenCommandInput.Split(CheatConsole.COMMAND_DELIMITER);
            string CommandPrompt = RawCommandPrompt[0];
            return AllCheatCommands.FindAll((CheatCommand Cmd) => Cmd.GetCheatConsoleCommand().Contains(CommandPrompt));
        }

        #endregion
        void Awake()
        {
            CollectAllCheatCommandMethods();
        }
        void Start()
        {
            SceneManager.activeSceneChanged += (Scene CurrentScene, Scene NextScene) =>
            {
                GetAllSceneObjectsWithCheats();
            };
        }
        void CollectAllCheatCommandMethods()
        {
            Assembly[] AllAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var Assembly in AllAssemblies)
            {
                Type[] AssemblyTypes = Assembly.GetTypes();

                foreach (var ObjectType in AssemblyTypes)
                {
                    MethodInfo[] Methods = ObjectType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    //If its a static method we just add it with a  null target to the List of CheatCommands
                    //if its not we find all objects in the scene of the given type and add them into the dictinairy
                    foreach (var Method in Methods)
                    {
                        CheatCommand CheatCommandAttribute = Method.GetCustomAttribute<CheatCommand>();
                       

                        if(CheatCommandAttribute != null)
                        {
                            AllCheatCommands.Add(CheatCommandAttribute);

                            if (Method.IsStatic)
                            {
                                AddMethod(Method,CheatCommandAttribute);
                            }
                            else
                            {
                               
                              
                                //If our dictionair< already contains this type then we add a new tuple to the list of objects
                                if (AllTypesWithNonStaticCheatMethods.ContainsKey(ObjectType))
                                {
                                    AllTypesWithNonStaticCheatMethods[ObjectType].Add(new Tuple<CheatCommand,MethodInfo>(CheatCommandAttribute,Method));
                                    continue;
                                }
                                else
                                {
                                    //If its a type with a cheatmethod that our dictionairy doesnt contain already we simply add it to the list of tuples consisting of cheatcommands 
                                    //and their according methods
                                    AllTypesWithNonStaticCheatMethods.Add(ObjectType, new List<Tuple<CheatCommand, MethodInfo>>() { new(CheatCommandAttribute, Method) });
                                    continue;
                                }
                               
                            }
                        }
                    }
                }
            }

            GetAllSceneObjectsWithCheats();
        }
        /// <summary>
        /// Adds a method to our dictionairy if its static ignore the method holder
        /// </summary>
        /// <param name="MethodToAdd"></param>
        /// <param name="CheatCommandAttribute"></param>
        /// <param name="MethodHolder"></param>
        void AddMethod(MethodInfo MethodToAdd,CheatCommand CheatCommandAttribute,object MethodHolder = null)
        {
            string CheatCommand = CheatCommandAttribute.GetCheatConsoleCommand();

            Tuple<object, MethodInfo> Method = new(MethodHolder, MethodToAdd);

            if (CheatCommandMethods.ContainsKey(CheatCommand))
            {
                CheatCommandMethods[CheatCommand].Add(Method);
            }
            else if (!CheatCommandMethods.ContainsKey(CheatCommand) || !CheatCommandMethods[CheatCommand].Contains(Method))
            {
                CheatCommandMethods.Add(CheatCommand, new List<Tuple<object, MethodInfo>>() { Method });
            }
        }
        /// <summary>
        /// Collects all objects in the scene containing nonstatic cheat methods
        /// </summary>
        void GetAllSceneObjectsWithCheats()
        {
            foreach(var TypeWithNonStaticCheats in AllTypesWithNonStaticCheatMethods)
            {
                //Search all objects in the scene of the given type 
                var ObjectsWithCheatMethods = FindObjectsOfType(TypeWithNonStaticCheats.Key);
                if (ObjectsWithCheatMethods.Length == 0) return;
                //Loop over all cheat methods for the given type
                foreach (var CheatMethod in AllTypesWithNonStaticCheatMethods[TypeWithNonStaticCheats.Key])
                {
                    foreach (var Object in ObjectsWithCheatMethods)
                    {
                        CheatCommand CheatCommandAttribute = CheatMethod.Item1;
                        AddMethod(CheatMethod.Item2, CheatCommandAttribute, Object);
                    }
                }
            }
        }
        /// <summary>
        /// Removes the object and its cheat from our available cheats 
        /// </summary>
        /// <param name="ObjectToRemove"></param>
        static public void RemoveSceneObjectFromCheats(object ObjectToRemove)
        {
            string CheatCommand = AllTypesWithNonStaticCheatMethods[ObjectToRemove.GetType()][0].Item1.GetCommandDescription();

            if (CheatCommandMethods.ContainsKey(CheatCommand))
            {
                CheatCommandMethods.Remove(CheatCommand);
            }
           
        }
#endif
    }

}


