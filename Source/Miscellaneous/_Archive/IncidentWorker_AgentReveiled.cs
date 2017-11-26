using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound


namespace AgentReveiled
{
    /// <summary>
    /// This is the incident "Agent Reveiled".
    /// A colonist is revealed to be an agent of an enemy faction.
    /// He/she will try to escape... (And remains to have the keys to do so.. ;-) )
    /// </summary>
    /// <author>Haplo</author>
    class IncidentWorker_AgentReveiled : IncidentWorker
    {

        public override bool TryExecute(IncidentParms parms)
        {
            // Don't execute, when the colonist-count is too low
            if (Find.ListerPawns.ColonistCount <= 4)
                return false;

            // Find the faction of the agent
            Faction faction;
            if (Find.ListerPawns.PawnsHostileToColony.Count() > 0)
                faction = Find.ListerPawns.PawnsHostileToColony.RandomElement<Pawn>().Faction;
            else
                faction = Find.FactionManager.AllFactions.Where(f => (f != Faction.OfColony) && f.ColonyGoodwill < 0.0f).RandomElement();

            if (faction == null)
                return false;

            // Find the pawn to use
            Pawn pawn;

            IEnumerable<Pawn> pawns = Find.ListerPawns.FreeColonists;
            pawn = pawns.ElementAt(UnityEngine.Random.Range(3, pawns.Count() - 1)); // Don't use colonist 0,1,2

            if (pawn == null)
                return false;


            // Set the faction of the found pawn to the target faction
            pawn.ChangePawnFactionTo(faction);

            
            // New duty: leave as fast as possible
            //pawn.MindState.duty = new PawnDuty(DutyType.ExitMapNearest);
            SquadBrainState brainState = new SquadBrainState_ExitMapNearest();
            List<Pawn> list = new List<Pawn>();
            list.Add(pawn);
            SquadBrain.MakeNewSquadBrain(faction, brainState, list);


            // Make letter
            StringBuilder str = new StringBuilder();
            //str = "AgentReveiledIncident".Translate();
            //str = "AgentReveiledIncidentEscape".Translate();
            str.AppendFormat("{0} was revealed as an agent of {1}.", pawn.Name, faction.GetLabel());
            str.AppendLine();
            str.AppendLine();
            str.AppendFormat("{0} is trying to escape.", pawn.LabelShort);

            // Make a Letter without a Jump-To-Option
            //Find.LetterStack.ReceiveLetter(new Letter(str.ToString(), LetterType.BadUrgent));

            // Make a Letter with a Jump-To-Option
            Find.LetterStack.ReceiveLetter(new Letter(str.ToString(), LetterType.BadUrgent, pawn));

            return true;
        }

    }
}
