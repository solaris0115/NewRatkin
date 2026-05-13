using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// 장비(방패·배너 등)가 허용하는 무기 파지 유형을 정의하고, 비호환 무기 장착을 차단한다.
    /// <see cref="allowedGripTypes"/>가 설정되면 Enum 판정, 비어 있으면 레거시 <see cref="allowedWeaponTags"/> 폴백.
    /// </summary>
    public class CompProperties_GripTypeFilter : CompProperties
    {
        public CompProperties_GripTypeFilter()
        {
            this.compClass = typeof(CompGripTypeFilter);
        }

        /// <summary>허용할 무기 파지 유형(화이트리스트). 설정되어 있으면 태그보다 우선한다.</summary>
        public List<RK_WeaponGripType> allowedGripTypes;

        /// <summary>허용할 무기의 WeaponTag 리스트 (레거시 화이트리스트)</summary>
        public List<string> allowedWeaponTags;

        /// <summary>차단 시 메시지 번역 키. null이면 기본 메시지 사용.</summary>
        public string blockReasonKey = null;
    }

    public class CompGripTypeFilter : ThingComp
    {
        public CompProperties_GripTypeFilter Props =>
            (CompProperties_GripTypeFilter)this.props;

        private Apparel Apparel => parent as Apparel;

        private Pawn Wearer => Apparel?.Wearer;

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            if (Props.allowedGripTypes.NullOrEmpty())
            {
                yield break;
            }

            string value = CompWeaponGripType.FormatGripTypesForDisplay(Props.allowedGripTypes);
            if (value.NullOrEmpty())
            {
                yield break;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("RK_AllowedGripType_Report".Translate());

            yield return new StatDrawEntry(
                StatCategoryDefOf.Apparel,
                "RK_AllowedGripType_Label".Translate(),
                value,
                report.ToString().TrimEnd(),
                82,
                null,
                null,
                false,
                false);
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);

            if (pawn?.equipment?.Primary == null)
            {
                return;
            }

            ThingWithComps primaryWeapon = pawn.equipment.Primary;
            string reason;

            if (!TryIsWeaponAllowed(primaryWeapon.def, out reason))
            {
                DropIncompatibleWeapon(pawn, primaryWeapon, reason);
            }
        }

        private void DropIncompatibleWeapon(Pawn pawn, ThingWithComps weapon, string reason)
        {
            ThingWithComps droppedWeapon;
            if (pawn.equipment.TryDropEquipment(weapon, out droppedWeapon, pawn.Position, false))
            {
                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    string message;
                    if (!Props.blockReasonKey.NullOrEmpty())
                    {
                        message = "RK_ApparelWeaponIncompatible_Dropped".Translate(
                            parent.Label,
                            weapon.Label,
                            Props.blockReasonKey.Translate());
                    }
                    else
                    {
                        message = "RK_ApparelWeaponIncompatible_Dropped_Default".Translate(
                            parent.Label,
                            weapon.Label);
                    }

                    Messages.Message(message, pawn, MessageTypeDefOf.CautionInput, false);
                }
            }
        }

        public bool TryIsWeaponAllowed(ThingDef weaponDef, out string reason)
        {
            if (weaponDef == null)
            {
                reason = "weapon_definition_null";
                return false;
            }

            if (!Props.allowedGripTypes.NullOrEmpty())
            {
                return TryIsWeaponAllowedByGrip(weaponDef, out reason);
            }

            return TryIsWeaponAllowedByLegacyTags(weaponDef, out reason);
        }

        private bool TryIsWeaponAllowedByGrip(ThingDef weaponDef, out string reason)
        {
            List<RK_WeaponGripType> weaponGrips = CompWeaponGripType.GripTypesFor(weaponDef);
            if (weaponGrips == null || weaponGrips.Count == 0)
            {
                reason = "allowed_no_grip_comp";
                return true;
            }

            if (CompWeaponGripType.IsGripCompatible(weaponGrips, Props.allowedGripTypes))
            {
                reason = "allowed_by_grip_type";
                return true;
            }

            string allowedList = CompWeaponGripType.FormatGripTypesForDisplay(Props.allowedGripTypes);
            if (!Props.blockReasonKey.NullOrEmpty())
            {
                reason = Props.blockReasonKey.Translate(weaponDef.label, allowedList);
            }
            else
            {
                reason = $"blocked_missing_grip:{weaponDef.defName}:{allowedList}";
            }

            return false;
        }

        private bool TryIsWeaponAllowedByLegacyTags(ThingDef weaponDef, out string reason)
        {
            List<string> allowedTags = Props.allowedWeaponTags;
            if (allowedTags == null || allowedTags.Count == 0)
            {
                reason = "allowed_weapon_tags_not_configured";
                return false;
            }

            List<string> weaponTags = weaponDef.weaponTags;
            if (weaponTags == null || weaponTags.Count == 0)
            {
                reason = "weapon_has_no_tags";
                return false;
            }

            for (int i = 0; i < weaponTags.Count; i++)
            {
                string weaponTag = weaponTags[i];
                if (weaponTag.NullOrEmpty())
                {
                    continue;
                }

                if (allowedTags.Contains(weaponTag))
                {
                    reason = $"allowed_tag:{weaponTag}";
                    return true;
                }
            }

            string allowedTagList = string.Join(", ", allowedTags);
            if (!Props.blockReasonKey.NullOrEmpty())
            {
                reason = Props.blockReasonKey.Translate(weaponDef.label, allowedTagList);
            }
            else
            {
                reason = $"blocked_missing_allowed_tag:{weaponDef.defName}:{allowedTagList}";
            }

            return false;
        }
    }
}
