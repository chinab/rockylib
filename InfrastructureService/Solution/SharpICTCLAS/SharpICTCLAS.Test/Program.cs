using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using SharpICTCLAS;

namespace SharpICTCLAS.Test
{
    class Program
    {
        public static string DictPath = Path.Combine(Environment.CurrentDirectory, "Data") + Path.DirectorySeparatorChar;
        public static string coreDictFile = DictPath + "coreDict.dct";
        public static string biDictFile = DictPath + "BigramDict.dct";
        public static string contextFile = DictPath + "nr.ctx";
        public static string nrFile = DictPath + "tr.dct";

        static void Main(string[] args)
        {
            TestDictionary();
            //TestNShortPath();
            //TestAtomSegment();
            //TestGenerateWordNet();
            //TestBiGraphGenerate();
            //TestBiSegment();
            //TestContextStat();
            //TestCCStringCompare();

            Console.Write("按下回车键退出......");
            Console.ReadLine();
        }

        #region 测试字典的读取

        public static void TestDictionary()
        {
            WordDictionary dict = new WordDictionary();
            if (dict.Load(coreDictFile, false))
            {
                for (int j = 2; j <= 5; j++)
                {
                    Console.WriteLine("====================================\r\n汉字:{0}, ID ：{1}\r\n", Utility.CC_ID2Char(j), j);

                    Console.WriteLine("  词长  频率  词性   词");
                    for (int i = 0; i < dict.indexTable[j].nCount; i++)
                        Console.WriteLine("{0,5} {1,6} {2,5}  ({3}){4}",
                           dict.indexTable[j].WordItems[i].nWordLen,
                           dict.indexTable[j].WordItems[i].nFrequency,
                           Utility.GetPOSString(dict.indexTable[j].WordItems[i].nPOS),
                           Utility.CC_ID2Char(j),
                           dict.indexTable[j].WordItems[i].sWord);
                }
            }
            else
                Console.WriteLine("Wrong!");
        }

        #endregion

        #region 测试原子分词

        public static void TestAtomSegment()
        {
            string sSentence = @"三星SHX-132型号的(手机)1元钱２５６.８９元12.14%百分比12％";
            sSentence = Predefine.SENTENCE_BEGIN + sSentence + Predefine.SENTENCE_END;
            List<AtomNode> nodes = Segment.AtomSegment(sSentence);
            for (int i = 0; i < nodes.Count; i++)
                Console.WriteLine("{0,10} {1,5}", nodes[i].sWord, nodes[i].nPOS);
        }

        #endregion

        #region 测试 N 最短路径

        public static void TestNShortPath()
        {
            int n = 2;
            List<int[]> result;
            int[] aPath;

            ColumnFirstDynamicArray<ChainContent> apCost = new ColumnFirstDynamicArray<ChainContent>();
            apCost.SetElement(0, 1, new ChainContent(1));
            apCost.SetElement(1, 2, new ChainContent(1));
            apCost.SetElement(1, 3, new ChainContent(2));
            apCost.SetElement(2, 3, new ChainContent(1));
            apCost.SetElement(2, 4, new ChainContent(1));
            apCost.SetElement(3, 4, new ChainContent(1));
            apCost.SetElement(4, 5, new ChainContent(1));
            apCost.SetElement(3, 6, new ChainContent(2));
            apCost.SetElement(4, 6, new ChainContent(3));
            apCost.SetElement(5, 6, new ChainContent(1));
            Console.WriteLine(apCost.ToString());

            NShortPath.Calculate(apCost, n);
            NShortPath.printResultByIndex();

            //----------------------------------------------------
            // 所有路径
            //----------------------------------------------------
            Console.WriteLine("\r\n\r\n所有路径：");
            for (int i = 0; i < n; i++)
            {
                result = NShortPath.GetPaths(i);
                for (int j = 0; j < result.Count; j++)
                {
                    aPath = result[j];
                    for (int k = 0; k < aPath.Length; k++)
                        Console.Write("{0}, ", aPath[k]);

                    Console.WriteLine();
                }
                Console.WriteLine("========================");
            }

            //----------------------------------------------------
            // 最佳路径
            //----------------------------------------------------
            Console.WriteLine("\r\n最佳路径：");
            aPath = NShortPath.GetBestPath();
            for (int k = 0; k < aPath.Length; k++)
                Console.Write("{0}, ", aPath[k]);

            Console.WriteLine();

            //----------------------------------------------------
            // 最多 n 个路径
            //----------------------------------------------------
            Console.WriteLine("\r\n最多 {0} 条路径：", 5);
            result = NShortPath.GetNPaths(5);
            for (int j = 0; j < result.Count; j++)
            {
                aPath = result[j];
                for (int k = 0; k < aPath.Length; k++)
                    Console.Write("{0}, ", aPath[k]);

                Console.WriteLine();
            }
        }

        #endregion

        #region 测试初始分词

        public static void TestGenerateWordNet()
        {
            WordDictionary coreDict = new WordDictionary();
            if (!coreDict.Load(coreDictFile))
            {
                Console.WriteLine("字典装入错误！");
                return;
            }

            string sSentence = @"他说的确实在理";
            sSentence = Predefine.SENTENCE_BEGIN + sSentence + Predefine.SENTENCE_END;

            List<AtomNode> atomSegment = Segment.AtomSegment(sSentence);
            RowFirstDynamicArray<ChainContent> m_segGraph = Segment.GenerateWordNet(atomSegment, coreDict);

            Console.WriteLine(m_segGraph.ToString());
        }

        #endregion

        #region 测试初次分词产生的二叉表

