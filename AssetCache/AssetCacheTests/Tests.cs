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

        [SetUp]
        public void SetUp()
        {
            smallSample = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "../../Samples/Small.unity");
            smallSampleCopy = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "../../Samples/Small-Copy.unity");
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
            // todo
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
                    var fileCache = cache.Build(smallSample,
                        () => {});
                    cache.Merge(smallSample, fileCache);
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
        public void TestMergeAgainAfterModification()
        {
            // todo
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
                    var fileCache = cache.Build(smallSample,
                        () => {});
                    cache.Merge(smallSample, fileCache);
                }
            }

            Assert.AreEqual(3, cache.GetLocalAnchorUsages(1253103));
            Assert.AreEqual(2, cache.GetLocalAnchorUsages(1253104));
            Assert.AreEqual(2, cache.GetGuidUsages("f70555f144d8491a825f0804e09c671c"));
            Assert.AreEqual(1, cache.GetGuidUsages("bb7e9cca953d340059eb1e053bbbae31"));
            Assert.AreEqual(new List<ulong> {1253104, 1253106, 1253105},
                cache.GetComponentsFor(1253103));
        }
    }
}