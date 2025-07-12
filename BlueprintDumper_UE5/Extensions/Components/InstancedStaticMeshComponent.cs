using BlueprintDumper_UE5.Serialization;
using CUE4Parse.UE4.Assets.Exports;

namespace BlueprintDumper_UE5.Extensions.Components
{
    public class UInstancedStaticMeshComponent : UActorComponent
    {
        public CUE4Parse.UE4.Assets.Exports.Component.StaticMesh.UInstancedStaticMeshComponent InstancedStaticMesh
            => (CUE4Parse.UE4.Assets.Exports.Component.StaticMesh.UInstancedStaticMeshComponent)_object;

        public UInstancedStaticMeshComponent(UObject Object, string ComponentClass)
            : base(Object, ComponentClass)
        { }

        public override void Serialize(PropertySerializer Serializer)
        {
            base.Serialize(Serializer);
            Serializer.InstancedStaticMesh_BulkSerialize(InstancedStaticMesh);
        }
    }
}
