using TMPro;
using UnityEngine;
using UnityEngine.Timeline;

namespace Features.UI
{
    /// <summary>
    /// this class is for revealing text in cutscenes
    /// </summary>
    public class CutsceneTextScrambleReveal:MonoBehaviour,ITimeControl
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float charsPerSecond = 23f;
        [SerializeField] private int scrambleWindow = 4;

        private string resolved = "";
        private char[] pool;

        private readonly System.Text.StringBuilder sb = new();
        private readonly System.Random rnd = new();
        
        public void SetTime(double time) => Render((float)time);

        public void OnControlTimeStart()
        {
            resolved = label.text;
            BuildPool();
        }

        public void OnControlTimeStop()=> label.text = resolved;

        private void BuildPool()
        {
            if (pool != null) return;
            var table = label.font.characterTable;
            var chars = new System.Collections.Generic.List<char>(table.Count);
            foreach (var c in table)
            {
                if (c.unicode > 0xFFFF) continue;
                char ch = (char)c.unicode;
                if(char.IsWhiteSpace(ch) || char.IsControl(ch)) continue;
                chars.Add(ch);
            }
            pool = chars.ToArray();
        }

        private void Render(float elapsed)
        {
            int n = resolved.Length;
            int settled = Mathf.Clamp(Mathf.FloorToInt(elapsed * charsPerSecond), 0, n);
            int visibleEnd = Mathf.Min(settled+scrambleWindow, n);

            sb.Clear();
            for (int i = 0; i < visibleEnd; i++)
            {
                if (i < settled)
                {
                    sb.Append(resolved[i]);
                } else if (char.IsWhiteSpace(resolved[i]))
                {
                    sb.Append(resolved[i]);
                }
                else
                {
                    sb.Append(pool[rnd.Next(pool.Length)]);
                }
            }
            label.text = sb.ToString();
        }
    }
}