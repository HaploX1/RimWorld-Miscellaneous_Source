using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace AIRobot
{
    // Based of Verse.Dialog_ChangeNameTriple
    public class X2_Dialog_ChangeNameTriple_Robots : Window
    {

        private Pawn pawn;
        private string curName;
        private const int MaxNameLength = 16;

        private NameTriple CurPawnName
        {
            get
            {
                NameTriple nameTriple = NameTriple.FromString(this.pawn.Name.ToString());
                if (nameTriple != null)
                {
                    return new NameTriple(nameTriple.First, this.curName, nameTriple.Last);
                }
                throw new InvalidOperationException();
            }
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(500f, 175f);
            }
        }

        public X2_Dialog_ChangeNameTriple_Robots(Pawn pawn)
        {
            this.pawn = pawn;
            this.curName = pawn.Name.ToString();
            base.forcePause = true;
            base.absorbInputAroundWindow = true;
            base.closeOnClickedOutside = true;
  
        }

        public override void DoWindowContents(Rect inRect)
        {

            bool flag = false;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                flag = true;
                Event.current.Use();
            }
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(15f, 15f, 500f, 50f), this.CurPawnName.ToString().Replace(" '' ", " "));
            Text.Font = GameFont.Small;
            string text = Widgets.TextField(new Rect(15f, 50f, inRect.width / 2f - 20f, 35f), this.curName);
            if (text.Length < 16)
            {
                this.curName = text;
            }
            if (!Widgets.ButtonText(new Rect(inRect.width / 2f + 20f, inRect.height - 35f, inRect.width / 2f - 20f, 35f), "OK".Translate(), true, false, true) && !flag)
            {
                return;
            }
            if (string.IsNullOrEmpty(this.curName))
            {
                this.curName = ((NameTriple)this.pawn.Name).Nick;
            }
            this.pawn.Name = this.CurPawnName;
            Find.WindowStack.TryRemove(this, true);
            //Messages.Message("PawnGainsName".Translate(this.curName), this.pawn, MessageTypeDefOf.PositiveEvent);

            Messages.Message("AIRobot_RobotIsRenamed".Translate(this.curName), this.pawn, MessageTypeDefOf.PositiveEvent);
        }

    }
}
