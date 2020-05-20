using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace KoiotoOTCConverter
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        string otcVer = "Rev2"; // 対応しているOpenTaikoChartのバージョン
        string appDirectory = Path.GetDirectoryName(Path.GetFullPath(Environment.GetCommandLineArgs()[0])); // KoiotoOTCConverterが実行されているディレクトリ
        SettingWindow.Setting setting;  // 設定ファイルの値

        public MainWindow()
        {
            InitializeComponent();

            // バージョン表記
            FormMain.Title += " Ver." + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            // 変換形式の表記
            TextBoxMainWrite("変換形式:Open Taiko Chart " + otcVer);
        }

        private void TextBoxMain_Loaded(object sender, RoutedEventArgs e)
        {
            // コマンドライン引数の処理
            string[] files = Environment.GetCommandLineArgs();

            if (files.Count() > 1)
            {
                // コマンドライン引数が存在する場合
                FileReader(files, 1, files.Length);
                Application.Current.Shutdown();
            }
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

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            // [ファイル]→開く
            // Windows API Code Pack使用
            var dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Path.GetDirectoryName(appDirectory);
            dialog.Filters.Add(new CommonFileDialogFilter("tjaファイル", "*.tja"));
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                FileReader(dialog.FileNames.ToArray(), 0, dialog.FileNames.Count());
            }
        }

        private void OpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            // [ファイル]→フォルダーを開く
            // Windows API Code Pack使用
            var dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Path.GetDirectoryName(appDirectory);
            dialog.IsFolderPicker = true;
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                FileReader(dialog.FileNames.ToArray(), 0, dialog.FileNames.Count());
            }
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            // [ファイル]→設定
            var sw = new SettingWindow();
            sw.Owner = this;
            sw.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // [ファイル]→終了
            Application.Current.Shutdown();
        }

        private void Github_Click(object sender, RoutedEventArgs e)
        {
            // [ヘルプ]→GitHubのページを開く
            System.Diagnostics.Process.Start("https://github.com/nyoro-wrl/KoiotoOTCConverter");
        }

        private void TextBoxMain_PreviewDragOver(object sender, DragEventArgs e)
        {
            // TextBoxMainでのD&D許可
            e.Effects = DragDropEffects.Copy;
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
        }

        private void FormMain_Drop(object sender, DragEventArgs e)
        {
            // MainWindowへのD&D処理
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            FileReader(files, 0, files.Length);
        }

        private void FileReader(string[] files, int startIndex, int endIndex)
        {
            // 設定ファイルの値読み込み
            setting = new SettingWindow.Setting();
            setting = SettingWindow.LoadSetting(setting);

            for (int fileIndex = startIndex; fileIndex < endIndex; fileIndex++)
            {
                TextBoxMainWrite();
                TextBoxMainWrite(files[fileIndex]);

                // ディレクトリかファイルか判別
                if (File.GetAttributes(files[fileIndex]).HasFlag(FileAttributes.Directory))
                {
                    // ディレクトリが読み込まれた場合

                    // ダイアログが埋もれないようにアクティブ化
                    FormMain.Activate();

                    string msgText = "フォルダーが読み込まれました。フォルダー内にある全ての.tjaを変換しますか？";
                    string msgCaption = "警告";

                    TextBoxMainWrite();
                    TextBoxMainWrite(msgCaption + "：" + msgText);
                    TextBoxMain.ScrollToEnd();

                    var result = MessageBox.Show(this, files[fileIndex] + "\r" + msgText, msgCaption, MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                    // ダイアログでの選択を判別
                    if (result == MessageBoxResult.Yes)
                    {
                        // はいを選んだ場合
                        TextBoxMainWrite();
                        TextBoxMainWrite(">>はい");

                        // ディレクトリから.tjaファイルを検索（サブディレクトリを含む）
                        var tjaFiles = Directory.EnumerateFiles(files[fileIndex], "*.tja", SearchOption.AllDirectories);

                        if (tjaFiles.Count() >= 1)
                        {
                            // .tjaファイルが1件以上の場合
                            foreach (string tjaFilePath in tjaFiles)
                            {
                                TextBoxMainWrite();
                                TextBoxMainWrite(tjaFilePath);
                                OTCConvert(tjaFilePath);
                            }

                            msgText = tjaFiles.Count() + "件の.tjaファイルを変換しました。";
                        }
                        else
                        {
                            // .tjaファイルが0件の場合
                            msgText = ".tjaファイルがありませんでした。";
                        }

                        TextBoxMainWrite();
                        TextBoxMainWrite(msgText);
                        TextBoxMain.ScrollToEnd();

                        MessageBox.Show(this, msgText, "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // いいえを選んだ場合
                        TextBoxMainWrite();
                        TextBoxMainWrite(">>いいえ");
                    }
                }
                else
                {
                    // ファイルが読み込まれた場合
                    OTCConvert(files[fileIndex]);
                }
            }

            TextBoxMainWrite();
            TextBoxMainWrite("処理完了");

            TextBoxMain.ScrollToEnd();
        }

        private void OTCConvert(string filePath)
        {
            // .tjaのみ許可
            if (Path.GetExtension(filePath) == ".tja")
            {
                var tja = new StreamReader(filePath, Encoding.GetEncoding("SHIFT_JIS"));

                string tjaLine;     // 現在の行
                int countLine = 0;  // 現在の行数

                var tci = new OpenTaikoChartInfomation();

                var tcic = new OpenTaikoChartInfomation_Courses();

                var tcc = new OpenTaikoChartCourse();

                var artistList = new List<string>() { };
                var creatorList = new List<string>() { };
                var courseList = new List<OpenTaikoChartInfomation_Courses>() { };

                var multipleList = new List<string>() { };

                var balloonList = new List<int>() { };
                var measures = new List<List<string>>() { };
                var measureLine = new List<string>() { };

                string nowDifficulty = "Oni";   // 現在の難易度
                int nowLevel = 0;               // 現在のレベル
                int nowPlayside = 0;            // 現在のプレイサイド 0:シングル, 1:ダブル1P, 2:ダブル2P 3以上:以降の人数に応じて
                int? nowDoubleplayScoreinit = null;     // [DP時]現在のSCOREINIT
                int? nowDoubleplayScorediff = null;     // [DP時]現在のSCOREDIFF
                int? nowDoubleplayScoreshinuchi = null; // [DP時]現在のSCORESHINUCHI

                bool isMeasure = false;     // #START～#END内かどうか？
                var attentionMsg = new List<string>() { };  // 警告メッセージのリスト

                // 1行ずつ処理
                while (!tja.EndOfStream)
                {
                    tjaLine = tja.ReadLine();

                    countLine++;

                    comment = tjaLine.IndexOf(commentStr);

                    if (comment >= 0)
                    {
                        // コメント文の削除
                        string str = tjaLine.Substring(comment);
                        attentionMsg.Add("注意：[" + countLine + "行目] コメント文 " + str + " は削除されます。");

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
                    artist = tjaLine.IndexOf(artistStr);
                    creator = tjaLine.IndexOf(creatorStr);
                    albumart = tjaLine.IndexOf(albumartStr);

                    colon = tjaLine.IndexOf(colonStr);

                    course = tjaLine.IndexOf(courseStr);
                    level = tjaLine.IndexOf(levelStr);

                    scoreinit = tjaLine.IndexOf(scoreinitStr);
                    scorediff = tjaLine.IndexOf(scorediffStr);
                    balloon = tjaLine.IndexOf(balloonStr);
                    scoreshinuchi = tjaLine.IndexOf(scoreshinuchiStr);

                    comma = tjaLine.IndexOf(commaStr);
                    sharp = tjaLine.IndexOf(sharpStr);

                    start = tjaLine.IndexOf(startStr);
                    end = tjaLine.IndexOf(endStr);

                    bmscroll = tjaLine.IndexOf(bmscrollStr);
                    hbscroll = tjaLine.IndexOf(hbscrollStr);

                    if (colon >= 0 & sharp != 0)
                    {
                        // ヘッダーチェック
                        string header = tjaLine.Substring(0, colon + colonStr.Length);

                        switch (header)
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
                            case artistStr:
                            case creatorStr:
                            case albumartStr:
                            case scoreshinuchiStr:
                                attentionMsg.Add("情報：[" + countLine + "行目] " + tjaLine + " 独自ヘッダーを検出しました。");
                                break;
                            default:
                                attentionMsg.Add("注意：[" + countLine + "行目] " + tjaLine + " はKoiotoでサポートされていません。");
                                break;
                        }
                    }

                    if (sharp == 0 & start != 0 & end != 0)
                    {
                        // 命令文チェック
                        string instruction = tjaLine;

                        int i = tjaLine.IndexOf(" ");

                        if (i >= 0)
                        {
                            // 命令文だけ抜き出す
                            instruction = tjaLine.Substring(0, i + sharpStr.Length);
                        }

                        switch (instruction)
                        {
                            case bpmchangeStr:
                            case gogostartStr:
                            case gogoendStr:
                            case measureStr:
                            case scrollStr:
                            case delayStr:
                                break;
                            default:
                                attentionMsg.Add("注意：[" + countLine + "行目] " + tjaLine + " はKoiotoでサポートされていません。");
                                break;
                        }
                    }

                    if (title == 0)
                    {
                        tci.title = tjaLine.Substring(titleStr.Length);
                    }

                    if (subtitle == 0)
                    {
                        string subtitleData;
                        subtitleData = tjaLine.Substring(subtitleStr.Length);

                        if (subtitleData.Length >= 2)
                        {
                            switch (subtitleData.Substring(0, 2))
                            {
                                // 先頭2文字による処理分け
                                case "--":
                                    artistList.Add(subtitleData.Substring(2));
                                    tci.artist = artistList.ToArray();
                                    break;
                                case "++":
                                    tci.subtitle = subtitleData.Substring(2);
                                    break;
                                default:
                                    tci.subtitle = subtitleData;
                                    break;
                            }
                        }
                        else
                        {
                            tci.subtitle = subtitleData;
                        }
                    }

                    if (artist == 0)
                    {
                        artistList.Add(tjaLine.Substring(artistStr.Length));
                        tci.artist = artistList.ToArray();
                    }

                    if (creator == 0)
                    {
                        creatorList.Add(tjaLine.Substring(creatorStr.Length));
                        tci.creator = creatorList.ToArray();
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
                            if (tci.background != null)
                            {
                                // 既にtci.backgroundにデータが存在する
                                switch (setting.bgPriority)
                                {
                                    case SettingWindow.bgMOVIE:
                                        attentionMsg.Add("変更：" + bgmovieStr + tci.background + " が優先されます。");
                                        break;
                                    default:
                                        tci.background = str;
                                        break;
                                }
                            }
                            else
                            {
                                tci.background = str;
                            }
                        }
                    }

                    if (bgmovie == 0)
                    {
                        string str = tjaLine.Substring(bgmovieStr.Length);

                        if (str != "")
                        {
                            if (tci.background != null)
                            {
                                // 既にtci.backgroundにデータが存在する
                                switch (setting.bgPriority)
                                {
                                    case SettingWindow.bgIMAGE:
                                        attentionMsg.Add("変更：" + bgimageStr + tci.background + " が優先されます。");
                                        break;
                                    default:
                                        tci.background = str;
                                        break;
                                }
                            }
                            else
                            {
                                tci.background = str;
                            }
                        }
                    }

                    if (movieoffset == 0)
                    {
                        // .tciではオフセットが逆
                        tci.movieoffset = DoubleSubstring(tjaLine, movieoffsetStr.Length) * -1;
                    }

                    if (bpm == 0)
                    {
                        tci.bpm = DoubleSubstring(tjaLine, bpmStr.Length);
                    }

                    if (offset == 0)
                    {
                        // .tciではオフセットが逆
                        tci.offset = DoubleSubstring(tjaLine, offsetStr.Length) * -1;

                        if (setting.bOffset)
                        {
                            double i = (double)tci.offset + setting.offset;
                            tci.offset = Math.Round(i, 15);

                            if (setting.offset > 0)
                            {
                                attentionMsg.Add("変更：offsetに +" + setting.offset + " の補正がかかります。");
                            }
                            else if (setting.offset < 0)
                            {
                                attentionMsg.Add("変更：offsetに " + setting.offset + " の補正がかかります。");
                            }
                        }
                    }

                    if (demostart == 0)
                    {
                        tci.songpreview = DoubleSubstring(tjaLine, demostartStr.Length);
                    }

                    if (albumart == 0)
                    {
                        tci.albumart = tjaLine.Substring(albumartStr.Length);
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
                                nowDifficulty = str;
                                break;
                            case "0":
                                nowDifficulty = "Easy";
                                break;
                            case "1":
                                nowDifficulty = "Normal";
                                break;
                            case "2":
                                nowDifficulty = "Hard";
                                break;
                            case "4":
                                nowDifficulty = "Edit";
                                break;
                            case "3":
                            default:
                                nowDifficulty = "Oni";
                                break;
                        }
                    }

                    if (level == 0)
                    {
                        nowLevel = IntSubstring(tjaLine, levelStr.Length);
                    }

                    if (scoreinit == 0)
                    {
                        tcc.scoreinit = ScoreSubstring(tjaLine, scoreinitStr.Length);
                        nowDoubleplayScoreinit = tcc.scoreinit;
                    }

                    if (scorediff == 0)
                    {
                        tcc.scorediff = ScoreSubstring(tjaLine, scorediffStr.Length);
                        nowDoubleplayScorediff = tcc.scorediff;
                    }

                    if (scoreshinuchi == 0)
                    {
                        tcc.scoreshinuchi = ScoreSubstring(tjaLine, scoreshinuchiStr.Length);
                        nowDoubleplayScoreshinuchi = tcc.scoreshinuchi;
                    }

                    if (balloon == 0)
                    {
                        string str = tjaLine.Substring(balloonStr.Length);

                        str = str.Replace(" ", "");
                        str = str.TrimEnd(',');

                        if (str != "")
                        {
                            balloonList = str.Split(',').Select(a => int.Parse(a)).ToList();

                            tcc.balloon = balloonList.ToArray();
                        }
                    }

                    if (end == 0)
                    {
                        isMeasure = false;

                        // #ENDと同時に.tccの書き込み処理に入る

                        tcic.difficulty = nowDifficulty.ToLower();
                        tcic.level = nowLevel;

                        tcc.measures = measures.Select(a => a.ToArray()).ToArray();

                        if (attentionMsg.Count > 0)
                        {
                            // .tccに関する警告メッセージの出力
                            TextBoxMainWrite();
                            foreach (var msg in attentionMsg)
                            {
                                TextBoxMainWrite(msg);
                            }
                        }

                        bool dupDificculty = false; // 被っている難易度が存在するか？
                        int dupCourse = 0;          // 被っている難易度の位置

                        if (courseList.Count >= 1)
                        {
                            foreach (var item in tci.courses)
                            {
                                if (item.difficulty == nowDifficulty.ToLower())
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
                            if (nowPlayside > 0)
                            {
                                // DP
                                tcc.scoreinit = nowDoubleplayScoreinit;
                                tcc.scorediff = nowDoubleplayScorediff;

                                string relativePath = OTCWrite<OpenTaikoChartCourse>(tcc, Path.GetDirectoryName(filePath), nowDifficulty + "_" + nowPlayside + "P", ".tcc");
                                multipleList.Add(relativePath);
                                tci.courses[dupCourse].multiple = multipleList.ToArray();
                            }
                            else
                            {
                                // SP
                                string relativePath = OTCWrite<OpenTaikoChartCourse>(tcc, Path.GetDirectoryName(filePath), nowDifficulty, ".tcc");
                                tci.courses[dupCourse].single = relativePath;
                            }
                        }
                        else
                        {
                            // 新しいCourseを作成
                            if (nowPlayside > 0)
                            {
                                // DP
                                string relativePath = OTCWrite<OpenTaikoChartCourse>(tcc, Path.GetDirectoryName(filePath), nowDifficulty + "_" + nowPlayside + "P", ".tcc");
                                multipleList.Add(relativePath);
                            }
                            else
                            {
                                // SP
                                string relativePath = OTCWrite<OpenTaikoChartCourse>(tcc, Path.GetDirectoryName(filePath), nowDifficulty, ".tcc");
                                tcic.single = relativePath;
                            }

                            // Courseの追加
                            courseList.Add(tcic);
                            tci.courses = courseList.ToArray();
                        }

                        // 一部の変数をリセット
                        attentionMsg = new List<string>() { };
                        tcic = new OpenTaikoChartInfomation_Courses();
                        tcc = new OpenTaikoChartCourse();
                        measures.Clear();
                    }

                    if (bmscroll == 0 | hbscroll == 0)
                    {
                        // 唯一の#START～#END外の命令文なので特別扱い
                        // 命令文の挿入
                        string str = InstructionConvert(tjaLine);
                        measureLine.Add(str);
                    }

                    if (isMeasure)
                    {
                        if (tjaLine.Length > 0)
                        {
                            if (sharp == 0)
                            {
                                // 命令文の挿入
                                string str = InstructionConvert(tjaLine);
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
                        isMeasure = true;

                        if (tjaLine.Length == startStr.Length + 3)
                        {
                            // #START P1 への対応
                            string str = tjaLine.Substring(startStr.Length + 1);

                            if (Regex.IsMatch(str, "^P[0-9]*"))
                            {
                                nowPlayside = int.Parse(str.Substring(1));
                            }
                            else
                            {
                                nowPlayside = 0;
                            }
                        }
                        else
                        {
                            nowPlayside = 0;
                        }
                    }
                }

                if (setting.bCreator)
                {
                    // 設定からCreatorをインポート
                    creatorList.Add(setting.creator);
                    tci.creator = creatorList.ToArray();
                    TextBoxMainWrite();
                    TextBoxMainWrite("変更：creatorに \"" + setting.creator + "\" が格納されます。");
                }

                if (artistList.Count >= 2)
                {
                    // artistの重複削除（大文字小文字を区別しない）
                    tci.artist = artistList.Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
                }

                if (creatorList.Count >= 2)
                {
                    // creatorの重複削除（大文字小文字を区別しない）
                    tci.creator = creatorList.Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
                }

                OTCWrite<OpenTaikoChartInfomation>(tci, Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath), ".tci");

                tja.Close();
            }
            else
            {
                TextBoxMainWrite();
                TextBoxMainWrite("エラー：読み込めるのは.tjaファイルのみです。");
            }
        }

        private string OTCWrite<Type>(Type otc, string directory, string filename, string extension)
        {
            // .tti .ttcファイルの書き込み
            directory += @"\";
            string writePath = directory + filename + extension;

            SafeCreateDirectory(Path.GetDirectoryName(writePath));

            using (var fs = new FileStream(writePath, FileMode.Create, FileAccess.Write))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.UTF8, true, true))    // インデントと改行の挿入
            {
                var otcType = otc.GetType();
                var serializer = new DataContractJsonSerializer(otcType);
                serializer.WriteObject(writer, otc);

                TextBoxMainWrite();
                TextBoxMainWrite(writePath + " が作成されました。");
            }

            // directoryから見た相対パスを返す
            return ToRelativePath(directory, writePath);
        }

        private string InstructionConvert(string tjaLine)
        {
            // 命令文の変換
            tjaLine = tjaLine.Replace(bpmchangeStr, bpmchangeTcc);
            tjaLine = tjaLine.Replace(gogostartStr, gogostartTcc);
            tjaLine = tjaLine.Replace(measureStr, measureTcc);

            tjaLine = tjaLine.ToLower();

            return tjaLine;
        }

        private void TextBoxMainWrite()
        {
            TextBoxMainWrite("");
        }

        private void TextBoxMainWrite(string text)
        {
            TextBoxMain.AppendText("\r" + text);
        }

        private int IntSubstring(string tjaLine, int i)
        {
            return (int)Math.Truncate(DoubleSubstring(tjaLine, i));
        }

        private double DoubleSubstring(string tjaLine, int i)
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

        private int? ScoreSubstring(string tjaLine, int i)
        {
            // SCOREINIT: SCOREDIFF:用のInsSubstring
            // 空白だった場合にnullを返す

            tjaLine = tjaLine.Substring(i);

            if (tjaLine.Length != 0)
            {
                return int.Parse(tjaLine);
            }
            else
            {
                return null;
            }
        }

        private DirectoryInfo SafeCreateDirectory(string path)
        {
            // 指定したパスにディレクトリが存在しない場合、ディレクトリを作成
            if (Directory.Exists(path))
            {
                return null;
            }

            return Directory.CreateDirectory(path);
        }

        private string ToRelativePath(string basePath, string targetPath)
        {
            // 相対パスを取得

            // "%"を"%25"に変換しておく（デコード対策）
            basePath = basePath.Replace("%", "%25");
            targetPath = targetPath.Replace("%", "%25");

            Uri u1 = new Uri(basePath);
            Uri u2 = new Uri(targetPath);
            Uri relativeUri = u1.MakeRelativeUri(u2);
            string relativePath = relativeUri.ToString();

            relativePath = Uri.UnescapeDataString(relativePath);

            // "%25"を"%"に戻す
            relativePath = relativePath.Replace("%25", "%");

            return relativePath;
        }

        #region OpenTaikoChartClass

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
            public int? scoreinit { get; set; }  // ?を外してみる
            [DataMember(Order = 2)]
            public int? scorediff { get; set; }  // ?を外してみる
            [DataMember(Order = 3)]
            public int? scoreshinuchi { get; set; }
            [DataMember(Order = 4)]
            public int[] balloon { get; set; }  // ?を外してみる
            [DataMember(Order = 5)]
            public string[][] measures { get; set; }
        }

        #endregion

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
        int artist, creator, albumart;  // 独自ヘッダー
        const string artistStr = "ARTIST:";
        const string creatorStr = "CREATOR:";
        const string albumartStr = "ALBUMART:";

        // .tci対応（コース別）
        int course, level;
        //int style;    // DPかどうかは#STARTの後のP1,P2で判別可能なので省略
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
        int scoreshinuchi;  //独自ヘッダー
        const string scoreshinuchiStr = "SCORESHINUCHI:";

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

        // 生成されたファイルの編集しやすさを考えるとstringにはnullが入るより""入れたほうがいいのかもしれない（特にstring[]）
        // /が　＼/みたいな書き方で出力されてしまう（JSONではそれが正しいらしい）
        // ＼は＼＼と出力するのがOTCの規定らしい
        // 出力ディレクトリを選べる機能
        // 出力後のファイルを整形しなおす機能（改行など）
        // SUBTITLE:の振り分け先（subtitle,artist）を++,--のケースを含めて設定できる機能
        // Easy, Normal, Hard, Oni, Editの各.tccファイルを事前に設定した名前で出力できる機能（"0_Easy.tcc","1_Normal.tcc"など）
        // 上の機能、できれば.tjaファイル名の名前も指定できるようにしたいところ。{filename}とか
        // #N,#E,#Mが他の命令文でも検知してしまう問題への対応。文字列完全一致だとtrueでいい気はする
        // 真打の自動計算
    }
}
