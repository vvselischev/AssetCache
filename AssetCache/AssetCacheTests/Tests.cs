using System;
using System.Collections.Generic;
using System.IO;
using AssetCache;
using NUnit.Framework;

namespace AssetCacheTests
{
    [TestFixture]
    public class Tests
    {
        private string smallSample;
        private string smallSampleCopy;
        private string anotherSample;

        [SetUp]
        public void SetUp()
        {
            smallSample = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "../../Samples/Small.unity");
            smallSampleCopy = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "../../Samples/Small-Copy.unity");
            anotherSample = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "../../Samples/Another-Small.unity");
        }

        [Test]
        public void TestIdUsages()
        {
            var cache = new AssetCacheImpl();
            var fileCache = cache.Build(smallSample, () => { });
            cache.Merge(smallSample, fileCache);

            Assert.AreEqual(3, cache.GetLocalAnchorUsages(1253103));
            Assert.AreEqual(2, cache.GetLocalAnchorUsages(1253104));
            Assert.AreEqual(1, cache.GetLocalAnchorUsages(1253105));
            Assert.AreEqual(1, cache.GetLocalAnchorUsages(1253107));
            Assert.AreEqual(1, cache.GetLocalAnchorUsages(310984660));
        }

        [Test]
        public void TestGuidUsages()
        {
            var cache = new AssetCacheImpl();
            var fileCache = cache.Build(smallSample, () => { });
            cache.Merge(smallSample, fileCache);

            Assert.AreEqual(2, cache.GetGuidUsages("f70555f144d8491a825f0804e09c671c"));
            Assert.AreEqual(1, cache.GetGuidUsages("bb7e9cca953d340059eb1e053bbbae31"));
            Assert.AreEqual(1, cache.GetGuidUsages("aba4101f7625143f49d0968febe1a1b4"));
        }

        [Test]
        public void TestAttachedComponents()
        {
            var cache = new AssetCacheImpl();
            var fileCache = cache.Build(smallSample, () => { });
            cache.Merge(smallSample, fileCache);

            Assert.AreEqual(new List<ulong> {1253104, 1253106, 1253105},
                cache.GetComponentsFor(1253103));
            Assert.AreEqual(new List<ulong> {1253103, 1253104, 1253107},
                cache.GetComponentsFor(1253105));
        }

        [Test]
        public void TestMergeSameFileNotDuplicate()
        {
            var cache = new AssetCacheImpl();
            var fileCache = cache.Build(smallSample, () => { });
            cache.Merge(smallSample, fileCache);
            cache.Merge(smallSample, fileCache);

            Assert.AreEqual(3, cache.GetLocalAnchorUsages(1253103));
            Assert.AreEqual(2, cache.GetLocalAnchorUsages(1253104));
            Assert.AreEqual(2, cache.GetGuidUsages("f70555f144d8491a825f0804e09c671c"));
            Assert.AreEqual(1, cache.GetGuidUsages("bb7e9cca953d340059eb1e053bbbae31"));
            Assert.AreEqual(new List<ulong> {1253104, 1253106, 1253105},
                cache.GetComponentsFor(1253103));
        }

        [Test]
        public void TestMergeSeveralFiles()
        {
            var cache = new AssetCacheImpl();
            var fileCache = cache.Build(smallSample, () => { });
            cache.Merge(smallSample, fileCache);

            var copyFileCache = cache.Build(smallSampleCopy, () => { });
            cache.Merge(smallSampleCopy, copyFileCache);

            Assert.AreEqual(6, cache.GetLocalAnchorUsages(1253103));
            Assert.AreEqual(4, cache.GetLocalAnchorUsages(1253104));
            Assert.AreEqual(4, cache.GetGuidUsages("f70555f144d8491a825f0804e09c671c"));
            Assert.AreEqual(2, cache.GetGuidUsages("bb7e9cca953d340059eb1e053bbbae31"));
            Assert.AreEqual(new List<ulong> {1253104, 1253106, 1253105, 1253104, 1253106, 1253105},
                cache.GetComponentsFor(1253103));
        }

        [Test]
        public void TestInterruptionWithoutModification()
        {
            var cache = new AssetCacheImpl(1);
            while (true)
            {
                try
                {
                    var fileCache = cache.Build(smallSample,
                        () => { throw new OperationCanceledException(); });
                    cache.Merge(smallSample, fileCache);
                    break;
                }
                catch (OperationCanceledException)
                {
                }
            }

            Assert.AreEqual(3, cache.GetLocalAnchorUsages(1253103));
            Assert.AreEqual(2, cache.GetLocalAnchorUsages(1253104));
            Assert.AreEqual(2, cache.GetGuidUsages("f70555f144d8491a825f0804e09c671c"));
            Assert.AreEqual(1, cache.GetGuidUsages("bb7e9cca953d340059eb1e053bbbae31"));
            Assert.AreEqual(new List<ulong> {1253104, 1253106, 1253105},
                cache.GetComponentsFor(1253103));
        }

        [Test]
        public void TestInterruptionWithModification()
        {
            var fileName = Path.GetTempFileName();
            File.Copy(smallSample, fileName, true);
            
            var cache = new AssetCacheImpl(4);
            try
            {
                cache.Build(smallSample,
                    () => { throw new OperationCanceledException(); });
                // Must ask for interruption
                Assert.Fail();
            }
            catch (OperationCanceledException)
            {
                File.Copy(anotherSample, fileName, true);
                while (true)
                {
                    try
                    {
                        var anotherCache = cache.Build(anotherSample, () => { });
                        cache.Merge(anotherSample, anotherCache);
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }

            Assert.AreEqual(1, cache.GetLocalAnchorUsages(1253103));
            Assert.AreEqual(0, cache.GetLocalAnchorUsages(1253104));
            Assert.AreEqual(1, cache.GetGuidUsages("f70555f144d8491a825f0804e09c671c"));
            Assert.AreEqual(1, cache.GetGuidUsages("bb7e9cca953d340059eb1e053bbbae31"));
            Assert.AreEqual(new List<ulong>(),
                cache.GetComponentsFor(1253103));
            Assert.AreEqual(1, cache.GetLocalAnchorUsages(1043780135));
            Assert.AreEqual(new List<ulong> {1050298693, 1050298695, 1050298694},
                cache.GetComponentsFor(1050298692));
            
            File.Delete(fileName);
        }

        [Test]
        public void TestBuildAndMergeAgainAfterModification()
        {
            var fileName = Path.GetTempFileName();

            File.SetLastWriteTime(smallSample, DateTime.Now.Subtract(TimeSpan.FromSeconds(1)));
            File.Copy(smallSample, fileName, true);
            var cache = new AssetCacheImpl(1);
            var fileCache = cache.Build(fileName,
                        () => { });
            cache.Merge(fileName, fileCache);
            
            File.SetLastWriteTime(anotherSample, DateTime.Now);
            File.Copy(anotherSample, fileName, true);
            var anotherCache = cache.Build(fileName,
                () => { });
            cache.Merge(fileName, anotherCache);

            Assert.AreEqual(1, cache.GetLocalAnchorUsages(1253103));
            Assert.AreEqual(0, cache.GetLocalAnchorUsages(1253104));
            Assert.AreEqual(1, cache.GetGuidUsages("f70555f144d8491a825f0804e09c671c"));
            Assert.AreEqual(1, cache.GetGuidUsages("bb7e9cca953d340059eb1e053bbbae31"));
            Assert.AreEqual(new List<ulong>(),
                cache.GetComponentsFor(1253103));
            Assert.AreEqual(1, cache.GetLocalAnchorUsages(1043780135));
            Assert.AreEqual(new List<ulong> {1050298693, 1050298695, 1050298694},
                cache.GetComponentsFor(1050298692));
            
            File.Delete(fileName);
        }
    }
}
