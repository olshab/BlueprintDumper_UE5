using CUE4Parse.Utils;

namespace BlueprintDumper_UE5
{
    public class DumpSettings
    {
        public string DumpFolder = string.Empty;
        public string DumpList = string.Empty;
        public string PaksDirectory = string.Empty;
        public string EngineVersion = string.Empty;
        public string MappingsFilepath = string.Empty;
        public string AESKey = string.Empty;
        public string ProjectDirectory = string.Empty;
        public bool bScanProjectForReferencedAssets = true;
        public string AssetsPackagePathToScanAt = string.Empty;




        public bool bChangePathToStaticMeshes = true;  // for tiles
        public bool bChangePathToBlueprints = true;  // for tiles
        public bool bConvertActorSpawnerIntoChildActor = false;  // for tiles
        public bool bExportReferencedMeshes = true;
        public bool bExportReferencedMaterials = true;
        public bool bExportTextures = true;
        public bool bIgnoreExistingAssetsAtPath = true;
        public bool bListMaterialsWithTextures = true;


        public string BlueprintsDirectory = "/Game/NewTiles/Blueprints/_buffer";
        public string NewPathToMeshes = "/Game/NewTiles/Meshes/_buffer";  // used only if bChangePathToStaticMeshes set to True
        public string NewPathToBlueprints = "/Game/NewTiles/Blueprints/_buffer";  // used only if bChangePathToBlueprints set to True
        // used only if bScanProjectForReferencedAssets and bIgnoreExistingAssetsAtPath set to True
        public List<string> IgnoreExistingAssetsAtPath = [];

        public string CustomTag = "OriginalTiles";
        public string GenerateAtPath = "/Game/OldTiles";

        public string[] PossibleBaseColorParameterNames = {
            "BaseColor",
            "Diffuse",
            "BlueChannel_Texture",  // from MI_GroundWall
        };

        // in case we want to use assets which already exist in game
        public string[] DontChangePathForPackages = {

        };

        public DumpSettings()
        {

        }
    }
}
