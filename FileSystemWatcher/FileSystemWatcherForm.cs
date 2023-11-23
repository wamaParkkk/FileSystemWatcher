using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FileSystemWatcher
{
    public partial class FileSystemWatcherForm : Form
    {
        delegate void view(string value, string type);
        private view view_event;
        
        string localPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"C:\amkor\k5\fcsp\Epoxy\"));
        string localPath2 = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"C:\amkor\k5\Epoxy_M\"));
        string answerFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\"));
        string ftpPath = "ftp://10.141.12.165/Material/";
        string user = "K5FC";
        string pwd = "K5fc123!";

        string sEpoxyFreezerInfo;

        FTPclient ftpClient;

        int iCnt;

        public FileSystemWatcherForm()
        {
            InitializeComponent();
            
            //FileCheck();
        }

        private void FileSystemWatcherForm_Load(object sender, EventArgs e)
        {
            if (INFO_LOAD())
            {
                ftpClient = new FTPclient(ftpPath + sEpoxyFreezerInfo + "/", user, pwd);
            }
            else
            {
                MessageBox.Show("해동기(냉동고) 정보를 불러오지 못했습니다", "알림");
            }
            
            iCnt = 0;

            checkTimer.Enabled = true;
        }

        private void FileSystemWatcherForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            checkTimer.Enabled = false;
        }

        private bool INFO_LOAD()
        {
            // "EpoxyFreezerInfo.txt" 파일 내용은 ftp서버의 폴더 명으로 지정함
            string sTmpData;
            string FileName = "EpoxyFreezerInfo.txt";

            if (File.Exists(answerFilePath + FileName))
            {
                byte[] bytes;
                using (var fs = File.Open(answerFilePath + FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    bytes = new byte[fs.Length];
                    fs.Read(bytes, 0, (int)fs.Length);
                    sTmpData = Encoding.Default.GetString(bytes);
                    sEpoxyFreezerInfo = sTmpData;
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        /*
        private void FileCheck()
        {
            System.IO.FileSystemWatcher watcher = new System.IO.FileSystemWatcher();
            watcher.Path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\"));
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "Test.txt";
            watcher.Changed += new FileSystemEventHandler(Changed);
            watcher.EnableRaisingEvents = true;
            view_event += new view(Form_view_event);
        }

        private void Changed(object source, FileSystemEventArgs e)
        {
            string msg = e.FullPath + " → " + e.ChangeType;
            Invoke(view_event, new object[] { msg, "0" });
        }

        private void Form_view_event(string value, string type)
        {
            textBox1.Clear();
            textBox1.Text += value + Environment.NewLine;
        }
        */
        private void checkTimer_Tick(object sender, EventArgs e)
        {
            _File_Check();

            listBoxHistory.SelectedIndex = listBoxHistory.Items.Count - 1;
            listBoxHistory.SelectedIndex = -1;
        }

        private void _File_Check()
        {
            if (ftpClient.FtpFileExists("Request.txt"))
            {
                _listBox_Log_Clear();

                listBoxHistory.Items.Add(string.Format("[{0}] Client → 재고 현황 파일 업로드(조회) 요청", DateTime.Now.ToString()));

                // 재고 현황 파일 ftp서버에 업로드
                if (_ftpFileUpload("epoxystock.fdb"))
                {
                    // 재고 현황 파일 업로드 요청에 대한 완료 응답
                    if (_ftpFileUpload2("RequestComplete.txt"))
                    {
                        listBoxHistory.Items.Add(string.Format("[{0}] Host → 재고 현황 파일 업로드 완료", DateTime.Now.ToString()));

                        ftpClient.FtpDelete("Request.txt");
                    }
                }                               
            }

            if (ftpClient.FtpFileExists("Update.txt"))
            {
                _listBox_Log_Clear();

                listBoxHistory.Items.Add(string.Format("[{0}] Client → 재고 현황 파일 업데이트 발생", DateTime.Now.ToString()));

                if (iCnt >= 4)
                {
                    if (sEpoxyFreezerInfo == "SMD_FR-D03")
                        File.Delete(localPath2 + "epoxystock.fdb");
                    else
                        File.Delete(localPath + "epoxystock.fdb");

                    // 재고 현황 파일 ftp서버에서 다운로드
                    if (_ftpFileDownload("epoxystock.fdb"))
                    {
                        // 재고 현황 파일 다운로드 완료
                        if (_ftpFileUpload2("UpdateComplete.txt"))
                        {
                            listBoxHistory.Items.Add(string.Format("[{0}] Host → 재고 현황 파일 다운로드(업데이트) 완료", DateTime.Now.ToString()));

                            ftpClient.FtpDelete("Update.txt");

                            ftpClient.FtpDelete("epoxystock.fdb");

                            iCnt = 0;
                        }
                    }
                }
                else
                {
                    iCnt++;
                }
            }
        }

        private bool _ftpFileUpload(string fileName)
        {
            if (sEpoxyFreezerInfo == "SMD_FR-D03")
            {
                if (ftpClient.Upload(localPath2 + fileName, fileName))
                    return true;
                else
                    return false;
            }
            else
            {
                if (ftpClient.Upload(localPath + fileName, fileName))
                    return true;
                else
                    return false;
            }
        }

        private bool _ftpFileUpload2(string fileName)
        {
            if (ftpClient.Upload(answerFilePath + fileName, fileName))
                return true;
            else
                return false;
        }

        private bool _ftpFileDownload(string fileName)
        {
            if (sEpoxyFreezerInfo == "SMD_FR-D03")
            {
                if (ftpClient.Download(fileName, localPath2 + fileName, true))
                    return true;
                else
                    return false;
            }
            else
            {
                if (ftpClient.Download(fileName, localPath + fileName, true))
                    return true;
                else
                    return false;
            }
        }

        private void _listBox_Log_Clear()
        {
            if (listBoxHistory.Items.Count > 100)
                listBoxHistory.Items.Clear();            
        }
    }
}
