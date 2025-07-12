using CUE4Parse.Utils;

namespace BlueprintDumper_UE5.Utils
{
    public static class UnrealHelpers
    {
        public static string GetAssetName(this string AssetPath)
        {
            if (AssetPath.Contains('/'))  // if AssetPath in the following format: /Game/Meshes/SM_MyMesh
            {
                return AssetPath.SubstringBeforeLast('.').SubstringAfterLast('/');
            }
            else if (AssetPath.Contains('\\'))  // if AssetPath in the following format: ...\Content\Meshes\SM_MyMesh.uasset
            {
                return AssetPath.SubstringAfterLast('\\').SubstringBeforeLast('.');
            }
            return string.Empty;
        }

        public static string GetPackagePathFromPackageName(this string PackageName)
        {
            return "DeadByDaylight/Content/" + PackageName.Substring(6);
        }

        public static string GetPackageNameFromFilePath(this string FilePath)
        {
            return "/Game" + FilePath.SubstringAfter("Content").SubstringBeforeLast('.').Replace('\\', '/');
        }

        public static string[] GetAllPackages(this string AssetFilePath)
        {
            string AssetDirectory = AssetFilePath.SubstringBeforeLast('\\');
            string AssetFileName = AssetFilePath.SubstringAfterLast('\\').SubstringBeforeLast('.');

            return Directory.GetFiles(AssetDirectory, AssetFileName);
        }
    }
}
