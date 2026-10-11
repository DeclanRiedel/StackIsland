using System.IO;
using UnityEditor;
using UnityEngine;

namespace StackIsland.EditorTools
{
    /// <summary>Menu / batch entry for the balance simulator (<see cref="BalanceModel"/>).
    /// Batch: -executeMethod StackIsland.EditorTools.BalanceSim.RunBatch</summary>
    public static class BalanceSim
    {
        [MenuItem("Stack Island/Run Balance Sim")]
        public static void RunMenu() => Debug.Log(BalanceModel.Run());

        public static void RunBatch()
        {
            var text = BalanceModel.Run();
            Directory.CreateDirectory("Tools/BalanceSim");
            File.WriteAllText("Tools/BalanceSim/balance.txt", text);
            foreach (var line in text.Split('\n')) if (line.Length > 0) Debug.Log(line);
        }
    }
}
