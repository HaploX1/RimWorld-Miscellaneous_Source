using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;

namespace Incidents
{
    public class CustomRumorOfDef : Def
    {
        public float occureChance = 20f;
        public float treasureChance = 90f;

        public List<TreasureCollection> treasureDefs;

        public float daysPawnsAreGoneMin;
        public float daysPawnsAreGoneMax;

        public int enemySpawnChanceTreasure;
        public int enemySpawnChanceNoTreasure;
        public int enemySpawnTimeMin;
        public int enemySpawnTimeMax;

        public List<PawnKindDef> pawnFoundDefs;
        public int pawnFoundChance = 0;
        public bool pawnWithEquipment = false;

        public string InitialMessageVariable;
        public string InitialMessageButtonAcceptVariable;
        public string InitialMessageButtonRejectVariable;
        public string InitialMessageRejectedVariable;
        public string SelectPawnsMessageVariable;
        public string SelectPawnsMessage_ButtonAssignVariable;
        public string SelectPawnsMessage_ButtonAssignErrorVariable;
        public string SelectPawnsMessage_ButtonUnassignVariable;
        public string SelectPawnsMessage_ButtonAbortVariable;
        public string SelectPawnsMessage_ButtonPostponeVariable;
        public string SelectPawnsMessage_ButtonSendVariable;
        public string SelectPawnsMessage_ButtonSendErrorVariable;
        public string LetterLabel_ReturnedVariable;
        public string LetterMessage_ReturnedBaseVariable;
        public string LetterMessage_ReturnedNoTreasureVariable;
        public string LetterMessage_ReturnedWithTreasureVariable;
        public string LetterMessage_ReturnedWithNewColonistVariable;
        public string LetterMessage_ReturnedWithEnemyVariable;

    }
}
