using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using RimWorld;

namespace TrainingFacility
{
    public class Graphic_RandomMote : Graphic_Random
    {
        private Dictionary<Thing, Material> MoteMaterialRefference = new Dictionary<Thing, Material>();

        public Graphic_RandomMote()
        { }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            Material workMaterial = null;
            if (MoteMaterialRefference.ContainsKey(thing))
            {
                workMaterial = MoteMaterialRefference[thing];
            }
            else
            {
                workMaterial = this.MatSingle;
                MoteMaterialRefference.Add(thing, workMaterial);

                // Do some cleanup for destroyed motes
                List<Thing> keys = new List<Thing>(MoteMaterialRefference.Keys.InRandomOrder());
                //for (int i = keys.Count - 1; i >=0; i--)
                for ( int i = 0; i < keys.Count; i++ )
                {
                    if (keys[i].Destroyed)
                        MoteMaterialRefference.Remove(keys[i]);
                }
            }

            Mote mote = (Mote)thing;
            ThingDef def = mote.def;
            float ageSecs = mote.AgeSecs;
            Material material = workMaterial;
            float num = 1f;
            if (def.mote.fadeInTime != 0f && ageSecs <= def.mote.fadeInTime)
            {
                num = ageSecs / def.mote.fadeInTime;
                material = FadedMaterialPool.FadedVersionOf(workMaterial, num);
            }
            else if (ageSecs < def.mote.fadeInTime + def.mote.solidTime)
            {
                num = 1f;
                material = workMaterial;
            }
            else if (def.mote.fadeOutTime != 0f)
            {
                num = 1f - (ageSecs - def.mote.fadeInTime - def.mote.solidTime) / def.mote.fadeOutTime;
                material = FadedMaterialPool.FadedVersionOf(workMaterial, num);
            }
            if (num <= 0f)
            {
                return;
            }
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(mote.DrawPos, Quaternion.AngleAxis(mote.exactRotation, Vector3.up), mote.ExactScale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        public override string ToString()
        {
            return string.Concat(new object[] { "Mote(path=", this.path, ", shader=", base.Shader, ", color=", this.color, ", colorTwo=unsupported)" });
        }


    }
}
