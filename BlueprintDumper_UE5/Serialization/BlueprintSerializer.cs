using BlueprintDumper_UE5.Extensions;
using BlueprintDumper_UE5.Extensions.Components;
using BlueprintDumper_UE5.Utils;
using CUE4Parse.UE4.Assets.Exports;
using Newtonsoft.Json;
using System.Text;

namespace BlueprintDumper_UE5.Serialization
{
    public class BlueprintSerializer
    {
        public readonly HashSet<string> ReferencedPackages = [];

        private readonly UObject _blueprintObject;
        private readonly string _customComponentTag;

        private readonly JsonWriter _writer;
        private readonly StringBuilder _sb;

        
        private readonly PropertySerializer _propertySerializer;

        public BlueprintSerializer(UObject BlueprintObject, string CustomComponentTag)
        {
            _blueprintObject = BlueprintObject;
            _customComponentTag = CustomComponentTag;

            _sb = new StringBuilder();
            StringWriter sw = new StringWriter(_sb);
            _writer = new JsonTextWriter(sw);

            _propertySerializer = new PropertySerializer(_writer, ReferencedPackages);

            _writer.Formatting = Formatting.Indented;
            _writer.WriteStartObject();
        }

        public void SerializeBlueprintNameAndParent(Dictionary<string, string> AlreadyExistingAssets)
        {            
            string BlueprintPackageName = _blueprintObject.Owner!.Name.GetAssetName();
            _writer.WritePropertyName("BlueprintName");
            _writer.WriteValue(BlueprintPackageName);

            string? SuperClassObjectPath = null;
            var BlueprintGeneratedClassObject = (CUE4Parse.UE4.Objects.Engine.UBlueprintGeneratedClass)_blueprintObject;
            var SuperClass = BlueprintGeneratedClassObject.SuperStruct.ResolvedObject;
            if (SuperClass is null)
            {
                throw new Exception($"Failed to get SuperClass for blueprint `{_blueprintObject.GetPathName()}`");
            }

            SuperClassObjectPath = SuperClass.GetPathName();
            _writer.WritePropertyName("ParentClassObjectPath");
            _writer.WriteValue(SuperClassObjectPath);
        }

        public void Serialize(USimpleConstructionScript? SCS)
        {
            _writer.WritePropertyName("Components");
            _writer.WriteStartArray();
            if (SCS is not null)
            {
                foreach (USCS_Node RootNode in SCS.RootNodes)
                {
                    Serialize(RootNode, RootNode.ParentComponentOrVariableName);
                }
            }
            _writer.WriteEndArray();
        }

        public void Serialize(UInheritableComponentHandler? InheritableComponentHandler)
        {
            _writer.WritePropertyName("ComponentOverrideRecords");
            _writer.WriteStartArray();
            if (InheritableComponentHandler is not null)
            {
                foreach (FComponentOverrideRecord Record in InheritableComponentHandler.Records ?? [])
                {
                    Serialize(Record);
                }
            }
            _writer.WriteEndArray();
        }

        public void SerializeOverrideObjectPaths(Dictionary<string, string> OverrideObjectPaths)
        {
            _writer.WritePropertyName("OverrideObjectPaths");
            _writer.WriteStartArray();
            foreach (var PathPair in OverrideObjectPaths)
            {
                _writer.WriteStartObject();
                _writer.WritePropertyName("OldPath");
                _writer.WriteValue(PathPair.Key);
                _writer.WritePropertyName("NewPath");
                _writer.WriteValue(PathPair.Value);
                _writer.WriteEndObject();
            }
            _writer.WriteEndArray();
        }

        public void SaveToDisk(string JsonFilePath)
        {
            _writer.WriteEndObject();
            File.WriteAllText(JsonFilePath, _sb.ToString());
        }

        private void Serialize(USCS_Node Node, string? ParentComponentName)
        {
            _writer.WriteStartObject();

            _writer.WritePropertyName("InternalVariableName");
            _writer.WriteValue(Node.InternalVariableName);

            _writer.WritePropertyName("ParentNodeName");
            _writer.WriteValue(ParentComponentName);

            if (Node.ComponentTemplate is null)
            {
                _writer.WritePropertyName("ComponentClass");
                _writer.WriteValue(Node.ComponentClass);

                _writer.WritePropertyName("Properties");
                _writer.WriteStartObject();
                _writer.WriteEndObject();

                _writer.WriteEndObject();
                return;
            }

            // TODO: remove code duplication
            _writer.WritePropertyName("ComponentClass");
            _writer.WriteValue(Node.ComponentClass);

            // Add custom ComponentTag
            if (!string.IsNullOrEmpty(_customComponentTag))
            {
                Node.ComponentTemplate.AddComponentTag(_customComponentTag);
            }
            if (Node.ComponentTemplate is UActorSpawner)
            {
                Node.ComponentTemplate.AddComponentTag("ActorSpawner");
            }

            _writer.WritePropertyName("Properties");
            _writer.WriteStartObject();
            Node.ComponentTemplate.Serialize(_propertySerializer);
            _writer.WriteEndObject();

            _writer.WriteEndObject();

            // Serialize all child nodes
            foreach (USCS_Node ChildNode in Node.ChildNodes ?? [])
            {
                Serialize(ChildNode, Node.InternalVariableName);
            }
        }

        private void Serialize(FComponentOverrideRecord Record)
        {
            _writer.WriteStartObject();

            _writer.WritePropertyName("SCSVariableName");
            _writer.WriteValue(Record.ComponentKey.SCSVariableName);

            _writer.WritePropertyName("ComponentClass");
            _writer.WriteValue(Record.ComponentClass?.GetPathName());

            _writer.WritePropertyName("Properties");
            _writer.WriteStartObject();
            Record.ComponentTemplate?.Serialize(_propertySerializer);
            _writer.WriteEndObject();

            _writer.WriteEndObject();
        }
    }
}
