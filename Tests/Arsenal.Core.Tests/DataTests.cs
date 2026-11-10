using System.Linq;
using Arsenal.Core.Data;
using Arsenal.Core.Rules;
using Xunit;

namespace Arsenal.Core.Tests
{
    public class DataTests
    {
        [Fact]
        public void GameData_IsValid()
        {
            var errors = DatabaseValidator.Validate(TestData.Database);
            Assert.True(errors.Count == 0, string.Join("\n", errors));
        }

        [Fact]
        public void GameData_HasEveryFamily()
        {
            foreach (var f in WeaponFamilies.All)
                Assert.Contains(TestData.Database.Weapons, w => w.family == f);
        }

        [Fact]
        public void EveryAttachment_FitsAtLeastOneWeapon()
        {
            var db = TestData.Database;
            foreach (var a in db.Attachments)
            {
                bool fits = db.Weapons.Any(w => w.slots.Any(s => CompatibilityService.CheckSlot(w, s, a).Success));
                Assert.True(fits, $"{a.id} ne s'installe sur aucune arme");
            }
        }

        [Fact]
        public void Validator_DetectsBrokenReferences()
        {
            var w = new WeaponDefinition
            {
                id = "x", family = "pistol", baseStats = new StatBlock { damage = 1, fireRate = 100, magazineSize = 1, reloadTime = 1 },
                parts = new[] { new PartDefinition() },
                slots = new[] { new SlotDefinition { id = "magazine", types = new[] { "magazine" }, defaultAttachment = "missing", required = true } }
            };
            var db = new GameDatabase(new WeaponCatalog { weapons = new[] { w } }, new AttachmentCatalog(), new FinishCatalog(), new EconomyConfig());
            var errors = DatabaseValidator.Validate(db);
            Assert.Contains(errors, e => e.Contains("missing"));
            Assert.Contains(errors, e => e.Contains("arme de départ"));
        }
    }
}
