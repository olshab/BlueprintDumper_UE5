using BlueprintDumper_UE5.Extensions.Components;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects.Properties;

namespace BlueprintDumper_UE5.Extensions
{
    public class UBlueprintGeneratedClass
    {
        public readonly USimpleConstructionScript? SimpleConstructionScript;

        public readonly UInheritableComponentHandler? InheritableComponentHandler;

        private readonly UObject _object;

        public UBlueprintGeneratedClass(UObject Object)
        {
            _object = Object;
            SimpleConstructionScript = GetSimpleConstructionScript();
            InheritableComponentHandler = GetInheritableComponentHandler();
        }

        public List<UActorComponent> GetAllComponents()
        {
            List<UActorComponent> Components = [];

            if (SimpleConstructionScript is not null)
            {
                foreach (USCS_Node SCS_Node in SimpleConstructionScript.AllNodes)
                {
                    if (SCS_Node.ComponentTemplate is not null)
                    {
                        Components.Add(SCS_Node.ComponentTemplate);
                    }
                }
            }
            if (InheritableComponentHandler is not null)
            {
                foreach (FComponentOverrideRecord Record in InheritableComponentHandler.Records ?? [])
                {
                    if (Record.ComponentTemplate is not null)
                    {
                        Components.Add(Record.ComponentTemplate);
                    }
                }
            }
            return Components;
        }

        public List<T> GetComponentsOfClass<T>() where T : UActorComponent
        {
            List<T> Components = [];

            if (SimpleConstructionScript is not null)
            {
                foreach (USCS_Node SCS_Node in SimpleConstructionScript.AllNodes)
                {
                    if (SCS_Node.ComponentTemplate is not null && SCS_Node.ComponentTemplate is T Component)
                    {
                        Components.Add(Component);
                    }
                }
            }
            if (InheritableComponentHandler is not null)
            {
                foreach (FComponentOverrideRecord Record in InheritableComponentHandler.Records ?? [])
                {
                    if (Record.ComponentTemplate is not null && Record.ComponentTemplate is T Component)
                    {
                        Components.Add(Component);
                    }
                }
            }
            return Components;
        }

        public List<UActorComponent> GetComponentsOfClass(string ComponentClass)
        {
            // Search for all components with the exact Class name
            List<UActorComponent> Components = [];

            if (SimpleConstructionScript is not null)
            {
                foreach (USCS_Node SCS_Node in SimpleConstructionScript.AllNodes)
                {
                    if (SCS_Node.ComponentTemplate is not null && SCS_Node.ComponentTemplate.ClassName == ComponentClass)
                    {
                        Components.Add(SCS_Node.ComponentTemplate);
                    }
                }
            }
            if (InheritableComponentHandler is not null)
            {
                foreach (FComponentOverrideRecord Record in InheritableComponentHandler.Records ?? [])
                {
                    if (Record.ComponentTemplate is not null && Record.ComponentTemplate.ClassName == ComponentClass)
                    {
                        Components.Add(Record.ComponentTemplate);
                    }
                }
            }
            return Components;
        }

        public bool HasAnyComponents()
        {
            // Any blueprint which is a child of AActor class has SimpleConstructionScript in it
            if (SimpleConstructionScript is null)
            {
                return false;
            }
            if (SimpleConstructionScript.AllNodes.Count > 1)
            {
                return true;
            }
            
            // If there is only one SCS_Node, chech if its name is not "DefaultSceneRoot"
            if (SimpleConstructionScript.AllNodes.Count == 1)
            {
                if (SimpleConstructionScript.AllNodes[0].InternalVariableName != "DefaultSceneRoot")
                {
                    return true;
                }
            }
            if (InheritableComponentHandler is not null && InheritableComponentHandler.Records?.Count > 0)
            {
                return true;
            }

            return false;
        }

        private USimpleConstructionScript? GetSimpleConstructionScript()
        {
            var SimpleConstructionScriptProperty = _object.Properties
                .First(x => x.Name.Text == "SimpleConstructionScript").Tag as ObjectProperty;
            if (SimpleConstructionScriptProperty is null || SimpleConstructionScriptProperty?.Value is null)
            {
                return null;
            }

            var SimpleConstructionScriptObject = SimpleConstructionScriptProperty.Value.ResolvedObject?.Load();
            if (SimpleConstructionScriptObject is null)
            {
                throw new Exception($"Could not load SimpleConstructionScript object for {_object.GetPathName()}");
            }
            return new USimpleConstructionScript(SimpleConstructionScriptObject);
        }

        private UInheritableComponentHandler? GetInheritableComponentHandler()
        {
            var InheritableComponentHandlerProperty = _object.Properties
                .FirstOrDefault(x => x.Name.Text == "InheritableComponentHandler")?.Tag as ObjectProperty;
            if (InheritableComponentHandlerProperty is null || InheritableComponentHandlerProperty?.Value is null)
            {
                return null;
            }

            var InheritableComponentHandlerObject = InheritableComponentHandlerProperty.Value.ResolvedObject?.Load();
            if (InheritableComponentHandlerObject is null)
            {
                throw new Exception($"Could not load InheritableComponentHandler object for {_object.GetPathName()}");
            }
            return new UInheritableComponentHandler(InheritableComponentHandlerObject);
        }
    }
}
