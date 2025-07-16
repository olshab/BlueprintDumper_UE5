using BlueprintDumper_UE5.Extensions;
using BlueprintDumper_UE5.Serialization;
using BlueprintDumper_UE5.Utils;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.Textures.BC;
using System.Reflection;

namespace BlueprintDumper_UE5
{
    public class BlueprintDumper
    {
        private readonly DumpSettings _dumpSettings;

        private readonly IFileProvider _provider;

        private readonly string[] _blueprintsToDump;

        private List<string> AlreadyExportedTextures = [];  // used in ExportTexturesFromMaterial method
        private List<string> AlreadyListedMaterials = [];

        // used in ExportReferencedMeshes/Blueprints: pairs AssetName - PackagePath (i.e. SM_Mesh - /Game/Meshes/SM_Mesh)
        private readonly Dictionary<string, string> AlreadyExistingAssets = [];

        private IPackage? _currentPackage;

        private HashSet<string> ReferencedAssets = [];
        private HashSet<string> ReferencedBlueprints = [];
        private HashSet<string> ReferencedStaticMeshes = [];
        private HashSet<string> ReferencedMaterials = [];

        public BlueprintDumper(DumpSettings DumpSettings)
        {
            _dumpSettings = DumpSettings;

            string OodleBinaryFilepath = GetDll("oo2core_9_win64.dll");
            OodleHelper.Initialize(OodleBinaryFilepath);
            string ZlibBinaryFilepath = GetDll("zlib-ng2.dll");
            ZlibHelper.Initialize(ZlibBinaryFilepath);
            string DetexBinaryFilepath = GetDll("Detex.dll");
            DetexHelper.Initialize(DetexBinaryFilepath);

            _provider = InitializeProvider();





            //var TestPackage = _provider.LoadPackageObject("DeadByDaylight/Content/Blueprints/BP_DBDGameInstance.BP_DBDGameInstance_C");
            //return;







            if (_dumpSettings.bScanProjectForReferencedAssets)
            {
                AlreadyExistingAssets = GetProjectAssets();
            }

            _blueprintsToDump = File.ReadAllLines(_dumpSettings.DumpList);
            if (Directory.Exists(_dumpSettings.DumpFolder))
            {
                Directory.Delete(_dumpSettings.DumpFolder, true);
            }
            Directory.CreateDirectory(_dumpSettings.DumpFolder);
        }

        public bool Execute()
        {
            foreach (var BlueprintPackagePath in _blueprintsToDump)
            {
                DumpBlueprint(BlueprintPackagePath);
            }

            // Save the lists of referenced assets
            if (!Directory.Exists($"{_dumpSettings.DumpFolder}\\Lists"))
            {
                Directory.CreateDirectory($"{_dumpSettings.DumpFolder}\\Lists");
            }            
            SaveReferencedAssetsList(ReferencedBlueprints, $"{_dumpSettings.DumpFolder}\\Lists\\ReferencedBlueprints.txt");
            SaveReferencedAssetsList(ReferencedStaticMeshes, $"{_dumpSettings.DumpFolder}\\Lists\\ReferencedStaticMeshes.txt");
            SaveReferencedAssetsList(ReferencedMaterials, $"{_dumpSettings.DumpFolder}\\Lists\\ReferencedMaterials.txt");

            if (_dumpSettings.bExportReferencedMeshes)
            {
                ExportReferencedMeshes();
            }
            if (_dumpSettings.bExportReferencedMaterials && _dumpSettings.bExportTextures)
            {
                ExportReferencedMaterials();
            }

            return true;
        }

