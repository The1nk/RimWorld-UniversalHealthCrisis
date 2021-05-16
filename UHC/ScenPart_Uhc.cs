using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Random = System.Random;

namespace The1nk.UniversalHealthCrisis {
    public class ScenPart_Uhc : ScenPart {
        private float percentChance = 0.5f;
        
        private float targetPercentMin = 0f;
        private float targetPercentMax = 0.5f;

        private PawnCapacityDef targetCap = PawnCapacityDefOf.Sight;

        private bool humanlikes = true;
        private bool animals = true;
        private bool factionless = true;
        private bool myColony = true;
        private bool friendlyFactions = true;
        private bool neutralFactions = true;
        private bool hostileFactions = true;
        private bool setPainless = true;

        private Random random = new Random();

        public ScenPart_Uhc() {
            
        }

        public override void Randomize() {
            percentChance = RandBetween(.2f, .6f);
            targetPercentMin = RandBetween(0f, .5f);
            targetPercentMax = RandBetween(targetPercentMin, 1f);

            targetCap = DefDatabase<PawnCapacityDef>.AllDefs.RandomElement();

            humanlikes = RandBetween(0, 1) == 1;
            animals = RandBetween(0, 1) == 1;
            factionless = RandBetween(0, 1) == 1;
            myColony = RandBetween(0, 1) == 1;
            friendlyFactions = RandBetween(0, 1) == 1;
            neutralFactions = RandBetween(0, 1) == 1;
            hostileFactions = RandBetween(0, 1) == 1;
            setPainless = RandBetween(0, 1) == 1;
        }

        public override void ExposeData() {
            base.ExposeData();

            Scribe_Values.Look(ref percentChance, nameof(percentChance));
            Scribe_Values.Look(ref targetPercentMin, nameof(targetPercentMin));
            Scribe_Values.Look(ref targetPercentMax, nameof(targetPercentMax));

            Scribe_Defs.Look(ref targetCap, nameof(targetCap));

            Scribe_Values.Look(ref humanlikes, nameof(humanlikes));
            Scribe_Values.Look(ref animals, nameof(animals));
            Scribe_Values.Look(ref factionless, nameof(factionless));
            Scribe_Values.Look(ref myColony, nameof(myColony));
            Scribe_Values.Look(ref friendlyFactions, nameof(friendlyFactions));
            Scribe_Values.Look(ref neutralFactions, nameof(neutralFactions));
            Scribe_Values.Look(ref hostileFactions, nameof(hostileFactions));
            Scribe_Values.Look(ref setPainless, nameof(setPainless));
        }

        public override bool TryMerge(ScenPart other) {
            if (!(other is ScenPart_Uhc uhc))
                return false;

            return true;
        }

        public override void DoEditInterface(Listing_ScenEdit listing) {
            Rect scenPartRect = listing.GetScenPartRect((ScenPart) this, ScenPart.RowHeight * 17);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(scenPartRect);
            listingStandard.ColumnWidth = ((Rect) scenPartRect).width;

            listingStandard.Label("Chance".Translate(percentChance.ToString("P")));
            percentChance = listingStandard.Slider(percentChance, 0f, 1f);

            listingStandard.Label("MinCap".Translate(targetPercentMin.ToString("P")));
            targetPercentMin = listingStandard.Slider(targetPercentMin, 0f, 1f);

            listingStandard.Label("MaxCap".Translate(targetPercentMax.ToString("P")));
            targetPercentMax = listingStandard.Slider(targetPercentMax, 0f, 1f);

            if (listingStandard.ButtonText(targetCap.LabelCap)) {
                FloatMenuUtility.MakeMenu(DefDatabase<PawnCapacityDef>.AllDefsListForReading,
                    (l => l.LabelCap), l => () => targetCap = l);
            }
            
            listingStandard.CheckboxLabeled("Humanlikes".Translate(), ref humanlikes);
            listingStandard.CheckboxLabeled("Animals".Translate(), ref animals);
            listingStandard.CheckboxLabeled("Factionless".Translate(), ref factionless);
            listingStandard.CheckboxLabeled("MyColony".Translate(), ref myColony);
            listingStandard.CheckboxLabeled("FriendlyFactions".Translate(), ref friendlyFactions);
            listingStandard.CheckboxLabeled("NeutralFactions".Translate(), ref neutralFactions);
            listingStandard.CheckboxLabeled("HostileFactions".Translate(), ref hostileFactions);
            listingStandard.CheckboxLabeled("PainlessScars".Translate(), ref setPainless);
            
            listingStandard.End();
        }

