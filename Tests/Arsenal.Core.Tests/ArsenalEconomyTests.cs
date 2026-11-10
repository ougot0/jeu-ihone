using Arsenal.Core.Data;
using Arsenal.Core.Services;
using Xunit;

namespace Arsenal.Core.Tests
{
    public class ArsenalEconomyTests
    {
        [Fact]
        public void NewGame_HasStarterWeaponBuildAndMoney()
        {
            var (profile, arsenal, economy) = TestData.NewGame();
            Assert.Equal(TestData.Database.Economy.startingMoney, profile.money);
            Assert.Single(profile.ownedWeapons);
            Assert.NotNull(arsenal.SelectedBuild);
            Assert.Equal("mag_p9_std", arsenal.SelectedBuild.GetAttachment("magazine"));
            Assert.Equal(1, economy.Level);
        }

        [Fact]
        public void BuyWeapon_RespectsLevelAndFunds()
        {
            var (profile, arsenal, economy) = TestData.NewGame();
            Assert.Equal(ResultCodes.LevelTooLow, economy.BuyWeapon("talon_ar7").Code);

            economy.GrantReward(0, economy.Progression.TotalXpForLevel(4));
            Assert.Equal(4, economy.Level);
            Assert.Equal(ResultCodes.InsufficientFunds, economy.BuyWeapon("talon_ar7").Code);

            economy.GrantReward(10000, 0);
            int before = profile.money;
            var r = economy.BuyWeapon("talon_ar7");
            Assert.True(r.Success, r.Message);
            Assert.Equal(before - 5500, profile.money);
            Assert.Single(arsenal.GetBuilds("talon_ar7"));
            Assert.Contains("stock_ar_std", profile.ownedAttachments);
            Assert.Equal(ResultCodes.AlreadyOwned, economy.BuyWeapon("talon_ar7").Code);
        }

        [Fact]
        public void Install_RequiresOwnershipAndCompatibility_ThenChangesStats()
        {
            var (profile, arsenal, economy) = TestData.NewGame();
            var build = arsenal.SelectedBuild;
            Assert.Equal(ResultCodes.NotOwned, arsenal.Install(build.id, "magazine", "mag_p9_ext").Code);

            Assert.True(economy.BuyAttachment("mag_p9_ext").Success);
            int changes = 0;
            arsenal.Changed += () => changes++;
            Assert.True(arsenal.Install(build.id, "magazine", "mag_p9_ext").Success);
            Assert.Equal(1, changes);
            Assert.Equal(21, arsenal.GetFinalStats(build).magazineSize);

            economy.GrantReward(5000, 5000);
            Assert.True(economy.BuyAttachment("optic_reflex_r1").Success);
            Assert.Equal(ResultCodes.WrongMount, arsenal.Install(build.id, "optic", "optic_reflex_r1").Code);
        }

        [Fact]
        public void Uninstall_RequiredSlot_RevertsToDefault()
        {
            var (profile, arsenal, economy) = TestData.NewGame();
            var build = arsenal.SelectedBuild;
            economy.BuyAttachment("mag_p9_ext");
            arsenal.Install(build.id, "magazine", "mag_p9_ext");
            Assert.True(arsenal.Uninstall(build.id, "magazine").Success);
            Assert.Equal("mag_p9_std", build.GetAttachment("magazine"));
            Assert.Equal(ResultCodes.SlotRequired, arsenal.Uninstall(build.id, "magazine").Code);
        }

        [Fact]
        public void Conflicts_AreEnforced()
        {
            var (profile, arsenal, economy) = TestData.NewGame();
            profile.ownedWeapons.Add("talon_ar7");
            profile.ownedAttachments.AddRange(new[] { "grip_angled", "light_l2" });
            arsenal.EnsureDefaultBuilds();
            var b = arsenal.GetBuilds("talon_ar7")[0];
            Assert.True(arsenal.Install(b.id, "underbarrel", "grip_angled").Success);
            Assert.Equal(ResultCodes.Conflict, arsenal.Install(b.id, "rail", "light_l2").Code);
        }

        [Fact]
        public void Builds_CanBeCreatedDuplicatedRenamedDeleted()
        {
            var (profile, arsenal, economy) = TestData.NewGame();
            var first = arsenal.SelectedBuild;
            Assert.True(arsenal.CreateBuild("vk9_sentinel", "  Compétition  ", out var b2).Success);
            Assert.Equal("Compétition", b2.name);
            Assert.True(arsenal.DuplicateBuild(b2.id, "Copie", out var b3).Success);
            Assert.NotEqual(b2.id, b3.id);
            Assert.Equal(ResultCodes.InvalidName, arsenal.RenameBuild(b3.id, "   ").Code);
            Assert.True(arsenal.RenameBuild(b3.id, "Nuit").Success);
            Assert.Equal(3, arsenal.GetBuilds("vk9_sentinel").Count);
            Assert.True(arsenal.DeleteBuild(b3.id).Success);
            Assert.True(arsenal.DeleteBuild(b2.id).Success);
            Assert.Equal(ResultCodes.LastBuild, arsenal.DeleteBuild(first.id).Code);
            Assert.Equal(ResultCodes.NotOwned, arsenal.CreateBuild("talon_ar7", "x", out _).Code);
        }

        [Fact]
        public void Progression_CurveIsMonotonicAndRaisesLevelUp()
        {
            var (profile, arsenal, economy) = TestData.NewGame();
            var p = economy.Progression;
            for (int l = 2; l < 30; l++) Assert.True(p.TotalXpForLevel(l + 1) > p.TotalXpForLevel(l));
            int ups = 0;
            economy.LevelUp += _ => ups++;
            economy.GrantReward(100, p.TotalXpForLevel(3));
            Assert.Equal(2, ups);
            Assert.Equal(3, economy.Level);
        }
    }
}
