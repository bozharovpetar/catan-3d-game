// SCAFFOLD — example EditMode tests (pass from M1). Add the full catalogue from docs/09-TESTING.md §2.
using System.Linq;
using Hexhaven.Core;
using NUnit.Framework;

namespace Hexhaven.Tests
{
    public class BoardTests
    {
        [Test]
        public void T101_BoardCounts()
        {
            var b = Board.CreateGeometry();
            Assert.AreEqual(19, b.Tiles.Count);
            Assert.AreEqual(54, b.Vertices.Count);
            Assert.AreEqual(72, b.Edges.Count);
            Assert.AreEqual(9, b.Harbors.Count);
            Assert.AreEqual(24, b.Vertices.Count(v => v.Tiles.Count == 3));
            Assert.AreEqual(12, b.Vertices.Count(v => v.Tiles.Count == 2));
            Assert.AreEqual(18, b.Vertices.Count(v => v.Tiles.Count == 1));
            Assert.AreEqual(30, b.Edges.Count(e => e.Tiles.Count == 1));
        }

        [Test]
        public void T103_BeginnerLayout()
        {
            var b = BoardGenerator.Generate(BoardMode.Beginner, new Pcg32(1));
            Assert.AreEqual(Terrain.Ruins, b.Tiles[10].Terrain);
            Assert.AreEqual(0, b.Tiles[10].Number);
            Assert.AreEqual(12, b.Tiles[9].Number);
            Assert.AreEqual(HarborType.Timber, b.Harbors[1].Type);
            Assert.IsTrue(BoardGenerator.SatisfiesC1(b) && BoardGenerator.SatisfiesC2(b) && BoardGenerator.SatisfiesC3(b)
                          && BoardGenerator.SatisfiesC4(b) && BoardGenerator.SatisfiesC5(b));
        }

        [Test]
        public void T104_BalancedConstraintsHoldForManySeeds()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                var b = BoardGenerator.Generate(BoardMode.BalancedRandom, new Pcg32((ulong)seed));
                Assert.IsTrue(BoardGenerator.SatisfiesC1(b) && BoardGenerator.SatisfiesC2(b) && BoardGenerator.SatisfiesC3(b)
                              && BoardGenerator.SatisfiesC4(b) && BoardGenerator.SatisfiesC5(b), "seed " + seed);
            }
        }
    }
}
