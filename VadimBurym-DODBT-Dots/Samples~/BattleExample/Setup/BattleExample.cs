using System;
using DODBT.Example.Battle.Grid;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DODBT.Example
{
    [DisallowMultipleComponent]
    public sealed class BattleExample : MonoBehaviour
    {
        public UnitDefinition Footman;
        public UnitDefinition Orc;
        public BattlefieldGrid Grid;
        [Min(1)] public int UnitsPerArmy = 5000;
        [Min(1)] public int Columns = 100;
        [Min(.1f)] public float Spacing = 1;
        public Vector3 FirstArmyOrigin = Vector3.zero;
        public Vector3 SecondArmyOrigin = new(0, 0, 57);
        public uint Seed = 12345;
        public BattleSession Session { get; private set; }

        private void Start() => StartBattle();

        public void StartBattle()
        {
            StopBattle();
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
            {
                Debug.LogWarning("DODBT Battle Example requires URP. Open its README for setup; project rendering settings are not changed automatically.", this);
                return;
            }
            if (Footman == null || Orc == null || Grid == null)
                throw new InvalidOperationException("Assign both unit definitions and the battlefield grid.");
            if (UnitsPerArmy < 1 || UnitsPerArmy > 100000 || Columns < 1 || Spacing <= 0)
                throw new InvalidOperationException("Invalid army size or formation spacing.");
            Footman.Validate();
            Orc.Validate();
            Session = new BattleSession(this);
        }

        public void StopBattle()
        {
            Session?.Dispose();
            Session = null;
        }

        private void OnDestroy() => StopBattle();
    }
}