        public override void Notify_NewPawnGenerating(Pawn pawn, PawnGenerationContext context) {
            ModifyPawn(pawn, context);
        }

        public override void Notify_PawnGenerated(Pawn pawn, PawnGenerationContext context, bool redressed) {
            if (redressed) // This means the pawn has already been generated, so we don't want to re-mutilate them
                return;
            
            ModifyPawn(pawn, context);
        }

        private void ModifyPawn(Pawn pawn, PawnGenerationContext context) {
            if (!humanlikes && pawn.RaceProps.Humanlike)
                return;

            if (!animals && pawn.RaceProps.Animal)
                return;

            if (!factionless && pawn.Faction == null)
                return;

            if (!myColony && pawn.Faction != null && pawn.Faction.IsPlayer)
                return;

            if (Faction.OfPlayerSilentFail != null) {
                if (!friendlyFactions && pawn.Faction != null && !pawn.Faction.IsPlayer) {
                    var ret = pawn.Faction.RelationWith(Faction.OfPlayer, true);
                    if (ret != null && ret.kind == FactionRelationKind.Ally)
                        return;
                }

                if (!neutralFactions && pawn.Faction != null && !pawn.Faction.IsPlayer) {
                    var ret = pawn.Faction.RelationWith(Faction.OfPlayer, true);
                    if (ret != null && ret.kind == FactionRelationKind.Neutral)
                        return;
                }

                if (!hostileFactions && pawn.Faction != null && !pawn.Faction.IsPlayer) {
                    var ret = pawn.Faction.RelationWith(Faction.OfPlayer, true);
                    if (ret != null && ret.kind == FactionRelationKind.Hostile)
                        return;
                }
            }

            if (targetPercentMax < targetPercentMin) {
                var a = targetPercentMax;
                targetPercentMax = targetPercentMin;
                targetPercentMin = a;
            }

            if (targetPercentMin < 0f)
                targetPercentMin = 0f;

            if (targetPercentMax > 1f)
                targetPercentMax = 1f;

            var thisPawnsTarget = RandBetween(targetPercentMin, targetPercentMax);
            var tries = 0;
            var tries2 = 0;
            var damageType = RandomPermanentInjuryDamageType(false);

            while (PawnCapacityUtility.CalculateCapacityLevel(pawn.health.hediffSet, targetCap) >
                   thisPawnsTarget) {
                var parts = pawn.health.hediffSet.GetNotMissingParts().Where(x =>
                        x.depth == BodyPartDepth.Outside &&
                        ((double) x.def.permanentInjuryChanceFactor != 0.0 || x.def.pawnGeneratorCanAmputate) &&
                        !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(x) &&
                        x.def.tags.Any(t => t.ToString().ToLower().Contains(targetCap.ToString().ToLower())))
                    .ToList();

                if (!parts.Any())
                    return;

                var target = parts.RandomElement();

                if (target == null)
                    return;
                
                HediffDef hediffDefFromDamage =
                    HealthUtility.GetHediffDefFromDamage(damageType, pawn, target);

                if (!hediffDefFromDamage.HasComp(typeof(HediffComp_GetsPermanent)) || target.def.permanentInjuryChanceFactor <= 0f) {
                    tries++;
                    if (tries > 150) {
                        Log.Warning(
                            $"Universal Health Crisis - Unable to damage pawn {pawn.Name.ToStringFull} due to damage def of {hediffDefFromDamage.defName} not being able to be made permanent OR permanentInjuryChanceFactor of {target.def.defName} being {target.def.permanentInjuryChanceFactor} (should be >0 to be damageable)");
                        return;
                    }

                    continue;
                }

                var hediff = pawn.health.hediffSet.hediffs.FirstOrDefault(h =>
                        h.Part == target &&
                        h.def == hediffDefFromDamage)
                    as Hediff_Injury;

                if (hediff == null) {
                    hediff = (Hediff_Injury) HediffMaker.MakeHediff(hediffDefFromDamage, pawn, target);
                    hediff.Severity = Rand.RangeInclusive(2, 6);
                    hediff.Part = target;

                    var comp = hediff.TryGetComp<HediffComp_GetsPermanent>();
                    if (comp != null) {
                        comp.IsPermanent = true;

                        if (setPainless)
                            SetPainless(comp);
                    }

                    if (pawn.health.WouldDieAfterAddingHediff(hediff))
                        return; // Don't add it

                    if (context == PawnGenerationContext.PlayerStarter &&
                        pawn.health.WouldBeDownedAfterAddingHediff(hediff))
                        return; // Don't add it
                
                    pawn.health.AddHediff(hediff);
                }
                else {
                    //pawn.health.RemoveHediff(hediff); // Remove it, and let's .. remove that part since it's been so unlucky. :(

                    Hediff_MissingPart hediffMissingPart =
                        (Hediff_MissingPart) HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn);
                    hediffMissingPart.lastInjury = hediffDefFromDamage;
                    hediffMissingPart.Part = target;
                    hediffMissingPart.IsFresh = false;
                    var comp = hediffMissingPart.TryGetComp<HediffComp_GetsPermanent>();
                    if (comp != null) {
                        comp.IsPermanent = true;

                        if (setPainless)
                            SetPainless(comp);
                    }

                    if (pawn.health.WouldDieAfterAddingHediff(hediffMissingPart))
                        return; // Don't add it

                    if (context == PawnGenerationContext.PlayerStarter &&
                        pawn.health.WouldBeDownedAfterAddingHediff(hediff))
                        return; // Don't add it

                    pawn.health.AddHediff((Hediff) hediffMissingPart, target);
                }
            }
        }

