using System;

namespace AssetCache
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var testPath = "/home/vitalii/AssetCache/AssetCache/Examples/Small.unity";
            //var testPath = "/home/vitalii/AssetCache/AssetCache/Examples/SampleScene.unity";
            var cache = new AssetCacheImpl();

            var i = 0;
            while (true)
            {
                try
                {
                    var cacheResult = cache.Build(testPath, () =>
                    {
                        i++;
                        if (i % 2 == 0)
                        {
                            throw new OperationCanceledException();
                        }
                    });
                    cache.Merge(testPath, cacheResult);
                    break;
                }
                catch (OperationCanceledException e)
                {
                }
            }

            

            Console.WriteLine("id=241: " + cache.GetLocalAnchorUsages(241));
            Console.WriteLine("guid=0000000000000000f000000000000000: " + 
                              cache.GetGuidUsages("0000000000000000f000000000000000"));
            Console.WriteLine("components of id=241: ");
            var components = cache.GetComponentsFor(241);
            foreach (var component in components)
            {
                Console.WriteLine(component);
            }

            //Console.ReadKey();
        }
    }
}