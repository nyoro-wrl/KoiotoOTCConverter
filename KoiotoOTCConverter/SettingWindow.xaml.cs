﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
                Application.Current.Shutdown();
            }
        }

        private void SetSettingText()
        {
            TextBoxOFFSET.Text = setting.OFFSET.ToString();
        }

        private void GetSettingText()
        {
            var defaultValue = new Setting();

            if (Double.TryParse(TextBoxOFFSET.Text, out double result))
            {
                setting.OFFSET = result;
            }
            else
            {
                setting.OFFSET = defaultValue.OFFSET;
            }
        }

        private void TextBoxOFFSET_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 数値のみ入力を許可する
            Regex regex = new Regex("[^0-9.-]+");
            if (TextBoxOFFSET.Text.Length > 0 && e.Text == "-")
            {
                e.Handled = true;
                return;
            }
            var text = TextBoxOFFSET.Text + e.Text;
            e.Handled = regex.IsMatch(text);
        }

        private void TextBoxOFFSET_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double variation = 0.001;

            if (e.Delta > 0)
            {
                // ホイール上回転
                double i = Double.Parse(TextBoxOFFSET.Text);
                i += variation;
                TextBoxOFFSET.Text = i.ToString();
            }
            else
            {
                // ホイール下回転
                double i = Double.Parse(TextBoxOFFSET.Text);
                i -= variation;
                TextBoxOFFSET.Text = i.ToString();
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

        public class Setting
        {
            public double OFFSET { get; set; } = 0;
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
    }
}
