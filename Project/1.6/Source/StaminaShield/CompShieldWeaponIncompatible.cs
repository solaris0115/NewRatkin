using System.Collections.Generic;
using Verse;
using RimWorld;

namespace NewRatkin
{
    /// <summary>
    /// 방패가 특정 무기 태그를 가진 무기와 함께 사용될 때 발사를 차단하는 컴포넌트 속성
    /// </summary>
    public class CompProperties_ShieldWeaponIncompatible : CompProperties
    {
        public CompProperties_ShieldWeaponIncompatible()
        {
            this.compClass = typeof(CompShieldWeaponIncompatible);
        }

        /// <summary>
        /// 허용할 무기의 WeaponTag 리스트 (화이트리스트)
        /// 이 리스트의 태그가 하나라도 포함된 무기만 허용하고 나머지는 차단
        /// </summary>
        public List<string> allowedWeaponTags;

        /// <summary>
        /// 경고 메시지 키 (번역 키)
        /// null이면 기본 메시지 사용
        /// </summary>
        public string blockReasonKey = null;
    }

    /// <summary>
    /// 방패가 특정 무기 태그를 가진 무기와 함께 사용될 때 발사를 차단하는 컴포넌트
    /// </summary>
    public class CompShieldWeaponIncompatible : ThingComp
    {
        public CompProperties_ShieldWeaponIncompatible Props => 
            (CompProperties_ShieldWeaponIncompatible)this.props;

        /// <summary>
        /// Apparel로 캐스팅 (parent가 Apparel이어야 함)
        /// </summary>
        private Apparel Apparel => parent as Apparel;

        /// <summary>
        /// 착용한 Pawn
        /// </summary>
        private Pawn Wearer => Apparel?.Wearer;

        /// <summary>
        /// Apparel 착용 시 호출됨 - 호환되지 않는 무기를 자동으로 제거
        /// </summary>
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);

            if (pawn?.equipment?.Primary == null)
            {
                return; // 착용한 무기가 없으면 OK
            }

            ThingWithComps primaryWeapon = pawn.equipment.Primary;
            string reason;

            if (!TryIsWeaponAllowed(primaryWeapon.def, out reason))
            {
                // 호환되지 않는 무기 제거
                DropIncompatibleWeapon(pawn, primaryWeapon, reason);
            }
        }

        /// <summary>
        /// 호환되지 않는 무기를 바닥에 떨어뜨리고 메시지 표시
        /// </summary>
        private void DropIncompatibleWeapon(Pawn pawn, ThingWithComps weapon, string reason)
        {
            ThingWithComps droppedWeapon;
            if (pawn.equipment.TryDropEquipment(weapon, out droppedWeapon, pawn.Position, false))
            {
                // 사용자에게 알림 메시지 표시
                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    string message;
                    if (!Props.blockReasonKey.NullOrEmpty())
                    {
                        // 번역 키가 설정되어 있으면 사용
                        message = "RK_ApparelWeaponIncompatible_Dropped".Translate(
                            parent.Label, 
                            weapon.Label, 
                            Props.blockReasonKey.Translate()
                        );
                    }
                    else
                    {
                        // 기본 메시지
                        message = "RK_ApparelWeaponIncompatible_Dropped_Default".Translate(
                            parent.Label, 
                            weapon.Label
                        );
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