        private void SetPainless(HediffComp_GetsPermanent comp) {
            var field = typeof(HediffComp_GetsPermanent).GetField("painCategory", BindingFlags.Instance | BindingFlags.NonPublic);
            
            if (field != null)
                field.SetValue(comp, PainCategory.Painless);

            if (field == null)
                Log.Warning("Field is null!");
        }

        private float RandBetween(float min, float max) {
            return (float) random.NextDouble() * (max - min) + min;
        }

        private int RandBetween(int min, int max) {
            return random.Next(min, max + 1);
        }

        public override string Summary(Scenario scen) {
            var targets = "";
            var ret = "";

            if (animals) {
                var types = new List<string>();

                if (factionless)
                    types.Add("Wild");
                if (myColony)
                    types.Add("My Colony's");
                if (friendlyFactions)
                    types.Add("Friendly Factions'");
                if (neutralFactions)
                    types.Add("Neutral Factions'");
                if (hostileFactions)
                    types.Add("Hostile Factions'");

                ret = $"({string.Join(", ", types)}) animals";
            }

            if (humanlikes) {
                var types = new List<string>();

                if (factionless)
                    types.Add("Wild");
                if (myColony)
                    types.Add("My Colony's");
                if (friendlyFactions)
                    types.Add("Friendly Factions'");
                if (neutralFactions)
                    types.Add("Neutral Factions'");
                if (hostileFactions)
                    types.Add("Hostile Factions'");

                if (!string.IsNullOrEmpty(ret))
                    ret += ", ";
                ret += $"({string.Join(", ", types)}) humanlikes";
            }

            if (setPainless)
                ret = "SummaryPainless".Translate(percentChance.ToString("P"), targetPercentMin.ToString("P"),
                    targetPercentMax.ToString("P"), targets, targetCap.ToString()) + ret;
            else
                ret = "Summary".Translate(percentChance.ToString("P"), targetPercentMin.ToString("P"),
                    targetPercentMax.ToString("P"), targets, targetCap.ToString()) + ret;

            return ret;
        }

        private static DamageDef RandomPermanentInjuryDamageType(bool allowFrostbite)
        {
            switch (Rand.RangeInclusive(0, 3 + (allowFrostbite ? 1 : 0)))
            {
                case 0:
                    return DamageDefOf.Bullet;
                case 1:
                    return DamageDefOf.Scratch;
                case 2:
                    return DamageDefOf.Bite;
                case 3:
                    return DamageDefOf.Stab;
                case 4:
                    return DamageDefOf.Frostbite;
                default:
                    throw new Exception();
            }
        }
    }
}