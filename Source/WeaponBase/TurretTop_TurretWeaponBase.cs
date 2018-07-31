using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI; 
//using Verse.Sound;
using RimWorld; 
//using RimWorld.Planet;
//using RimWorld.SquadAI;

using CommonMisc; // for Helper class


namespace TurretWeaponBase
{
    public class TurretTop_TurretWeaponBase
    {
        private const float IdleTurnDegreesPerTick = 0.26f;
        private const int IdleTurnDuration = 140;
        private const int IdleTurnIntervalMin = 150;
        private const int IdleTurnIntervalMax = 350;

        private Building_TurretWeaponBase parentTurret;

        private Material TopMaterial = null;
        private Material TopMatCustom = null;
        private bool checkedForCustomMaterial = false;

        private float curRotationInt;
        private int ticksUntilIdleTurn;
        private int idleTurnTicksLeft;
        private bool idleTurnClockwise;

        private float CurRotation
        {
            get
            {
                return curRotationInt;
            }
            set
            {
                curRotationInt = value;

                if (curRotationInt > 360f)
                    curRotationInt = curRotationInt - 360f;

                if (curRotationInt < 0f)
                    curRotationInt = curRotationInt + 360f;

            }
        }

        public TurretTop_TurretWeaponBase(Building_TurretWeaponBase ParentTurret)
        {
            parentTurret = ParentTurret;
        }

        public void DrawTurret()
        {
            Matrix4x4 matrix4x4 = new Matrix4x4();
            Building_TurretWeaponBase.TopMatType usedTopMatType = parentTurret.usedTopMatType;

            // Check if a needed material is null
            if ((usedTopMatType == Building_TurretWeaponBase.TopMatType.ShortMediumLongMat &&
                    parentTurret.TopMatShortWeapon == null && 
                    parentTurret.TopMatMediumWeapon == null && 
                    parentTurret.TopMatLongWeapon == null) ||
                (usedTopMatType == Building_TurretWeaponBase.TopMatType.BuildingMat && 
                    parentTurret.def.building.turretTopMat == null))
            {
                // The Material of the gun should never be null, so we use that as the alternate
                usedTopMatType = Building_TurretWeaponBase.TopMatType.GunMat;
            }

            if (usedTopMatType == Building_TurretWeaponBase.TopMatType.BuildingMat)
            {
                // Use normal top graphic as defined in building.turretTop
                matrix4x4.SetTRS(parentTurret.DrawPos + Altitudes.AltIncVect, CurRotation.ToQuat(), Vector3.one);
                Graphics.DrawMesh(MeshPool.plane20, matrix4x4, parentTurret.def.building.turretTopMat, 0);
                return;
            }


            if (!checkedForCustomMaterial && TopMatCustom == null)
            {
                checkedForCustomMaterial = true;

                //Material m = Helper.LoadMaterial(parentTurret.gun.def.graphicData.texPath + "_top", ShaderType.Transparent, false);
                Texture2D mTex = ContentFinder<Texture2D>.Get(parentTurret.gun.def.graphicData.texPath + "_top", false);
                if (mTex != null)
                {
                    MaterialRequest mReq = new MaterialRequest(mTex);
                    Material m = MaterialPool.MatFrom(parentTurret.gun.def.graphicData.texPath + "_top", false);
                    if (m != null)
                        TopMatCustom = m;
                }
            }
            if (TopMatCustom != null && (usedTopMatType == Building_TurretWeaponBase.TopMatType.ShortMediumLongMat || usedTopMatType == Building_TurretWeaponBase.TopMatType.GunMat))
            {
                // If weapon graphic used, it needs to be rotated by 90°
                float workRotation = CurRotation - 90;
                if (CurRotation < 0)
                    workRotation += 360;

                Vector3 v3 = new Vector3(1.0f, 1.0f, 1.0f); // manual overdraw
                matrix4x4.SetTRS(parentTurret.DrawPos + Altitudes.AltIncVect, (workRotation).ToQuat(), v3); //Vector3.one);
                Graphics.DrawMesh(MeshPool.plane20, matrix4x4, TopMatCustom, 0);
                return;
            }


            if (usedTopMatType == Building_TurretWeaponBase.TopMatType.ShortMediumLongMat)
            {
                // Use special top graphic as defined in parentTurret.TopMatxxxxWeapon
                if (TopMaterial == null)
                {
                    float weaponPrice = parentTurret.gun.def.BaseMarketValue;

                    if (weaponPrice < parentTurret.priceShortMax)
                        TopMaterial = parentTurret.TopMatShortWeapon;
                    else if (weaponPrice < parentTurret.priceMediumMax)
                        TopMaterial = parentTurret.TopMatMediumWeapon;
                    else
                        TopMaterial = parentTurret.TopMatLongWeapon;
                }
                Vector3 v3 = new Vector3(1.0f, 1.0f, 1.0f); // manual overdraw
                matrix4x4.SetTRS(parentTurret.DrawPos + Altitudes.AltIncVect, CurRotation.ToQuat(), v3); //Vector3.one);
                Graphics.DrawMesh(MeshPool.plane20, matrix4x4, TopMaterial, 0);
                return;
            }


            if (usedTopMatType == Building_TurretWeaponBase.TopMatType.GunMat)
            {
                // If weapon graphic used, it needs to be rotated by 90°
                float workRotation = CurRotation - 90;
                if (CurRotation < 0)
                    workRotation += 360;

                matrix4x4.SetTRS(parentTurret.DrawPos + Altitudes.AltIncVect, (workRotation).ToQuat(), Vector3.one);
                Graphics.DrawMesh(MeshPool.plane20, matrix4x4, parentTurret.gun.Graphic.MatAt(Rot4.North), 0);
                return;
            }
        }

        public void TurretTopTick()
        {
            LocalTargetInfo currentTarget = parentTurret.CurrentTarget;
            if (currentTarget.IsValid)
            {

                float curRotation = (currentTarget.Cell.ToVector3Shifted() - this.parentTurret.DrawPos).AngleFlat();
                this.CurRotation = curRotation;
                ticksUntilIdleTurn = Rand.RangeInclusive( IdleTurnIntervalMin, IdleTurnIntervalMax ); // (150, 350);

            }
            else if (ticksUntilIdleTurn <= 0)
            {

                if (!idleTurnClockwise)
                    CurRotation -= IdleTurnDegreesPerTick; // 0.26f;
                else
                    CurRotation += IdleTurnDegreesPerTick; // 0.26f;

                idleTurnTicksLeft -= 1;
                if (idleTurnTicksLeft <= 0)
                    ticksUntilIdleTurn = Rand.RangeInclusive( IdleTurnIntervalMin, IdleTurnIntervalMax); // (150, 350);

            }
            else
            {
                ticksUntilIdleTurn -= 1;
                if (ticksUntilIdleTurn == 0)
                {
                    if (Rand.Value >= 0.5f)
                        idleTurnClockwise = false;
                    else
                        idleTurnClockwise = true;

                    idleTurnTicksLeft = IdleTurnDuration; // 140;
                }
            }
        }
    }
}
