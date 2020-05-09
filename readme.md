# KoiotoOTCConverter
.tjaを.tciと.tccに変換するツールです。

現在[Open Taiko Chart Rev2](https://github.com/AioiLight/Open-Taiko-Chart/blob/master/Rev2_ja-JP.md)に大体対応しています。  

~~GitHubの使い方を未だよくわかっていないのでちゃんとできているか不安。~~

## ダウンロード
[Releases](https://github.com/nyoro-wrl/KoiotoOTCConverter/releases)から一番新しいVerの.zipを解凍してください。

## 使い方
1. KoiotoOTCConverter.exeを開く
2. 出てきたウィンドウに.tjaファイルをD&D
3. 変換完了！

複数ファイルにも対応しています。

ウィンドウを開かずとも、そのまま.tjaファイルをD&Dで変換できます（その場合は処理が終わると自動的に閉じます）。

## 機能
- .tjaをD&Dで変換。
- 全難易度に対応。
- Koiotoで未対応のヘッダーや命令文は変換時に通知されます。命令文が未対応でも.tccには記載されます。
- 2人用の譜面にも対応。

## 追加予定の機能
- 出力先の指定
- 出力ファイルの整形
- 出力ファイルの名前指定
- ファイルを参照して変換
- 参照したフォルダ内の.tjaを全て変換
- 格納するcreatorを事前指定
- offsetの補正値
- SUBTITLE:の振り分け先指定
- .tcmへの対応

追加されるかどうかは気分次第です。

## 不具合
- /が\\/と出力される（動作には問題なし）。
