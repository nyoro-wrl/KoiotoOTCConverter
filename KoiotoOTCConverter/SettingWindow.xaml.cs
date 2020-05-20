using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KoiotoOTCConverter
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();

            setting = LoadSetting(setting);

            SetSettingText();
        }

        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Escape))
            {
                // Escキーで終了
                this.Close();
            }
        }

        private void SetSettingText()
        {
            TextBoxOffset.Text = setting.offset.ToString();
            CheckBoxOffset.IsChecked = setting.offsetEnable;

            TextBoxCreator.Text = setting.creator;
            CheckBoxCreator.IsChecked = setting.creatorEnable;

            switch (setting.bgPriority)
            {
                case bgIMAGE:
                    RadioButtonBackground2.IsChecked = true;
                    break;
                case bgMOVIE:
                    RadioButtonBackground3.IsChecked = true;
                    break;
                default:
                    RadioButtonBackground1.IsChecked = true;
                    break;
            }
        }

        private void GetSettingText()
        {
            setting.offset = Double.Parse(TextBoxOffset.Text);
            setting.offsetEnable = (bool)CheckBoxOffset.IsChecked;

            setting.creator = TextBoxCreator.Text;
            setting.creatorEnable = (bool)CheckBoxCreator.IsChecked;

            switch(GetRadioButtonContent(BackgroundPanel))
            {
                case bgIMAGE:
                    setting.bgPriority = bgIMAGE;
                    break;
                case bgMOVIE:
                    setting.bgPriority = bgMOVIE;
                    break;
                default:
                    setting.bgPriority = bgDefault;
                    break;
            }
        }

        private void TextBoxOffset_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 数値のみ入力を許可する
            Regex regex = new Regex("[^0-9.-]+");
            if (TextBoxOffset.Text.Length > 0 && e.Text == "-")
            {
                e.Handled = true;
                return;
            }
            var text = TextBoxOffset.Text + e.Text;
            e.Handled = regex.IsMatch(text);
        }

        private void TextBoxOffset_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TextBoxOffset.Text == "" | TextBoxOffset.Text == "-0")
            {
                // 空白か-0を0に修正する
                TextBoxOffset.Text = "0";
            }
        }

        private void TextBoxOffset_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double variation = 0.001;

            if (e.Delta > 0)
            {
                // ホイール上回転
                double i = Double.Parse(TextBoxOffset.Text);
                i += variation;
                TextBoxOffset.Text = i.ToString();
            }
            else
            {
                // ホイール下回転
                double i = Double.Parse(TextBoxOffset.Text);
                i -= variation;
                TextBoxOffset.Text = i.ToString();
            }
        }

        public static void SaveSetting(Setting setting)
        {
            using (var fs = new FileStream(settingPath, FileMode.Create, FileAccess.Write))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.UTF8, true, true))    // インデントと改行の挿入
            {
                var serializer = new DataContractJsonSerializer(typeof(Setting));
                serializer.WriteObject(writer, setting);
            }
        }

        public static Setting LoadSetting(Setting setting)
        {
            using (var fs = new FileStream(settingPath, FileMode.OpenOrCreate))
            {
                var serializer = new DataContractJsonSerializer(typeof(Setting));

                try
                {
                    setting = serializer.ReadObject(fs) as Setting;
                }
                catch (System.Runtime.Serialization.SerializationException)
                {
                    // ファイルが存在しなかったりフォーマットが正しくない場合デフォルト設定を保存する
                    fs.Close();
                    setting = new Setting();
                    SaveSetting(setting);
                }

                return setting;
            }
        }

        const string settingPath = @"KoiotoOTCConverter.setting.json";
        Setting setting;

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveClose();
        }

        private void SaveClose()
        {
            GetSettingText();
            SaveSetting(setting);
            MainWindow mw = Owner as MainWindow;
            mw.TextBoxMain.AppendText("\r" + "\r" + "設定を保存しました。");
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            // ダイアログが埋もれないようにアクティブ化
            FormMain.Activate();

            string msgText = "設定を既定値に戻しますか？" + "\r" + "（適用には保存が必要です）";

            var result = MessageBox.Show(this, msgText, "警告", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            // ダイアログでの選択を判別
            if (result == MessageBoxResult.Yes)
            {
                setting = new Setting();
                SetSettingText();
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                // Enterキーで保存して閉じる
                SaveClose();
            }
        }

        private void CheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            // チェックボックスの変更
            if ((bool)CheckBoxOffset.IsChecked)
            {
                TextBoxOffset.IsEnabled = true;
            }
            else
            {
                TextBoxOffset.IsEnabled = false;
            }
            if ((bool)CheckBoxCreator.IsChecked)
            {
                TextBoxCreator.IsEnabled = true;
            }
            else
            {
                TextBoxCreator.IsEnabled = false;
            }
        }

        private void RadioButtonBackground_Checked(object sender, RoutedEventArgs e)
        {
            // backgroundラジオボタンの変更

            string str = "";

            switch (GetRadioButtonContent(BackgroundPanel))
            {
                case bgDefault:
                    str = "後に定義されたものが優先されます。";
                    break;
                case bgIMAGE:
                    str = "常にBGIMAGE:が優先されます。";
                    break;
                case bgMOVIE:
                    str = "常にBGMOVIE:が優先されます。";
                    break;
            }

            TextBlockBackgound.Text = str;
        }

        private string GetRadioButtonContent(StackPanel sp)
        {
            foreach (RadioButton rb in sp.Children)
            {
                if ((bool)rb.IsChecked)
                {
                    return rb.Content.ToString();
                }
            }

            return "";
        }

        public class Setting
        {
            public double offset { get; set; } = 0;
            public bool offsetEnable { get; set; } = false;
            public string creator { get; set; } = "";
            public bool creatorEnable { get; set; } = false;
            public string bgPriority { get; set; } = bgDefault;
        }

        public const string bgDefault = "Default";
        public const string bgIMAGE = "BGIMAGE";
        public const string bgMOVIE = "BGMOVIE";

        // 設定ウィンドウが出る位置がめんどくさい
    }
}
