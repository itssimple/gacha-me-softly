using Chris.PachiRogue.UI;
using NUnit.Framework;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class StoryIntroModelTests
    {
        [Test]
        public void NewPlayer_WalksThreePagesThenEnds()
        {
            var model = new StoryIntroModel(returningPlayer: false);

            Assert.AreEqual(3, model.PageCount);
            Assert.AreEqual(StoryKeys.Page1, model.CurrentPageKey);
            Assert.IsFalse(model.IsLastPage);

            Assert.IsTrue(model.Advance());
            Assert.AreEqual(StoryKeys.Page2, model.CurrentPageKey);

            Assert.IsTrue(model.Advance());
            Assert.AreEqual(StoryKeys.Page3, model.CurrentPageKey);
            Assert.IsTrue(model.IsLastPage);

            Assert.IsFalse(model.Advance(), "Advance past the last page must return false.");
            Assert.AreEqual(StoryKeys.Page3, model.CurrentPageKey, "Page must not move past the end.");
        }

        [Test]
        public void ReturningPlayer_GetsSingleWelcomeBackPage()
        {
            var model = new StoryIntroModel(returningPlayer: true);

            Assert.AreEqual(1, model.PageCount);
            Assert.AreEqual(StoryKeys.WelcomeBack, model.CurrentPageKey);
            Assert.IsTrue(model.IsLastPage);
            Assert.IsFalse(model.Advance());
        }

        [Test]
        public void SanitizeName_TrimsWhitespace()
        {
            Assert.AreEqual("Chris", StoryIntroModel.SanitizeName("  Chris  "));
        }

        [Test]
        public void SanitizeName_CollapsesInternalWhitespace()
        {
            Assert.AreEqual("Chris the Bold", StoryIntroModel.SanitizeName("Chris \t the\n Bold"));
        }

        [Test]
        public void SanitizeName_EmptyInputs_ReturnEmpty()
        {
            Assert.AreEqual(string.Empty, StoryIntroModel.SanitizeName(null));
            Assert.AreEqual(string.Empty, StoryIntroModel.SanitizeName(""));
            Assert.AreEqual(string.Empty, StoryIntroModel.SanitizeName("   \t\n "));
        }

        [Test]
        public void SanitizeName_ClampsToMaxLength()
        {
            string longName = new string('a', StoryIntroModel.MaxNameLength + 20);
            string result = StoryIntroModel.SanitizeName(longName);

            Assert.AreEqual(StoryIntroModel.MaxNameLength, result.Length);
        }

        [Test]
        public void SanitizeName_ClampDoesNotLeaveTrailingSpace()
        {
            // 15 chars + space + more: the clamp cut lands after the space.
            string tricky = "abcdefghijklmno pqrstuvw";
            string result = StoryIntroModel.SanitizeName(tricky);

            Assert.LessOrEqual(result.Length, StoryIntroModel.MaxNameLength);
            Assert.IsFalse(result.EndsWith(" "), $"Result '{result}' has a trailing space.");
        }

        [Test]
        public void SanitizeName_KeepsUnicodeNames()
        {
            Assert.AreEqual("Åsa-Britt", StoryIntroModel.SanitizeName("Åsa-Britt"));
            Assert.AreEqual("ぷに", StoryIntroModel.SanitizeName(" ぷに "));
        }
    }
}
