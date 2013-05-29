using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SharpICTCLAS;

namespace InfrastructureService.DomainService
{
    public class WordSegmentApp
    {
        #region Fields
        private string dictPath;
        /// <summary>
        /// 在NShortPath方法中用来决定初步切分时分成几种结果
        /// </summary>
        private int nKind;
        private WordSegment wordSegment;
        private WordDictionary dict;
        #endregion

        #region Properties
        protected WordDictionary Dictionary
        {
            get
            {
                if (dict == null)
                {
                    dict = new WordDictionary();
                    dict.Load(dictPath + "coreDict.dct");
                }
                return dict;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// 构造函数，在没有指明nKind的情况下，nKind 取 1
        /// </summary>
        /// <param name="dictPath"></param>
        public WordSegmentApp(string dictPath)
            : this(dictPath, 1)
        {

        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dictPath"></param>
        /// <param name="nKind"></param>
        public WordSegmentApp(string dictPath, int nKind)
        {
            this.wordSegment = new WordSegment();
            //wordSegment.PersonRecognition = false;
            //wordSegment.PlaceRecognition = false;
            //wordSegment.TransPersonRecognition = false;
            //wordSegment.OnSegmentEvent += new SegmentEventHandler(this.OnSegmentEventHandler);
            wordSegment.InitWordSegment(dictPath);
            this.dictPath = dictPath;
            this.nKind = nKind;
        }
        /// <summary>
        /// 输出分词过程中每一步的中间结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSegmentEventHandler(object sender, SegmentEventArgs e)
        {
            switch (e.Stage)
            {
                case SegmentStage.BeginSegment:
                    Console.WriteLine("\r\n==== 原始句子：\r\n");
                    Console.WriteLine(e.Info + "\r\n");
                    break;
                case SegmentStage.AtomSegment:
                    Console.WriteLine("\r\n==== 原子切分：\r\n");
                    Console.WriteLine(e.Info);
                    break;
                case SegmentStage.GenSegGraph:
                    Console.WriteLine("\r\n==== 生成 segGraph：\r\n");
                    Console.WriteLine(e.Info);
                    break;
                case SegmentStage.GenBiSegGraph:
                    Console.WriteLine("\r\n==== 生成 biSegGraph：\r\n");
                    Console.WriteLine(e.Info);
                    break;
                case SegmentStage.NShortPath:
                    Console.WriteLine("\r\n==== NShortPath 初步切分的到的 N 个结果：\r\n");
                    Console.WriteLine(e.Info);
                    break;
                case SegmentStage.BeforeOptimize:
                    Console.WriteLine("\r\n==== 经过数字、日期合并等策略处理后的 N 个结果：\r\n");
                    Console.WriteLine(e.Info);
                    break;
                case SegmentStage.OptimumSegment:
                    Console.WriteLine("\r\n==== 将 N 个结果归并入OptimumSegment：\r\n");
                    Console.WriteLine(e.Info);
                    break;
                case SegmentStage.PersonAndPlaceRecognition:
                    Console.WriteLine("\r\n==== 加入对姓名、翻译人名以及地名的识别：\r\n");
                    Console.WriteLine(e.Info);
                    break;
                case SegmentStage.BiOptimumSegment:
                    Console.WriteLine("\r\n==== 对加入对姓名、地名的OptimumSegment生成BiOptimumSegment：\r\n");
                    Console.WriteLine(e.Info);
                    break;
                case SegmentStage.FinishSegment:
                    Console.WriteLine("\r\n==== 最终识别结果：\r\n");
                    Console.WriteLine(e.Info);
                    break;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// 分词
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
        public string[] Segment(string sentence)
        {
            List<string> list = new List<string>();
            List<WordResult[]> result = wordSegment.Segment(sentence, nKind);
            //int n = Utility.GetPOSValue("n");
            for (int i = 0; i < result.Count; i++)
            {
                for (int j = 1; j < result[i].Length - 1; j++)
                {
                    //if (result[i][j].nPOS == n)
                    //{
                    list.Add(result[i][j].sWord);
                    //}
                }
            }
            return list.ToArray();
        }

        public void AddWordToDictionary(string word)
        {
            var dict = this.Dictionary;
            dict.AddItem(word, Utility.GetPOSValue("n"), 10);
            dict.Save(dictPath + "coreDict.dct");
        }

        public void PrintWordsInfo(char c)
        {
            var dict = this.Dictionary;
            int ccid = Utility.CC_ID(c);
            Console.WriteLine("====================================\r\n汉字:{0}, ID ：{1}\r\n", Utility.CC_ID2Char(ccid), ccid);
            Console.WriteLine("  词长  频率  词性   词");
            for (int i = 0; i < dict.indexTable[ccid].nCount; i++)
            {
                Console.WriteLine("{0,5} {1,6} {2,5}  ({3}){4}",
                   dict.indexTable[ccid].WordItems[i].nWordLen,
                   dict.indexTable[ccid].WordItems[i].nFrequency,
                   Utility.GetPOSString(dict.indexTable[ccid].WordItems[i].nPOS),
                   Utility.CC_ID2Char(ccid),
                   dict.indexTable[ccid].WordItems[i].sWord);
            }
        }
        #endregion
    }
}