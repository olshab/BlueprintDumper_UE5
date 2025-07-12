namespace BlueprintDumper_UE5
{
    public static class Program
    {
        static void Main(string[] args)
        {
            var Settings = new DumpSettings()
            {
                DumpFolder = @"C:\Users\Oleg\Desktop\BlueprintDumper",
                DumpList = @"C:\Users\Oleg\Desktop\BlueprintReferenceViewer\Level_1.txt",
                //PaksDirectory = @"C:\Program Files\Epic Games\DeadByDaylight\DeadByDaylight\Content\Paks",
                //EngineVersion = "GAME_DeadByDaylight",
                PaksDirectory = @"E:\3.0.0\DeadByDaylight\Content\Paks",
                EngineVersion = "GAME_UE4_21",
                MappingsFilepath = @"C:\Users\Oleg\Mappings.usmap",
                AESKey = "0x22B1639B548124925CF7B9CBAA09F9AC295FCF0324586D6B37EE1D42670B39B3",
                
                CustomTag = "OriginalTiles",
                ProjectDirectory = @"C:\Users\Oleg\Desktop\DBDOldTiles",
                bScanProjectForReferencedAssets = false,
                AssetsPackagePathToScanAt = "/Game/OriginalTiles",
            };

            var Dumper = new BlueprintDumper(Settings);
            bool ExecuteResult = Dumper.Execute();

            return;
        }
    }
}
