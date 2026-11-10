using Arsenal.Core.Data;
using Arsenal.Core.Rules;
using Xunit;

namespace Arsenal.Core.Tests
{
    public class RulesTests
    {
        private static GameDatabase Db => TestData.Database;

        [Fact]
        public void Optic_RejectedOnMagazineSlot()
        {
            var w = Db.GetWeapon("talon_ar7");
            var r = CompatibilityService.CheckSlot(w, w.FindSlot("magazine"), Db.GetAttachment("optic_reflex_r1"));
            Assert.Equal(ResultCodes.WrongType, r.Code);
        }

        [Fact]
        public void ReflexSight_RejectedOnPistolPlate_ByMount()
        {
            var w = Db.GetWeapon("vk9_sentinel");
            var r = CompatibilityService.CheckSlot(w, w.FindSlot("optic"), Db.GetAttachment("optic_reflex_r1"));
            Assert.Equal(ResultCodes.WrongMount, r.Code);
        }

        [Fact]
        public void Scope8x_RejectedOnSmg_ByFamily()
        {
            var w = Db.GetWeapon("kestrel_k45");
            var r = CompatibilityService.CheckSlot(w, w.FindSlot("optic"), Db.GetAttachment("optic_scope_8x"));
            Assert.Equal(ResultCodes.WrongFamily, r.Code);
        }

        [Fact]
        public void StatCalculator_AppliesAddThenMultiplyAndClamps()
        {
            var b = new StatBlock { damage = 100, accuracy = 95, recoil = 5, fireRate = 600, magazineSize = 30, handling = 50, reloadTime = 2 };
            var mods = new[]
            {
                new AttachmentDefinition { modifiers = new[] { new StatModifier { stat = "accuracy", add = 10 }, new StatModifier { stat = "damage", mul = 0.9f } } },
                new AttachmentDefinition { modifiers = new[] { new StatModifier { stat = "recoil", add = -20 }, new StatModifier { stat = "magazineSize", add = 15 } } }
            };
            var s = StatCalculator.Compute(b, mods);
            Assert.Equal(100f, s.accuracy);          // borné à 100
            Assert.Equal(0f, s.recoil);              // borné à 0
            Assert.Equal(90f, s.damage, 3);
            Assert.Equal(45, s.magazineSize);
            Assert.Equal(50f, s.handling);
        }
    }
}
