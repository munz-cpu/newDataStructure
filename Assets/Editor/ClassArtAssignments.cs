using UnityEditor;

public static class ClassArtAssignments
{
    [MenuItem("DivineWar/Art/Assign Supplied Characters")]
    public static void Assign() =>
        AssetDatabase.ImportAsset(PlayerAvatarImporter.CataloguePath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

    [MenuItem("DivineWar/Validation/Supplied Class Art")]
    public static void ValidateBatch()
    {
        Assign();
        ClassCombatChecks.Batch();
    }
}
