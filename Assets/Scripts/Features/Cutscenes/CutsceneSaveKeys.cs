
namespace Features.Cutscenes
{
    public static class CutsceneSaveKeys
    {
        //string to put as a key into save file
        public static string Played(string cutsceneId) => $"cutscene.{cutsceneId}.played";
    }
}