        public static void TestBiGraphGenerate()
        {
            WordDictionary coreDict = new WordDictionary();
            if (!coreDict.Load(coreDictFile))
            {
                Console.WriteLine("coreDict 字典装入错误！");
                return;
            }

            WordDictionary biDict = new WordDictionary();
            if (!biDict.Load(biDictFile))
            {
                Console.WriteLine("字典装入错误！");
                return;
            }

            string sSentence = @"他说的确实在理";
            sSentence = Predefine.SENTENCE_BEGIN + sSentence + Predefine.SENTENCE_END;

            //---原子分词
            List<AtomNode> atomSegment = Segment.AtomSegment(sSentence);

            //---检索词库，加入所有可能分词方案并存入链表结构
            RowFirstDynamicArray<ChainContent> segGraph = Segment.GenerateWordNet(atomSegment, coreDict);

            //---检索所有可能的两两组合
            ColumnFirstDynamicArray<ChainContent> biGraphResult = Segment.BiGraphGenerate(segGraph, 0.1, biDict, coreDict);

            Console.WriteLine(biGraphResult.ToString());
        }

        #endregion

        #region 测试 Segment.BiSegment

        public static void TestBiSegment()
        {
            List<string> sentence = new List<string>();
            List<string> description = new List<string>();

            sentence.Add(@"他说的确实在理");
            description.Add(@"普通分词测试");

            sentence.Add(@"张华平3－4月份来北京开会");
            description.Add(@"数字切分");

            sentence.Add(@"1.加强管理");
            description.Add(@"剔除多余的“.”");

            sentence.Add(@"他出生于1980年1月1日10点");
            description.Add(@"日期合并");

            sentence.Add(@"他出生于甲子年");
            description.Add(@"年份识别");

            sentence.Add(@"馆内陈列周恩来和邓颖超生前使用过的物品");
            description.Add(@"姓名识别");

            WordDictionary coreDict = new WordDictionary();
            if (!coreDict.Load(coreDictFile))
            {
                Console.WriteLine("coreDict 字典装入错误！");
                return;
            }

            WordDictionary biDict = new WordDictionary();
            if (!biDict.Load(biDictFile))
            {
                Console.WriteLine("字典装入错误！");
                return;
            }

            string sSentence;
            string sDescription;

            for (int i = 0; i < sentence.Count; i++)
            {
                sSentence = sentence[i];
                sDescription = description[i];
                Console.WriteLine("\r\n============ {0} ============", sDescription);


                sSentence = Predefine.SENTENCE_BEGIN + sSentence + Predefine.SENTENCE_END;

                List<AtomNode> nodes = Segment.AtomSegment(sSentence);
                Console.WriteLine("原子切分：");
                for (int j = 0; j < nodes.Count; j++)
                    Console.Write("{0}, ", nodes[j].sWord);

                Console.WriteLine("\r\n\r\n实际切分：");
                Segment segment = new Segment(biDict, coreDict);
                segment.BiSegment(sSentence, 0.1, 1);

                for (int k = 0; k < segment.m_pWordSeg.Count; k++)
                {
                    for (int j = 0; j < segment.m_pWordSeg[k].Length; j++)
                        Console.Write("{0}, ", segment.m_pWordSeg[k][j].sWord);
                    Console.WriteLine();
                }
            }
        }

        #endregion

        #region 测试 ContextStat

        public static void TestContextStat()
        {
            ContextStat cs = new ContextStat();

            if (cs.Load(contextFile))
                if (!cs.Save(DictPath + "nr.ctx"))
                    Console.WriteLine("写文件失败！");
                else
                    Console.WriteLine("OK!");
            else
                Console.WriteLine("文件装载失败！");
        }

        #endregion

        #region 测试 CCStringCompare

        public static void TestCCStringCompare()
        {
            string[] s = { "公开赛", "公开赛", "公开信", "公开性", "公款", "公款吃喝", "公厘", "公理", "公理", "公里", "公里/小时", "公里／小时", "公里／小时", "公里数", "公历", "公例", "公立", "公粮", "公路", "公路", "公路局", "公路桥", "公路网" };
            string[] s1 = { "王@、", "王@。", "王@”", "王@』", "王@，", "王@霸", "王@传", "王@大夫", "王@大娘", "王@大爷", "王@道士", "王@的", "王@家", "王@老汉", "王@两", "王@末##末", "王@女士", "王@未##人", "王@未##它", "王@先生", "王@姓", "王朝@，", "王朝@的", "王储@殿下", "王储@兼", "王储@未##人", "王府井@百货大楼", "王府井@大街", "王公@贵族", "王宫@会见", "王国@。", "王国@”", "王国@，", "王国@的", "王国@里", "王国@政府", "王后@未##人", "王码@电脑", "王牌@。", "王牌@”", "王室@成员", "王营@煤矿", "王兆国@、", "王兆国@，", "王兆国@出席", "王兆国@等", "王兆国@对", "王兆国@会见", "王兆国@及", "王兆国@今天", "王兆国@受", "王兆国@说", "王兆国@在", "王兆国@指出", "王兆国@主持", "王子@的" };

            for (int i = 0; i < s.Length - 1; i++)
            {
                if (Utility.CCStringCompare(s[i], s[i + 1]) >= 0 && string.Compare(s[i], s[i + 1]) != 0)
                    Console.WriteLine("出现错误：{0}   <-->   {1}", s[i], s[i + 1]);
            }

            for (int i = 0; i < s1.Length - 1; i++)
            {
                if (Utility.CCStringCompare(s1[i], s1[i + 1]) >= 0 && string.Compare(s1[i], s1[i + 1]) != 0)
                    Console.WriteLine("出现错误：{0}   <-->   {1}", s1[i], s1[i + 1]);
            }
        }

        #endregion
    }
}