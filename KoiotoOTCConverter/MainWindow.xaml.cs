using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace KoiotoOTCConverter
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        const string otcVer = "Rev2";   // Open Taiko Chart Version
        string appDirectory = Path.GetDirectoryName(Path.GetFullPath(Environment.GetCommandLineArgs()[0])); // アプリの実行ディレクトリ

        // 稚拙なコードなのでジロジロ見ないでください！！
        public MainWindow()
        {
            InitializeComponent();

            // バージョン表記
            FormMain.Title += " Ver." + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            // [メニュー]ファイル→開く

            // WPFだと複数選択不可なのでFormのダイアログを呼ぶ
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.InitialDirectory = Path.GetDirectoryName(appDirectory);
            dialog.Filter = ".tjaファイル(*.tja)|*.tja";
            dialog.Multiselect = true;
            System.Windows.Forms.DialogResult dr = dialog.ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                fileReader(dialog.FileNames, 0, dialog.FileNames.Length);
            }
        }

        private void OpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            // [メニュー]ファイル→フォルダーを開く

            // WPFだと複数選択不可なのでFormのダイアログを呼ぶ
            //var dialog = new System.Windows.Forms.FolderBrowserDialog();
            //dialog.ShowDialog();
            //dialog.InitialDirectory = Path.GetDirectoryName(appDirectory);
            //dialog.Multiselect = true;
            //dialog.ValidateNames = false;
            //dialog.CheckFileExists = false;
            //dialog.CheckPathExists = true;
            //System.Windows.Forms.DialogResult dr = dialog.ShowDialog();

            //if (dr == System.Windows.Forms.DialogResult.OK)
            //{
            //    fileReader(dialog.FileNames, 0, dialog.FileNames.Length);
            //}
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // [メニュー]ファイル→終了
            Application.Current.Shutdown();
        }

        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Released)
            {
                // ドラッグをEscでキャンセルしたときに終了してしまうのを回避する小癪なやり方
                if (Keyboard.IsKeyDown(Key.Escape))
                {
                    // Escキーで終了
                    Application.Current.Shutdown();
                }
            }
        }

        private void TCIBox_Loaded(object sender, RoutedEventArgs e)
        {
            TCIBoxWrite("変換形式:Open Taiko Chart " + otcVer);

            // コマンドライン引数の処理
            string[] files = Environment.GetCommandLineArgs();

            if (files.Count() > 1)
            {
                fileReader(files, 1, files.Length);
                Application.Current.Shutdown();
            }
        }

        private void TCIBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            // TCIBoxでのD&D許可
            e.Effects = DragDropEffects.Copy;
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
        }

        private void FormMain_Drop(object sender, DragEventArgs e)
        {
            // D&D処理
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            fileReader(files, 0, files.Length);
        }

        private void fileReader(string[] files, int startindex, int endindex)
        {
            for (int i = startindex; i < endindex; i++)
            {
                TCIBoxWrite();
                TCIBoxWrite(files[i]);

                // ディレクトリかファイルか判別
                if (File.GetAttributes(files[i]).HasFlag(FileAttributes.Directory))
                {
                    // ディレクトリが読み込まれた場合

                    // ダイアログが埋もれないようにアクティブ化
                    FormMain.Activate();

                    string messageBoxText = "フォルダーが読み込まれました。フォルダー内にある全ての.tjaを変換しますか？";
                    string caption = "警告";
                    MessageBoxButton button = MessageBoxButton.YesNo;
                    MessageBoxImage icon = MessageBoxImage.Exclamation;

                    TCIBoxWrite();
                    TCIBoxWrite(caption + "：" + messageBoxText);
                    TCIBox.ScrollToEnd();
                    MessageBoxResult result = MessageBox.Show(this, files[i] + "\r" + messageBoxText, caption, button, icon);

                    // ダイアログでの選択を判別
                    if (result == MessageBoxResult.Yes)
                    {
                        // はいを選んだ場合
                        TCIBoxWrite();
                        TCIBoxWrite(">>はい");

                        // .tjaファイルを検索
                        IEnumerable<string> tjafiles = Directory.EnumerateFiles(files[i], "*.tja", SearchOption.AllDirectories);

                        if (tjafiles.Count() > 0)
                        {
                            foreach (string str in tjafiles)
                            {
                                TCIBoxWrite();
                                TCIBoxWrite(str);
                                OTCConvert(str);
                            }

                            messageBoxText = tjafiles.Count() + "件の.tjaファイルを変換しました。";
                            caption = "情報";
                            button = MessageBoxButton.OK;
                            icon = MessageBoxImage.Information;
                        }
                        else
                        {
                            messageBoxText = ".tjaファイルがありませんでした。";
                            caption = "情報";
                            button = MessageBoxButton.OK;
                            icon = MessageBoxImage.Information;

                            TCIBoxWrite();
                            TCIBoxWrite(messageBoxText);
                        }
                        TCIBox.ScrollToEnd();
                        MessageBox.Show(this, messageBoxText, caption, button, icon);
                    }
                    else
                    {
                        // いいえを選んだ場合
                        TCIBoxWrite();
                        TCIBoxWrite(">>いいえ");
                    }
                }
                else
                {
                    // ファイルが読み込まれた場合
                    OTCConvert(files[i]);
                }
            }

            TCIBoxWrite();
            TCIBoxWrite("処理完了");

            TCIBox.ScrollToEnd();
        }

        private void OTCConvert(string filePath)
        {
            // .tjaのみ許可
            if (Path.GetExtension(filePath) == ".tja")
            {
                var encord = Encoding.GetEncoding("SHIFT_JIS");
                var tja = new StreamReader(filePath, encord);

                string tjaLine;

                OpenTaikoChartInfomation tci = new OpenTaikoChartInfomation();

                OpenTaikoChartInfomation_Courses tcic = new OpenTaikoChartInfomation_Courses();

                OpenTaikoChartCourse tcc = new OpenTaikoChartCourse();

                int cCourse = 0;    // Courseの数（.Count()で代用可能かも）
                int cLine = 0;      // 現在読み込んでいる.tjaの行数

                var artist = new List<string>() { };
                var creator = new List<string>() { };
                var courses = new List<OpenTaikoChartInfomation_Courses>() { };

                var multiple = new List<string>() { };

                var balloons = new List<int>() { };
                var measures = new List<List<string>>() { };
                var measureLine = new List<string>() { };

                string nowdifficulty = "Oni";   // 現在の難易度
                int nowlevel = 0;               // 現在のレベル
                int playside = 0;               // 現在のプレイサイド 0:シングル, 1:ダブル1P, 2:ダブル2P

                courseInitialize();

                // 1行ずつ処理
                while (!tja.EndOfStream)
                {
                    tjaLine = tja.ReadLine();

                    cLine++;

                    comment = tjaLine.IndexOf(commentStr);

                    if (comment >= 0)
                    {
                        // コメント文の削除
                        string str = tjaLine.Substring(comment);
                        attentionMsg.Add("注意：[" + cLine + "行目] コメント文 " + str + " は削除されます。");

                        tjaLine = tjaLine.Substring(0, comment);
                    }

                    tjaLine = tjaLine.Trim();

                    title = tjaLine.IndexOf(titleStr);
                    subtitle = tjaLine.IndexOf(subtitleStr);
                    wave = tjaLine.IndexOf(waveStr);
                    bgimage = tjaLine.IndexOf(bgimageStr);
                    bgmovie = tjaLine.IndexOf(bgmovieStr);
                    movieoffset = tjaLine.IndexOf(movieoffsetStr);
                    bpm = tjaLine.IndexOf(bpmStr);
                    offset = tjaLine.IndexOf(offsetStr);
                    demostart = tjaLine.IndexOf(demostartStr);

                    colon = tjaLine.IndexOf(colonStr);

                    course = tjaLine.IndexOf(courseStr);
                    level = tjaLine.IndexOf(levelStr);
                    style = tjaLine.IndexOf(styleStr);

                    scoreinit = tjaLine.IndexOf(scoreinitStr);
                    scorediff = tjaLine.IndexOf(scorediffStr);
                    balloon = tjaLine.IndexOf(balloonStr);

                    comma = tjaLine.IndexOf(commaStr);
                    sharp = tjaLine.IndexOf(sharpStr);

                    start = tjaLine.IndexOf(startStr);
                    end = tjaLine.IndexOf(endStr);

                    bmscroll = tjaLine.IndexOf(bmscrollStr);
                    hbscroll = tjaLine.IndexOf(hbscrollStr);

                    if (title == 0)
                    {
                        tci.title = tjaLine.Substring(titleStr.Length);
                    }

                    if (subtitle == 0)
                    {
                        string str;
                        str = tjaLine.Substring(subtitleStr.Length);

                        if (str.Length > 1)
                        {
                            switch (str.Substring(0, 2))
                            {
                                // 先頭2文字による処理分け
                                case "--":
                                    artist.Add(str.Substring(2));
                                    tci.artist = artist.ToArray();
                                    break;
                                case "++":
                                    tci.subtitle = str.Substring(2);
                                    break;
                                default:
                                    tci.subtitle = str;
                                    break;
                            }
                        }
                        else
                        {
                            tci.subtitle = str;
                        }
                    }

                    if (wave == 0)
                    {
                        tci.audio = tjaLine.Substring(waveStr.Length);
                    }

                    if (bgimage == 0)
                    {
                        string str = tjaLine.Substring(bgimageStr.Length);

                        if (str != "")
                        {
                            tci.background = str;
                        }
                    }

                    if (bgmovie == 0)
                    {
                        string str = tjaLine.Substring(bgmovieStr.Length);

                        if (str != "")
                        {
                            tci.background = str;
                        }
                    }

                    if (movieoffset == 0)
                    {
                        // .tciではオフセットが逆
                        tci.movieoffset = doubleSubstring(tjaLine, movieoffsetStr.Length) * -1;
                    }

                    if (bpm == 0)
                    {
                        tci.bpm = doubleSubstring(tjaLine, bpmStr.Length);
                    }

                    if (offset == 0)
                    {
                        // .tciではオフセットが逆
                        tci.offset = doubleSubstring(tjaLine, offsetStr.Length) * -1;
                    }

                    if (demostart == 0)
                    {
                        tci.songpreview = doubleSubstring(tjaLine, demostartStr.Length);
                    }

                    if (course == 0)
                    {
                        string str = tjaLine.Substring(courseStr.Length);
                        switch (str)
                        {
                            case "Easy":
                            case "Normal":
                            case "Hard":
                            case "Oni":
                            case "Edit":
                                nowdifficulty = str;
                                break;
                            case "0":
                                nowdifficulty = "Easy";
                                break;
                            case "1":
                                nowdifficulty = "Normal";
                                break;
                            case "2":
                                nowdifficulty = "Hard";
                                break;
                            case "4":
                                nowdifficulty = "Edit";
                                break;
                            case "3":
                            default:
                                nowdifficulty = "Oni";
                                break;
                        }
                    }

                    if (level == 0)
                    {
                        nowlevel = intSubstring(tjaLine, levelStr.Length);
                    }

                    if (style == 0)
                    {
                        string str = tjaLine.Substring(styleStr.Length);
                        switch (str)
                        {
                            case "Double":
                            case "Couple":
                            case "2":
                                bdoubleplay = true;
                                break;
                            default:
                                bdoubleplay = false;
                                break;
                        }
                    }

                    if (scoreinit == 0)
                    {
                        tcc.scoreinit = intSubstring(tjaLine, scoreinitStr.Length);
                    }

                    if (scorediff == 0)
                    {
                        tcc.scorediff = intSubstring(tjaLine, scorediffStr.Length);
                    }

                    if (balloon == 0)
                    {
                        string str = tjaLine.Substring(balloonStr.Length);

                        str = str.Replace(" ", "");
                        str = str.TrimEnd(',');

                        if (str != "")
                        {
                            balloons = str.Split(',').Select(a => int.Parse(a)).ToList();

                            tcc.balloon = balloons.ToArray();
                        }
                    }

                    if (end == 0)
                    {
                        bmeasures = false;

                        tcic.difficulty = nowdifficulty.ToLower();
                        tcic.level = nowlevel;

                        tcc.measures = measures.Select(a => a.ToArray()).ToArray();

                        if (attentionMsg.Count > 0)
                        {
                            // .tccに関する警告メッセージの出力
                            TCIBoxWrite();
                            foreach (var msg in attentionMsg)
                            {
                                TCIBoxWrite(msg);
                            }
                        }

                        bool dupDificculty = false; // 被っている難易度が存在するか？
                        int dupCourse = 0;          // 被っている難易度の位置

                        if (cCourse >= 1)
                        {
                            foreach (var item in tci.courses)
                            {
                                if (item.difficulty == nowdifficulty.ToLower())
                                {
                                    // 難易度の被りを検出
                                    dupDificculty = true;
                                    break;
                                }
                                dupCourse++;
                            }
                        }

                        if (dupDificculty)
                        {
                            // 既存のCourseに上書き
                            if (bdoubleplay)
                            {
                                // DP時
                                switch (playside)
                                {
                                    case 1:
                                    case 2:
                                        OTCWrite<OpenTaikoChartCourse>(tcc, Path.GetDirectoryName(filePath), nowdifficulty + "_" + playside + "P", ".tcc");
                                        multiple.Add(nowdifficulty + "_" + playside + "P.tcc");
                                        tci.courses[dupCourse].multiple = multiple.ToArray();
                                        break;
                                    default:
                                        TCIBoxWrite();
                                        TCIBoxWrite("注意：難易度" + nowdifficulty + "は2人用の譜面が必要ですが、譜面数が足りていません。");
                                        for (int i = 1; i <= 2; i++)
                                        {
                                            // 暫定的に同じ譜面で埋める
                                            OTCWrite<OpenTaikoChartCourse>(tcc, Path.GetDirectoryName(filePath), nowdifficulty + "_" + i + "P", ".tcc");
                                            tci.courses[dupCourse].single = nowdifficulty + "_" + i + "P.tcc";
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                // SP時
                                OTCWrite<OpenTaikoChartCourse>(tcc, Path.GetDirectoryName(filePath), nowdifficulty, ".tcc");
                                tci.courses[dupCourse].single = nowdifficulty + ".tcc";
                            }

                        }
                        else
                        {
                            // 新しいCourseを作成
                            if (bdoubleplay)
                            {
                                // DP時
                                switch (playside)
                                {
                                    case 1:
                                    case 2:
                                        OTCWrite<OpenTaikoChartCourse>(tcc, Path.GetDirectoryName(filePath), nowdifficulty + "_" + playside + "P", ".tcc");
                                        multiple.Add(nowdifficulty + "_" + playside + "P.tcc");
                                        break;
                                    default:
                                        TCIBoxWrite();
                                        TCIBoxWrite("注意：難易度" + nowdifficulty + "は2人用の譜面が必要ですが、譜面数が足りていません。");
                                        for (int i = 1; i <= 2; i++)
                                        {
                                            // 暫定的に同じ譜面で埋める
                                            OTCWrite<OpenTaikoChartCourse>(tcc, Path.GetDirectoryName(filePath), nowdifficulty + "_" + i + "P", ".tcc");
                                            multiple.Add(nowdifficulty + "_" + i + "P.tcc");
                                        }
                                        tcic.multiple = multiple.ToArray();
                                        break;
                                }
                            }
                            else
                            {
                                // SP時
                                OTCWrite<OpenTaikoChartCourse>(tcc, Path.GetDirectoryName(filePath), nowdifficulty, ".tcc");
                                tcic.single = nowdifficulty + ".tcc";
                            }

                            // Courseの追加
                            courses.Add(tcic);
                            tci.courses = courses.ToArray();

                            cCourse++;
                        }

                        // 一部の変数をリセット
                        courseInitialize();
                        tcic = new OpenTaikoChartInfomation_Courses();
                        tcc = new OpenTaikoChartCourse();
                        measures.Clear();
                    }

                    if (colon >= 0)
                    {
                        // ヘッダーチェック
                        string str = tjaLine.Substring(0, colon + colonStr.Length);
                        switch (str)
                        {
                            case titleStr:
                            case subtitleStr:
                            case waveStr:
                            case bgimageStr:
                            case bgmovieStr:
                            case movieoffsetStr:
                            case bpmStr:
                            case offsetStr:
                            case demostartStr:
                            case courseStr:
                            case levelStr:
                            case styleStr:
                            case scoreinitStr:
                            case scorediffStr:
                            case balloonStr:
                                break;
                            default:
                                attentionMsg.Add("注意：[" + cLine + "行目] " + str + " はKoiotoでサポートされていません。");
                                break;
                        }
                    }

                    if (sharp == 0 & start != 0 & end != 0)
                    {
                        // 命令文チェック
                        if (!tccUnsupported(tjaLine))
                        {
                            attentionMsg.Add("注意：[" + cLine + "行目] " + tjaLine + " はKoiotoでサポートされていません。");
                        }
                    }

                    if (bmscroll == 0 | hbscroll == 0)
                    {
                        // 唯一の#START～#END外の命令文なので特別扱い
                        // 命令文の挿入
                        string str = tccInstructionConvert(tjaLine);
                        measureLine.Add(str);
                    }

                    if (bmeasures)
                    {
                        if (tjaLine.Length > 0)
                        {
                            if (sharp == 0)
                            {
                                // 命令文の挿入
                                string str = tccInstructionConvert(tjaLine);
                                measureLine.Add(str);
                            }
                            else
                            {
                                // 小節の挿入
                                string str = tjaLine.Replace(commaStr, "");
                                measureLine.Add(str);

                                if (comma >= 0)
                                {
                                    // 小節区切り
                                    measures.Add(measureLine);
                                    measureLine = new List<string>() { };
                                }
                            }
                        }
                    }

                    if (start == 0)
                    {
                        bmeasures = true;

                        if (tjaLine.Length > startStr.Length)
                        {
                            // #START P1, #START P2への対応
                            string str = tjaLine.Substring(startStr.Length + 1);

                            switch (str)
                            {
                                case "P1":
                                    playside = 1;
                                    bdoubleplay = true;
                                    break;
                                case "P2":
                                    playside = 2;
                                    bdoubleplay = true;
                                    break;
                                default:
                                    playside = 0;
                                    break;
                            }
                        }
                        else
                        {
                            playside = 0;
                        }
                    }
                }

                OTCWrite<OpenTaikoChartInfomation>(tci, Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath), ".tci");

                tja.Close();
            }
            else
            {
                TCIBoxWrite();
                TCIBoxWrite("エラー：読み込めるのは.tjaファイルのみです。");
            }
        }

        private void OTCWrite<Type>(Type otc, string directory, string filename, string extension)
        {
            // .tti .ttcファイルの書き込み
            using (var fs = new FileStream(directory + @"\" + filename + extension, FileMode.Create, FileAccess.Write))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.UTF8, true, true))                    // インデントと改行の挿入
            {
                var otcType = otc.GetType();
                var serializer = new DataContractJsonSerializer(otcType);
                serializer.WriteObject(writer, otc);

                TCIBoxWrite();
                TCIBoxWrite(directory + "\\" + filename + extension + " が作成されました。");
            }
        }

        private void TCIBoxWrite()
        {
            TCIBoxWrite("");
        }
        private void TCIBoxWrite(string text)
        {
            TCIBox.AppendText("\r" + text);
        }

        private string tccInstructionConvert(string tjaLine)
        {
            // 命令文の変換
            tjaLine = tjaLine.Replace(bpmchangeStr, bpmchangeTcc);
            tjaLine = tjaLine.Replace(gogostartStr, gogostartTcc);
            tjaLine = tjaLine.Replace(measureStr, measureTcc);

            tjaLine = tjaLine.ToLower();

            return tjaLine;
        }

        private bool tccUnsupported(string tjaLine)
        {
            // 命令文がKoiotoでサポートされているかのチェック
            int i = tjaLine.IndexOf(" ");
            if (i >= 0)
            {
                tjaLine = tjaLine.Substring(0, i + sharpStr.Length);
            }

            switch (tjaLine)
            {
                case bpmchangeStr:
                case gogostartStr:
                case gogoendStr:
                case measureStr:
                case scrollStr:
                case delayStr:
                    return true;
                default:
                    return false;
            }
        }

        private int intSubstring(string tjaLine, int i)
        {
            return (int)Math.Truncate(doubleSubstring(tjaLine, i));
        }

        private double doubleSubstring(string tjaLine, int i)
        {
            tjaLine = tjaLine.Substring(i);

            if (tjaLine.Length != 0)
            {
                return double.Parse(tjaLine);
            }
            else
            {
                return 0;
            }
        }

        [DataContract]
        public class OpenTaikoChartInfomation
        {
            [DataMember(Order = 1)]
            public string title { get; set; }
            [DataMember(Order = 2)]
            public string subtitle { get; set; }
            [DataMember(Order = 3)]
            public string[] artist { get; set; }
            [DataMember(Order = 4)]
            public string[] creator { get; set; }
            [DataMember(Order = 5)]
            public string audio { get; set; }
            [DataMember(Order = 6)]
            public string background { get; set; }
            [DataMember(Order = 7)]
            public double? movieoffset { get; set; }
            [DataMember(Order = 8)]
            public double? bpm { get; set; }
            [DataMember(Order = 9)]
            public double? offset { get; set; }
            [DataMember(Order = 10)]
            public double? songpreview { get; set; }
            [DataMember(Order = 11)]
            public string albumart { get; set; }
            [DataMember(Order = 12)]
            public OpenTaikoChartInfomation_Courses[] courses { get; set; }
        }

        [DataContract]
        public class OpenTaikoChartInfomation_Courses
        {
            [DataMember(Order = 1)]
            public string difficulty { get; set; }
            [DataMember(Order = 2)]
            public int? level { get; set; }
            [DataMember(Order = 3)]
            public string single { get; set; }
            [DataMember(Order = 4)]
            public string[] multiple { get; set; }
        }

        [DataContract]
        public class OpenTaikoChartCourse
        {
            [DataMember(Order = 1)]
            public int? scoreinit { get; set; }
            [DataMember(Order = 2)]
            public int? scorediff { get; set; }
            [DataMember(Order = 3)]
            public int? scoreshinuchi { get; set; }
            [DataMember(Order = 4)]
            public int[] balloon { get; set; }  // ?を外してみる
            [DataMember(Order = 5)]
            public string[][] measures { get; set; }
        }

        private void courseInitialize()
        {
            bdoubleplay = false;
            bmeasures = false;
            attentionMsg = new List<string>() { };
        }

        bool bdoubleplay = false;   // ダブルプレイかどうか？
        bool bmeasures = false;     // #START～#END内かどうか？

        List<string> attentionMsg;  // 警告メッセージのリスト

        // 汎用
        int comment, colon, sharp, comma;
        const string commentStr = "//";
        const string colonStr = ":";
        const string sharpStr = "#";
        const string commaStr = ",";

        // .tci対応
        int title, subtitle, wave, bgimage, bgmovie, movieoffset, bpm, offset, demostart;
        const string titleStr = "TITLE:";
        const string subtitleStr = "SUBTITLE:";
        const string waveStr = "WAVE:";
        const string bgimageStr = "BGIMAGE:";
        const string bgmovieStr = "BGMOVIE:";
        const string movieoffsetStr = "MOVIEOFFSET:";
        const string bpmStr = "BPM:";
        const string offsetStr = "OFFSET:";
        const string demostartStr = "DEMOSTART:";

        // .tci対応（コース別）
        int course, level, style;
        const string courseStr = "COURSE:";
        const string levelStr = "LEVEL:";
        const string styleStr = "STYLE:";

        // .tci未対応
        //int songvol, sevol, life, side, scoremode, game, genre, total, balloonnor, balloonexp, balloonmas, hiddenbranch, exam1, exam2, exam3, gaugeincr;
        //const string songvolStr = "SONGVOL:";
        //const string sevolStr = "SEVOL:";
        //const string lifeStr = "LIFE:";
        //const string sideStr = "SIDE:";
        //const string scoremodeStr = "SCOREMODE:";
        //const string gameStr = "GAME:";   // 多分一生対応されない
        //const string genreStr = "GENRE:";
        //const string totalStr = "TOTAL:";
        //const string balloonnorStr = "BALLOONNOR:";
        //const string balloonexpStr = "BALLOONEXP:";
        //const string balloonmasStr = "BALLOONMAS:";
        //const string hiddenbranchStr = "HIDDENBRANCH:";
        //const string exam1Str = "EXAM1:";
        //const string exam2Str = "EXAM2:";
        //const string exam3Str = "EXAM3:";
        //const string gaugeincrStr = "GAUGEINCR:";

        // .tcc対応
        int scoreinit, scorediff, balloon;
        const string scoreinitStr = "SCOREINIT:";
        const string scorediffStr = "SCOREDIFF:";
        const string balloonStr = "BALLOON:";

        int start, end;
        const string startStr = "#START";
        const string endStr = "#END";

        //int bpmchange, gogostart, gogoend, measure, scroll, delay;
        const string bpmchangeStr = "#BPMCHANGE ";
        const string gogostartStr = "#GOGOSTART";
        const string gogoendStr = "#GOGOEND";
        const string measureStr = "#MEASURE ";
        const string scrollStr = "#SCROLL ";
        const string delayStr = "#DELAY ";

        // .tcc未対応
        int bmscroll, hbscroll;
        const string bmscrollStr = "#BMSCROLL";
        const string hbscrollStr = "#HBSCROLL";
        //int section, branchstart, branchend, branchN, branchE, branchM, levelhold, barlineoff, barlineon, jposscroll, senotechange, nextsong, sudden;
        //const string sectionStr = "#SECTION";
        //const string branchstartStr = "#BRANCHSTART ";
        //const string branchendStr = "#BRANCHEND";
        //const string branchNStr = "#N";
        //const string branchEStr = "#E";
        //const string branchMStr = "#M";
        //const string levelholdStr = "#LEVELHOLD";
        //const string barlineoffStr = "#BARLINEOFF";
        //const string barlineonStr = "#BARLINEON";
        //const string jposscrollStr = "#JPOSSCROLL ";
        //const string senotechangeStr = "#SENOTECHANGE";
        //const string nextsongStr = "#NEXTSONG ";
        //const string suddenStr = "#SUDDEN ";

        // .tcc形式の命令文
        string bpmchangeTcc = "#bpm ";
        string gogostartTcc = "#gogobegin";
        string measureTcc = "#tsign ";

        // 生成されたファイルの編集しやすさを考えるとnullが入るより""入れたほうがいいかもしれない
        // /が　＼/みたいな書き方で出力されてしまう（JSONではそれが正しいらしい）
        // ＼は＼＼と出力するのがOTCの規定らしい
        // 出力ディレクトリを選べる機能
        // 出力後のファイルを整形しなおす機能（改行など）
        // SUBTITLE:の振り分け先（subtitle,artist）を++,--のケースを含めて設定できる機能
        // 事前に設定したcreatorが格納される機能
        // 事前に設定した補正値をoffsetにかける機能
        // Easy, Normal, Hard, Oni, Editの各.tccファイルを事前に設定した名前で出力できる機能（"0_Easy.tcc","1_Normal.tcc"など）
        // 上の機能、できれば.tjaファイル名の名前も指定できるようにしたいところ。{filename}とか
        // #N,#E,#Mが他の命令文でも検知してしまう問題への対応
        // 真打の自動計算
    }
}
