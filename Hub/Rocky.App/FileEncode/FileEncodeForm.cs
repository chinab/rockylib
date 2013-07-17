using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Rocky.App
{
    public partial class FileEncodeForm : Form
    {
        /// <summary>
        /// 需要转换的所有文件扩展名
        /// </summary>
        private List<string> extNameList;

        public FileEncodeForm()
        {
            InitializeComponent();
        }

        private void FileEncodeForm_Load(object sender, EventArgs e)
        {
            this.cmbSourceEncode.SelectedIndex = 4;
            this.cmbTargetEncode.SelectedIndex = 0;
        }

        private void buttonBrowser_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = AppDomain.CurrentDomain.BaseDirectory;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string[] strArray = textBox_fileFilter.Text.Trim().Split('|');
                if (strArray.Length % 2 > 0)  //不是2的倍数
                {
                    MessageBox.Show("文件过滤字符串设置有误!");
                    return;
                }
                if (extNameList == null)
                {
                    extNameList = new List<string>();
                }
                else
                {
                    extNameList.Clear();
                }
                bool hasAll = false;
                for (int i = 0; i < strArray.Length / 2; i++)
                {
                    string ext = Path.GetExtension(strArray[i * 2 + 1]).ToLower();
                    if (!hasAll && ext == ".*")
                    {
                        hasAll = true;
                    }
                    extNameList.Add(ext);
                }
                foreach (string file in Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.*", SearchOption.AllDirectories))
                {
                    if (extNameList.IndexOf(Path.GetExtension(file).ToLower()) > -1 || hasAll)
                    {
                        listSelectedFiles.Items.Add(file);
                    }
                }
            }
        }

        private void btnSelectFiles_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.Multiselect = true;
                fileDialog.InitialDirectory = folderBrowserDialog1.SelectedPath;
                if (textBox_fileFilter.Text.Trim() != string.Empty)
                {
                    fileDialog.Filter = textBox_fileFilter.Text.Trim();
                }
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string file in fileDialog.FileNames)
                    {
                        this.listSelectedFiles.Items.Add(file);
                    }
                }
            }
        }

        private void button_Clipboard_Click(object sender, EventArgs e)
        {
            string[] files = Clipboard.GetText().Split('\n');
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                if (file.IndexOf("\r") > -1)
                {
                    file = file.Replace("\r", string.Empty);
                }
                if (file.Trim() != string.Empty)
                {
                    listSelectedFiles.Items.Add(file);
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (this.listSelectedFiles.SelectedIndex > -1)
            {
                listSelectedFiles.Items.RemoveAt(this.listSelectedFiles.SelectedIndex);
            }
        }

        private void chkUnknownEncoding_CheckedChanged(object sender, EventArgs e)
        {
            cmbSourceEncode.Enabled = !chkUnknownEncoding.Checked;
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            this.txtResult.Text = "文件总数：" + listSelectedFiles.Items.Count.ToString() + "\r\n 正在执行......";
            this.Cursor = Cursors.WaitCursor;
            //真正执行转换
            int i = 0;
            try
            {
                for (i = 0; i < listSelectedFiles.Items.Count; i++)
                {
                    ConvertFileEncode(listSelectedFiles.Items[i].ToString());
                }
                this.txtResult.Text += "\r\n完成：" + listSelectedFiles.Items.Count.ToString();
            }
            catch (Exception ex)
            {
                this.txtResult.Text += "\r\n执行文件：" + listSelectedFiles.Items[i].ToString() + "转换时出现错误：" + ex.Message + "\r\n已经完成：" + i.ToString();
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// 根据下拉框的值返回相应的编码
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private Encoding GetSelectEncoding(int i)
        {
            Encoding encode;
            switch (i)
            {
                case 0:
                    encode = System.Text.Encoding.UTF8;
                    break;
                case 1:
                    encode = System.Text.Encoding.UTF7;
                    break;
                case 2:
                    encode = System.Text.Encoding.Unicode;
                    break;
                case 3:
                    encode = System.Text.Encoding.ASCII;
                    break;
                case 4:
                    encode = System.Text.Encoding.GetEncoding(936); //GB2321
                    break;
                case 5:
                    encode = System.Text.Encoding.GetEncoding("BIG5");
                    break;
                default:
                    encode = System.Text.Encoding.UTF8;
                    break;
            }
            return encode;
        }
        private void ConvertFileEncode(string filePath)
        {
            Encoding oriEncode;
            if (chkUnknownEncoding.Checked)  //编码识别
            {
                IdentifyEncoding ie = new IdentifyEncoding();
                FileInfo fi = new FileInfo(filePath);
                string encodingName = ie.GetEncodingName(fi);
                if (encodingName == "UNKNOWN")
                {
                    txtResult.Text += string.Format("\r\n{0}文件格式不正确或已损坏。 ", filePath);
                    return;
                }
                else
                {
                    oriEncode = Encoding.GetEncoding(encodingName);
                }
            }
            else
            {
                oriEncode = GetSelectEncoding(cmbSourceEncode.SelectedIndex);
            }
            string text = File.ReadAllText(filePath, oriEncode);
            if (chkIsBackup.Checked)  //备份
            {
                File.WriteAllText(filePath + ".bak", text, oriEncode);
            }
            File.WriteAllText(filePath, text, GetSelectEncoding(cmbTargetEncode.SelectedIndex));
            if (filePath.LastIndexOf("[1]") != -1)
            {
                File.Move(filePath, filePath.Replace("[1]", string.Empty));
            }
        }
    }
}