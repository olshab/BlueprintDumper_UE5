using BlueprintDumper_UE5.Serialization;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.UObject;

namespace BlueprintDumper_UE5.Extensions.Components
{
    public class UActorComponent
    {
        public readonly string ClassName;
        public readonly bool IsSceneComponent;

        protected readonly UObject _object;

        private readonly string[] ComponentClassesWhitelist = [
            "/Script/Engine.ArrowComponent",
            "/Script/Engine.ChildActorComponent",
            "/Script/Engine.DecalComponent",
            "/Script/Engine.HierarchicalInstancedStaticMeshComponent",
            "/Script/Engine.InstancedStaticMeshComponent",
            "/Script/Engine.ParticleSystemComponent",
            "/Script/Engine.PointLightComponent",
            "/Script/Engine.PostProcessComponent",
            "/Script/Engine.SceneComponent",
            "/Script/Engine.SkeletalMeshComponent",
            "/Script/Engine.SphereComponent",
            "/Script/Engine.SpotLightComponent",
            "/Script/Engine.StaticMeshComponent",
        ];

        public static UActorComponent CreateComponent(UObject Object, string ComponentClass)
        {
            return ComponentClass switch
            {
                "/Script/Engine.InstancedStaticMeshComponent" => new UInstancedStaticMeshComponent(Object, ComponentClass),
                "/Script/Engine.HierarchicalInstancedStaticMeshComponent" => new UInstancedStaticMeshComponent(Object, ComponentClass),
                "/Script/Engine.FoliageInstancedStaticMeshComponent" => new UInstancedStaticMeshComponent(Object, ComponentClass),
                "/Script/DeadByDaylight.ActorSpawner" => new UActorSpawner(Object, ComponentClass),
                
                _ => new UActorComponent(Object, ComponentClass)
            };
        }

        public UActorComponent(UObject Object, string ComponentClass)
        {
            _object = Object;
            ClassName = ComponentClass;
            IsSceneComponent = !ComponentClassesWhitelist.Contains(ComponentClass);
        }

        public virtual void Serialize(PropertySerializer Serializer)
        {
            foreach (var Property in _object.Properties)
            {
                if (IsSceneComponent)
                {
                    if (Serializer.IsSceneComponentProperty(Property))
                    {
                        Serializer.SerializePropertyValue(Property.Tag!, Property.Name.ToString());
                    }
                }
                else
                {
                    Serializer.SerializePropertyValue(Property.Tag!, Property.Name.ToString());
                }
            }
        }

        public void AddComponentTag(string TagName)
        {
            var ComponentTagsProperty = _object.Properties
                .FirstOrDefault(x => x.Name.ToString() == "ComponentTags")?.Tag as ArrayProperty;
            if (ComponentTagsProperty is null)
            {
                ComponentTagsProperty = new ArrayProperty(null!, null, ReadType.ZERO);
                var ComponentTagsPropertyTag = new FPropertyTag();
                ComponentTagsPropertyTag.Name = new FName("ComponentTags");
                ComponentTagsPropertyTag.Tag = ComponentTagsProperty;
                _object.Properties.Add(ComponentTagsPropertyTag);
            }

            var TagProperty = new NameProperty(null!, ReadType.ZERO);
            TagProperty.Value = new FName(TagName);

            ComponentTagsProperty.Value!.Properties.Add(TagProperty);
        }

        public void ClearComponentTags()
        {
            var ComponentTagsProperty = _object.Properties
                .FirstOrDefault(x => x.Name.ToString() == "ComponentTags")?.Tag as ArrayProperty;
            if (ComponentTagsProperty is null)
            {
                return;
            }
            ComponentTagsProperty.Value!.Properties.Clear();
        }

        public bool HasAnyTags(string[] Tags)
        {
            var ComponentTagsProperty = _object.Properties
                .FirstOrDefault(x => x.Name.ToString() == "ComponentTags")?.Tag as ArrayProperty;
            if (ComponentTagsProperty is null)
            {
                return false;
            }

            foreach (var ElementProperty in ComponentTagsProperty.Value!.Properties)
            {
                var TagProperty = ElementProperty as NameProperty;
                if (Tags.Contains(TagProperty!.Value.ToString()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