        private static string GetDll(string DllFilename)
        {
            string TempDllPath = Path.Combine(Path.GetTempPath(), DllFilename);
            if (!File.Exists(TempDllPath))
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"BlueprintDumper_UE5.Resources.{DllFilename}");
                if (stream == null)
                    throw new Exception($"Couldn't find {DllFilename} in Embedded Resources");
                var ba = new byte[(int)stream.Length];
                _ = stream.Read(ba, 0, (int)stream.Length);
                File.WriteAllBytes(TempDllPath, ba);
            }
            return TempDllPath;
        }

        private IFileProvider InitializeProvider()
        {
            var ParseResult = Enum.TryParse(_dumpSettings.EngineVersion, out EGame CUE4Parse_GameVersion);
            if (!ParseResult)
            {
                throw new Exception($"Failed to parse UE game version {_dumpSettings.EngineVersion}");
            }

            var VersionContainer = new VersionContainer(CUE4Parse_GameVersion);
            var Provider = new DefaultFileProvider(_dumpSettings.PaksDirectory, SearchOption.TopDirectoryOnly,
                VersionContainer, StringComparer.OrdinalIgnoreCase);
            Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(_dumpSettings.MappingsFilepath);
            Provider.Initialize();
            Provider.SubmitKey(new FGuid(), new FAesKey(_dumpSettings.AESKey));
            Provider.PostMount();
            Provider.LoadVirtualPaths();

            return Provider;
        }

        private Dictionary<string, string> GetProjectAssets()
        {
            Dictionary<string, string> ProjectAssets = [];
            if (!Directory.Exists(_dumpSettings.ProjectDirectory))
            {
                throw new Exception("Project directory doesn't exist. Uncheck bScanProjectForReferencedAssets");
            }

            string[] ProjectUAssetFiles = Directory.GetFiles($"{_dumpSettings.ProjectDirectory}\\Content", "*.uasset", SearchOption.AllDirectories);
            foreach (string ProjectAssetPath in ProjectUAssetFiles)
            {
                string PackageName = ProjectAssetPath.GetPackageNameFromFilePath();
                string AssetPath = "/Game" + ProjectAssetPath.SubstringAfter("Content").SubstringBeforeLast('.').Replace('\\', '/');
                if (AssetPath.StartsWith(_dumpSettings.AssetsPackagePathToScanAt))
                {
                    if (ProjectAssets.ContainsKey(ProjectAssetPath.GetAssetName()))
                    {
                        throw new Exception($"Two assets with the same name: {ProjectAssets[ProjectAssetPath.GetAssetName()]} and {AssetPath}");
                    }
                    ProjectAssets.Add(ProjectAssetPath.GetAssetName(), AssetPath);
                }
            }
            return ProjectAssets;
        }

        private void DumpBlueprint(string BlueprintPackagePath)
        {
            bool PackageFound = _provider.TryLoadPackage(BlueprintPackagePath, out _currentPackage);
            if (!PackageFound)
            {
                return;
            }
            Console.WriteLine($"Dumping blueprint {_currentPackage!.Name}");
            var PackageExports = _currentPackage.GetExports();

            var BlueprintGeneratedClass = PackageExports.First(x => x.ExportType == "BlueprintGeneratedClass");
            UBlueprintGeneratedClass BPGC = new(BlueprintGeneratedClass);

            // Assuming this blueprint is child of AActor
            if (!BPGC.HasAnyComponents())
            {
                return;
            }

            var Serializer = new BlueprintSerializer(BlueprintGeneratedClass, _dumpSettings.CustomTag);
            Serializer.SerializeBlueprintNameAndParent(AlreadyExistingAssets);

            Serializer.Serialize(BPGC.SimpleConstructionScript);
            Serializer.Serialize(BPGC.InheritableComponentHandler);
            ReferencedAssets = Serializer.ReferencedPackages;

            ListReferencedAssets("/Script/Engine.BlueprintGeneratedClass", ReferencedBlueprints);
            ListReferencedAssets("/Script/Engine.StaticMesh", ReferencedStaticMeshes);
            ListReferencedAssets("/Script/Engine.MaterialInstanceConstant", ReferencedMaterials);

            Serializer.SaveToDisk($"{_dumpSettings.DumpFolder}\\{_currentPackage.Name.GetAssetName()}.json");
        }

        private void ListReferencedAssets(string AssetType, HashSet<string> OutReferencedAssets)
        {
            for (int ImportIndex = 0; ImportIndex < _currentPackage!.ImportMapLength; ++ImportIndex)
            {
                var PackageIndex = new FPackageIndex(_currentPackage, -ImportIndex - 1);
                var ResolvedImport = PackageIndex.ResolvedObject;
                if (ResolvedImport is null)
                {
                    continue;
                }

                if (ResolvedImport.Class!.GetPathName() == AssetType && ReferencedAssets.Contains(ResolvedImport.GetPathName()))
                {
                    OutReferencedAssets.Add(ResolvedImport.GetPathName());
                }
            }
        }

        private static void SaveReferencedAssetsList(HashSet<string> ReferencedAsset, string FilePath)
        {
            File.WriteAllLines(FilePath, ReferencedAsset.OrderBy(x => x).ToList());
        }

        private void ExportReferencedMeshes()
        {
            string ExportDirectory = $"{_dumpSettings.DumpFolder}\\Intermediates";
            if (!Directory.Exists(ExportDirectory))
            {
                Directory.CreateDirectory(ExportDirectory);
            }

            foreach (string MeshPath in ReferencedStaticMeshes.Order())
            {
                string AssetName = MeshPath.GetAssetName();

                if (AlreadyExistingAssets.ContainsKey(AssetName))
                {
                    Console.WriteLine($"Skipping {AssetName} because it already exists in project");
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Exporting mesh {AssetName}...");
                Console.ForegroundColor = ConsoleColor.Gray;

                var StaticMesh = _provider.LoadPackageObject<UStaticMesh>(MeshPath);

                var ExporterOptions = new ExporterOptions
                {
                    ExportMaterials = false
                };
                var exporter = new Exporter(StaticMesh, ExporterOptions);
                var directory = new DirectoryInfo(ExportDirectory);
                exporter.TryWriteToDir(directory, out string label, out string savedFilePath);

                if (_dumpSettings.bExportTextures)
                {
                    string TextureDumpsDirectory = $"{_dumpSettings.DumpFolder}\\Textures";

                    if (!Directory.Exists(TextureDumpsDirectory))
                    {
                        Directory.CreateDirectory(TextureDumpsDirectory);
                    }
                    foreach (ResolvedObject? Material in StaticMesh.Materials)
                    {
                        if (Material is not null)
                        {
                            UMaterialInterface? MaterialInterface = Material.Load<UMaterialInterface>();
                            if (MaterialInterface is not null)
                            {
                                ExportTexturesFromMaterial(MaterialInterface, TextureDumpsDirectory);
                            }
                        }
                    }
                }
            }

            string MeshesExportsDirectory = $"{_dumpSettings.DumpFolder}\\Meshes";

            string[] PskFiles = Directory.GetFiles(ExportDirectory, "*.pskx", SearchOption.AllDirectories);
            foreach (string PskFile in PskFiles)
            {
                if (!Directory.Exists(MeshesExportsDirectory))
                {
                    Directory.CreateDirectory(MeshesExportsDirectory);
                }
                string NewDirectory = MeshesExportsDirectory + PskFile.SubstringAfterWithLast('\\');
                try
                {
                    File.Move(PskFile, NewDirectory);
                }
                catch { }
            }

            Directory.Delete(ExportDirectory, true);
        }

        private void ExportReferencedMaterials()
        {
            foreach (string MaterialPath in ReferencedMaterials.Order())
            {
                string AssetName = MaterialPath.GetAssetName();

                if (AlreadyExistingAssets.ContainsKey(AssetName))
                {
                    Console.WriteLine($"Skipping {AssetName} because it already exists in project");
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Exporting material {AssetName}...");
                Console.ForegroundColor = ConsoleColor.Gray;

                string TextureDumpsDirectory = $"{_dumpSettings.DumpFolder}\\Textures";
                if (!Directory.Exists(TextureDumpsDirectory))
                {
                    Directory.CreateDirectory(TextureDumpsDirectory);
                }

                var MaterialInterface = _provider.LoadPackageObject<UMaterialInterface>(MaterialPath);
                if (MaterialInterface is not null)
                {
                    ExportTexturesFromMaterial(MaterialInterface, TextureDumpsDirectory);
                }
            }
        }

        private void ExportTexturesFromMaterial(UMaterialInterface StaticMaterial, string exportDirectory)
        {
            if (AlreadyExistingAssets.ContainsKey(StaticMaterial.Name) || AlreadyListedMaterials.Contains(StaticMaterial.Name))
            {
                return;
            }
            if (StaticMaterial is UMaterialInstanceConstant MI)
            {
                List<string> DiffuseTextures = new List<string>();

                string MaterialsAndTexturesFilePath = $"{_dumpSettings.DumpFolder}\\MaterialsAndTextures.txt";

                if (!File.Exists(MaterialsAndTexturesFilePath))
                {
                    File.Create(MaterialsAndTexturesFilePath).Close();
                }
                List<string> Materials = File.ReadAllLines(MaterialsAndTexturesFilePath).ToList();
                Materials.Add(MI.Name);

                foreach (FTextureParameterValue TextureValue in MI.TextureParameterValues)
                {
                    bool MainBaseColorExported = false;
                    foreach (string PossibleBaseColor in _dumpSettings.PossibleBaseColorParameterNames)
                    {
                        if (TextureValue.Name.ToLower().Replace(" ", "").Contains(PossibleBaseColor.ToLower()))
                        {
                            string AssetName = string.Empty;
                            if (!MainBaseColorExported)
                            {
                                var Texture = TextureValue.ParameterValue.Load<UTexture2D>();
                                if (Texture is null)
                                {
                                    continue;
                                }
                                MainBaseColorExported = true;
                                AssetName = Texture!.Name;
                                if (AlreadyExistingAssets.ContainsKey(AssetName))
                                {
                                    DiffuseTextures.Add(AlreadyExistingAssets[AssetName]);
                                }
                                else
                                {
                                    DiffuseTextures.Add(AssetName);
                                    ExportTexture(Texture, AssetName, exportDirectory);
                                }
                            }                            

                            Materials.Add($"\t\"{TextureValue.Name}\": \"{AssetName}\"");
                        }
                    }
                }

                Materials.Add("");
                File.WriteAllLines(MaterialsAndTexturesFilePath, Materials);

                AlreadyListedMaterials.Add(MI.Name);

                string MaterialsDirectory = $"{_dumpSettings.DumpFolder}\\Materials";
                if (!Directory.Exists(MaterialsDirectory))
                {
                    Directory.CreateDirectory(MaterialsDirectory);
                }
                File.WriteAllLines($"{MaterialsDirectory}\\{MI.Name}.txt", DiffuseTextures);
            }
        }

        private void ExportTexture(UTexture2D Texture, string AssetName, string ExportDirectory)
        {
            if (AlreadyExistingAssets.Keys.Contains(AssetName))
            {
                Console.WriteLine($"Skipping {AssetName} because it already exists in project");
                return;
            }

            if (AlreadyExportedTextures.Contains(AssetName))
                return;

            AlreadyExportedTextures.Add(AssetName);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Exporting texture {AssetName}...");
            Console.ForegroundColor = ConsoleColor.Gray;

            using FileStream fs = new FileStream($"{ExportDirectory}\\{AssetName}.png", FileMode.Create, FileAccess.Write);

            var Bitmap = Texture.Decode()?.ToSkBitmap();
            if (Bitmap is not null)
            {
                using var Data = Bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                using var Stream = Data.AsStream();
                Stream.CopyTo(fs);
            }
        }
    }
}
