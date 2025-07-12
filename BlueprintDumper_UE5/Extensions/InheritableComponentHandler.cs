using BlueprintDumper_UE5.Extensions.Components;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace BlueprintDumper_UE5.Extensions
{
    public class UInheritableComponentHandler
    {
        public readonly List<FComponentOverrideRecord>? Records;

        private readonly UObject _object;

        public UInheritableComponentHandler(UObject Object)
        {
            _object = Object;
            Records = GetRecords();
        }

        private List<FComponentOverrideRecord>? GetRecords()
        {
            var RecordsProperty = _object.Properties.FirstOrDefault(x => x.Name == "Records")?.Tag as ArrayProperty;
            if (RecordsProperty is null || RecordsProperty?.Value?.Properties is null)
            {
                return null;
            }

            List<FComponentOverrideRecord> Records = new();

            foreach (var Record in RecordsProperty.Value.Properties)
            {
                var ComponentOverrideRecordProperty = Record as StructProperty;
                Records.Add(new FComponentOverrideRecord(ComponentOverrideRecordProperty!.Value!.StructType as FStructFallback));
            }
            return Records;
        }
    }

    public class FComponentOverrideRecord
    {
        public readonly ResolvedObject? ComponentClass;
        public readonly UActorComponent ComponentTemplate;
        public readonly FComponentKey ComponentKey;

        public FComponentOverrideRecord(FStructFallback? ComponentOverrideRecord)
        {
            if (ComponentOverrideRecord is null)
            {
                throw new Exception("ComponentOverrideRecords was null");
            }

            var ComponentClassProperty = ComponentOverrideRecord.Properties
                .First(x => x.Name == "ComponentClass").Tag as ObjectProperty;
            ComponentClass = ComponentClassProperty!.Value!.ResolvedObject;

            var ComponentTemplateProperty = ComponentOverrideRecord.Properties
                .First(x => x.Name == "ComponentTemplate").Tag as ObjectProperty;
            ComponentTemplate = UActorComponent.CreateComponent(ComponentTemplateProperty!.Value!.Load()!, ComponentClass!.GetPathName());

            var ComponentKeyProperty = ComponentOverrideRecord.Properties
                .First(x => x.Name == "ComponentKey").Tag as StructProperty;
            ComponentKey = new FComponentKey(ComponentKeyProperty!.Value!.StructType as FStructFallback);
        }
    }

    public class FComponentKey
    {
        public readonly ResolvedObject? OwnerClass;
        public readonly string SCSVariableName;
        public readonly FGuid AssociatedGuid;

        public FComponentKey(FStructFallback? ComponentKey)
        {
            if (ComponentKey is null)
            {
                throw new Exception("ComponentKey was null");
            }

            var OwnerClassProperty = ComponentKey.Properties
                .First(x => x.Name == "OwnerClass").Tag as ObjectProperty;
            OwnerClass = OwnerClassProperty!.Value!.ResolvedObject;

            var SCSVariableNameProperty = ComponentKey.Properties
                .First(x => x.Name == "SCSVariableName").Tag as NameProperty;
            SCSVariableName = SCSVariableNameProperty!.Value.ToString();

            var AssociatedGuidProperty = ComponentKey.Properties
                .First(x => x.Name == "AssociatedGuid").Tag as StructProperty;
            AssociatedGuid = (FGuid)AssociatedGuidProperty!.Value!.StructType;
        }
    }
}
