using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlueprintDumper_UE5.Serialization
{
    public class PropertySerializer
    {
        private readonly JsonWriter _writer;

        private readonly HashSet<string> _referencedPackages;

        private readonly HashSet<string> _sceneComponentProperties = [
            "PrimaryComponentTick",
            "ComponentTags",
            "bAutoActivate",
            "bEditableWhenInherited",
            "bCanEverAffectNavigation",
            "bIsEditorOnly",
            "RelativeLocation",
            "RelativeRotation",
            "RelativeScale3D",
            "bAbsoluteLocation",
            "bAbsoluteRotation",
            "bAbsoluteScale",
            "bVisible",
            "bShouldUpdatePhysicsVolume",
            "bHiddenInGame",
            "bUseAttachParentBound",
            "Mobility",
            "DetailMode",
        ];

        private readonly HashSet<string> _propertiesBlacklist = [
            "DetailLevelBeforeCastShadows",
            "BodyInstance",
            "bUseDefaultCollision",
        ];

        public PropertySerializer(JsonWriter Writer, HashSet<string> ReferencedPackages)
        {
            _writer = Writer;
            _referencedPackages = ReferencedPackages;
        }

        public void SerializePropertyValue(FPropertyTagType InProperty, string? PropertyName)
        {
            if (PropertyName is not null)
            {
                if (_propertiesBlacklist.Contains(PropertyName))
                {
                    return;
                }
                _writer.WritePropertyName(PropertyName);
            }

            if (InProperty is ArrayProperty ArrayProperty)
            {
                _writer.WriteStartArray();
                foreach (var InnerProperty in ArrayProperty.Value!.Properties)
                {
                    SerializePropertyValue(InnerProperty, null);
                }
                _writer.WriteEndArray();
                return;
            }
            else if (InProperty is BoolProperty BoolProperty)
            {
                _writer.WriteValue(BoolProperty.Value);
                return;
            }
            else if (InProperty is DelegateProperty)
            {
                throw new NotImplementedException("Serialization for FDelegateProperty is not implemented");
            }
            else if (InProperty is EnumProperty EnumProperty)
            {
                _writer.WriteValue(EnumProperty.Value.ToString());
                return;
            }
            else if (InProperty is InterfaceProperty)
            {
                throw new NotImplementedException("Serialization for FInterfaceProperty is not implemented");
            }
            else if (InProperty is MapProperty)
            {
                _writer.WriteNull();
                return;
            }
            else if (InProperty is MulticastDelegateProperty)
            {
                throw new NotImplementedException("Serialization for FMulticastDelegateProperty is not implemented");
            }
            else if (InProperty is MulticastInlineDelegateProperty)
            {
                throw new NotImplementedException("Serialization for FMulticastInlineDelegateProperty is not implemented");
            }
            else if (InProperty is MulticastSparseDelegateProperty)
            {
                throw new NotImplementedException("Serialization for FMulticastSparseDelegateProperty is not implemented");
            }
            else if (InProperty is NameProperty NameProperty)
            {
                _writer.WriteValue(NameProperty.Value.ToString());
                return;
            }
            else if (InProperty is ByteProperty ByteProperty)
            {
                _writer.WriteValue(ByteProperty.Value);
                return;
            }
            else if (InProperty is DoubleProperty DoubleProperty)
            {
                _writer.WriteValue(DoubleProperty.Value);
                return;
            }
            else if (InProperty is FloatProperty FloatProperty)
            {
                _writer.WriteValue(FloatProperty.Value);
                return;
            }
            else if (InProperty is Int16Property Int16Property)
            {
                _writer.WriteValue(Int16Property.Value);
                return;
            }
            else if (InProperty is Int64Property Int64Property)
            {
                _writer.WriteValue(Int64Property.Value);
                return;
            }
            else if (InProperty is Int8Property Int8Property)
            {
                _writer.WriteValue(Int8Property.Value);
                return;
            }
            else if (InProperty is IntProperty IntProperty)
            {
                _writer.WriteValue(IntProperty.Value);
                return;
            }
            else if (InProperty is UInt16Property UInt16Property)
            {
                _writer.WriteValue(UInt16Property.Value);
                return;
            }
            else if (InProperty is UInt32Property UInt32Property)
            {
                _writer.WriteValue(UInt32Property.Value);
                return;
            }
            else if (InProperty is UInt64Property UInt64Property)
            {
                _writer.WriteValue(UInt64Property.Value);
                return;
            }
            else if (InProperty is ObjectProperty ObjectProperty)
            {
                var Object = ObjectProperty.Value!.ResolvedObject;
                if (Object is not null)
                {
                    _writer.WriteValue(Object.GetPathName());
                    _referencedPackages.Add(Object.GetPathName());
                    return;
                }
            }
            else if (InProperty is SoftObjectProperty SoftObjectProperty)
            {
                _writer.WriteValue(SoftObjectProperty.Value.AssetPathName.ToString());
                return;
            }
            else if (InProperty is SetProperty)
            {
                throw new NotImplementedException("Serialization for FSetProperty is not implemented");
            }
            else if (InProperty is StrProperty StrProperty)
            {
                _writer.WriteValue(StrProperty.Value);
                return;
            }
            else if (InProperty is StructProperty StructProperty)
            {
                if (StructProperty.Value!.StructType is FStructFallback DefaultStruct)
                {
                    _writer.WriteStartObject();
                    foreach (var MemberProperty in DefaultStruct.Properties)
                    {
                        SerializePropertyValue(MemberProperty.Tag!, MemberProperty.Name.ToString());
                    }
                    _writer.WriteEndObject();
                }
                else
                {
                    JToken SpecialStruct = JToken.FromObject(StructProperty.Value!.StructType);
                    SpecialStruct.WriteTo(_writer);
                }
                return;
            }
            else if (InProperty is TextProperty TextProperty)
            {
                _writer.WriteValue(TextProperty.Value!.Text);
                return;
            }

            _writer.WriteNull();
        }

        public bool IsSceneComponentProperty(FPropertyTag Property)
        {
            return _sceneComponentProperties.Contains(Property.Name.ToString());
        }

        public void InstancedStaticMesh_BulkSerialize(UInstancedStaticMeshComponent InstancedStaticMeshComponent)
        {
            if (InstancedStaticMeshComponent.PerInstanceSMData is null)
            {
                return;
            }
            _writer.WritePropertyName("PerInstanceSMData");
            _writer.WriteStartArray();

            foreach (var PerInstanceData in InstancedStaticMeshComponent.PerInstanceSMData)
            {
                _writer.WriteStartObject();

                _writer.WritePropertyName("Transform");
                _writer.WriteStartObject();

                _writer.WritePropertyName("XPlane");
                _writer.WriteStartObject();
                _writer.WritePropertyName("X");
                _writer.WriteValue(PerInstanceData.Transform.M00);
                _writer.WritePropertyName("Y");
                _writer.WriteValue(PerInstanceData.Transform.M01);
                _writer.WritePropertyName("Z");
                _writer.WriteValue(PerInstanceData.Transform.M02);
                _writer.WritePropertyName("W");
                _writer.WriteValue(PerInstanceData.Transform.M03);
                _writer.WriteEndObject();

                _writer.WritePropertyName("YPlane");
                _writer.WriteStartObject();
                _writer.WritePropertyName("X");
                _writer.WriteValue(PerInstanceData.Transform.M10);
                _writer.WritePropertyName("Y");
                _writer.WriteValue(PerInstanceData.Transform.M11);
                _writer.WritePropertyName("Z");
                _writer.WriteValue(PerInstanceData.Transform.M12);
                _writer.WritePropertyName("W");
                _writer.WriteValue(PerInstanceData.Transform.M13);
                _writer.WriteEndObject();

                _writer.WritePropertyName("ZPlane");
                _writer.WriteStartObject();
                _writer.WritePropertyName("X");
                _writer.WriteValue(PerInstanceData.Transform.M20);
                _writer.WritePropertyName("Y");
                _writer.WriteValue(PerInstanceData.Transform.M21);
                _writer.WritePropertyName("Z");
                _writer.WriteValue(PerInstanceData.Transform.M22);
                _writer.WritePropertyName("W");
                _writer.WriteValue(PerInstanceData.Transform.M23);
                _writer.WriteEndObject();

                _writer.WritePropertyName("WPlane");
                _writer.WriteStartObject();
                _writer.WritePropertyName("X");
                _writer.WriteValue(PerInstanceData.Transform.M30);
                _writer.WritePropertyName("Y");
                _writer.WriteValue(PerInstanceData.Transform.M31);
                _writer.WritePropertyName("Z");
                _writer.WriteValue(PerInstanceData.Transform.M32);
                _writer.WritePropertyName("W");
                _writer.WriteValue(PerInstanceData.Transform.M33);
                _writer.WriteEndObject();

                _writer.WriteEndObject();  // Transform

                _writer.WriteEndObject();
            }

            _writer.WriteEndArray();  // PerInstanceSMData

            // Enable instances selection for ISM and HISM 
            _writer.WritePropertyName("bHasPerInstanceHitProxies");
            _writer.WriteValue(true);
        }
    }
}
