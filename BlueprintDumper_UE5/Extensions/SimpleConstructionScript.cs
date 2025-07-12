using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects.Properties;

namespace BlueprintDumper_UE5.Extensions
{
    public class USimpleConstructionScript
    {
        public readonly List<USCS_Node> RootNodes;
        public readonly List<USCS_Node> AllNodes;
        public readonly USCS_Node DefaultSceneRoot;

        private readonly UObject _object;

        public USimpleConstructionScript(UObject Object)
        {
            _object = Object;
            RootNodes = GetRootNodes();
            AllNodes = GetAllNodes();
            DefaultSceneRoot = GetDefaultSceneRoot();
        }

        private List<USCS_Node> GetRootNodes()
        {
            var RootNodesProperty = _object.Properties.First(x => x.Name.Text == "RootNodes").Tag as ArrayProperty;
            if (RootNodesProperty is null || RootNodesProperty?.Value?.Properties is null)
            {
                throw new Exception($"Failed to find `RootNodes` property inside {_object.GetPathName()}");
            }

            List<USCS_Node> RootNodes = new();

            foreach (var RootNode in RootNodesProperty.Value?.Properties!)
            {
                var RootNodeProperty = RootNode as ObjectProperty;
                var RootNodeObject = RootNodeProperty?.Value?.ResolvedObject?.Load();

                if (RootNodeObject is null)
                {
                    continue;
                }
                RootNodes.Add(new USCS_Node(RootNodeObject));
            }
            return RootNodes;
        }

        private List<USCS_Node> GetAllNodes()
        {
            var AllNodesProperty = _object.Properties.First(x => x.Name.Text == "AllNodes").Tag as ArrayProperty;
            if (AllNodesProperty is null || AllNodesProperty?.Value?.Properties is null)
            {
                throw new Exception($"Failed to find `AllNodes` property inside {_object.GetPathName()}");
            }

            List<USCS_Node> AllNodes = new();

            foreach (var AllNode in AllNodesProperty.Value?.Properties!)
            {
                var AllNodeProperty = AllNode as ObjectProperty;
                var AllNodeObject = AllNodeProperty?.Value?.ResolvedObject?.Load();

                if (AllNodeObject is null)
                {
                    continue;
                }
                AllNodes.Add(new USCS_Node(AllNodeObject));
            }
            return AllNodes;
        }

        private USCS_Node GetDefaultSceneRoot()
        {
            var DefaultSceneRootProperty = _object.Properties
                .First(x => x.Name.Text == "DefaultSceneRootNode").Tag as ObjectProperty;
            if (DefaultSceneRootProperty is null || DefaultSceneRootProperty?.Value is null)
            {
                throw new Exception($"Failed to find `DefaultSceneRoot` property inside {_object.GetPathName()}");
            }

            var DefaultSceneRootObject = DefaultSceneRootProperty.Value.ResolvedObject?.Load();
            if (DefaultSceneRootObject is null)
            {
                throw new Exception($"Could not load DefaultSceneRoot object for {_object.GetPathName()}");
            }
            return new USCS_Node(DefaultSceneRootObject);
        }
    }
}
