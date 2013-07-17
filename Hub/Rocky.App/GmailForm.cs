using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace Rocky.App
{
    public partial class GmailForm : Form
    {
        long splitLength = 1024L * 1024L * 20L;

        public GmailForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string inFilePath = openFileDialog1.FileName, outFolderPath = folderBrowserDialog1.SelectedPath;
            if (!string.IsNullOrEmpty(txtPwd.Text) && !string.IsNullOrEmpty(inFilePath) && !string.IsNullOrEmpty(outFolderPath))
            {
                string fileName = Path.GetFileName(inFilePath);
                string encryptInFilePath = inFilePath,
                    encryptOutFilePath = Path.Combine(outFolderPath, fileName);
                CryptoManaged c = new CryptoManaged(string.Empty);
                c.EncryptFile(encryptInFilePath, encryptOutFilePath, splitLength, SplitFileMode.InputFileLength);
                var gmail = new MailClient();
                gmail.Config(MailClient.SystemMail.Gmail, txtPwd.Text);
                //gmail.Priority = System.Net.Mail.MailPriority.High;
                gmail.SetBody("Key 4 EncryptLib.", c.Key + "," + c.IV, Directory.GetFiles(outFolderPath, fileName + "_Part*.temp"));
                gmail.AddTo("ilovehaley.kid@gmail.com", string.Empty);
                try
                {
                    txtPwd.ReadOnly = false;
                    gmail.Send();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    txtPwd.ReadOnly = true;
                    Console.WriteLine("SendCompleted.");
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string inFilePath = openFileDialog1.FileName, outFolderPath = folderBrowserDialog1.SelectedPath;
            if (!string.IsNullOrEmpty(txtKey.Text) && !string.IsNullOrEmpty(inFilePath) && !string.IsNullOrEmpty(outFolderPath))
            {
                string temp = Path.GetFileNameWithoutExtension(inFilePath);
                temp = temp.Remove(temp.IndexOf("_"));
                string fileName = temp + Path.GetExtension(inFilePath);
                string[] keys = txtKey.Text.Split(',');
                CryptoManaged c = new CryptoManaged(keys[0], keys[1]);
                string decryptOutFilePath = Path.Combine(outFolderPath, "D_" + fileName);
                c.DecryptFile(Path.GetDirectoryName(inFilePath) + fileName, decryptOutFilePath, splitLength, SplitFileMode.InputFileLength);
            }
        }
    }
}