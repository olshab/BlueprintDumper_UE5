using BlueprintDumper_UE5.Extensions.Components;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace BlueprintDumper_UE5.Extensions
{
    public class USCS_Node
    {
        public readonly string ComponentClass;
        public readonly UActorComponent? ComponentTemplate;
        public readonly string? ParentComponentOrVariableName;
        public readonly string? ParentComponentOwnerClassName;
        public readonly List<USCS_Node>? ChildNodes;
        public readonly FGuid VariableGuid;
        public readonly string InternalVariableName;

        private readonly UObject _object;

        public USCS_Node(UObject Object)
        {
            _object = Object;

            // Component Class
            var ComponentClassProperty = _object.Properties
                .First(x => x.Name.Text == "ComponentClass").Tag as ObjectProperty;
            if (ComponentClassProperty is null || ComponentClassProperty?.Value is null)
            {
                throw new Exception($"Failed to find `ComponentClass` property inside {_object.GetPathName()}");
            }

            var ResolvedImport = ComponentClassProperty.Value.ResolvedObject;
            if (ResolvedImport is null)
            {
                throw new Exception($"Could not resolve `ComponentClass` for {_object.GetPathName()}");
            }
            ComponentClass = ResolvedImport.GetPathName();

            // Component Template
            var ComponentTemplateProperty = _object.Properties
                .FirstOrDefault(x => x.Name.Text == "ComponentTemplate")?.Tag as ObjectProperty;
            if (ComponentTemplateProperty is not null && ComponentTemplateProperty?.Value is not null)
            {
                var ComponentTemplateObject = ComponentTemplateProperty.Value.ResolvedObject?.Load();
                if (ComponentTemplateObject is not null)
                {
                    ComponentTemplate = UActorComponent.CreateComponent(ComponentTemplateObject, ComponentClass);
                }
            }

            // Component Class conversion
            if (ComponentTemplate is UActorSpawner ActorSpawner)
            {
                ComponentClass = "/Script/Engine.ChildActorComponent";
            }
            else if (ComponentTemplate?.IsSceneComponent ?? false)
            {
                ComponentClass = "/Script/Engine.SceneComponent";
            }

            // Parent Component Or Variable Name
            var ParentComponentOrVariableNameProperty = _object.Properties
                .FirstOrDefault(x => x.Name.Text == "ParentComponentOrVariableName")?.Tag as NameProperty;
            if (ParentComponentOrVariableNameProperty is not null)
            {
                ParentComponentOrVariableName = ParentComponentOrVariableNameProperty.Value.Text;
            }

            // Parent Component Owner Class Name
            var ParentComponentOwnerClassNameProperty = _object.Properties
                .FirstOrDefault(x => x.Name.Text == "ParentComponentOwnerClassName")?.Tag as NameProperty;
            if (ParentComponentOwnerClassNameProperty is not null)
            {
                ParentComponentOwnerClassName = ParentComponentOwnerClassNameProperty.Value.Text;
            }

            // Child Nodes
            var ChildNodesProperty = _object.Properties.FirstOrDefault(x => x.Name.Text == "ChildNodes")?.Tag as ArrayProperty;
            if (ChildNodesProperty is not null && ChildNodesProperty?.Value?.Properties is not null)
            {
                ChildNodes = [];
                foreach (var ChildNode in ChildNodesProperty.Value.Properties)
                {
                    var ChildNodeProperty = ChildNode as ObjectProperty;
                    var ChildNodeObject = ChildNodeProperty?.Value?.ResolvedObject?.Load();

                    if (ChildNodeObject is null)
                    {
                        continue;
                    }
                    ChildNodes.Add(new USCS_Node(ChildNodeObject));
                }
            }

            // Variable Guid
            var VariableGuidProperty = _object.Properties.First(x => x.Name.Text == "VariableGuid").Tag as StructProperty;
            if (VariableGuidProperty is null || VariableGuidProperty.Value is null)
            {
                throw new Exception($"Could not find VariableGuid for {_object.GetPathName()}");
            }
            VariableGuid = (FGuid)VariableGuidProperty.Value.StructType;

            // Internal Variable Name
            var InternalVariableNameProperty = _object.Properties
                .First(x => x.Name.Text == "InternalVariableName").Tag as NameProperty;
            if (InternalVariableNameProperty is null)
            {
                throw new Exception($"Could not find InternalVariableName for {_object.GetPathName()}");
            }
            InternalVariableName = InternalVariableNameProperty.Value.Text;
        }
    }
}
