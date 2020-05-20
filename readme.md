# KoiotoOTCConverter
.tjaを.tciと.tccに変換するツールです。

現在[Open Taiko Chart Rev2](https://github.com/AioiLight/Open-Taiko-Chart/blob/master/Rev2_ja-JP.md)に大体対応しています。  

~~GitHubの使い方を未だよくわかっていないのでちゃんとできているか不安。~~

## ダウンロード
[release](https://github.com/nyoro-wrl/KoiotoOTCConverter/releases)から一番新しいVerの.zipを解凍してください。

## 使い方
1. KoiotoOTCConverter.exeを開く
2. 出てきたウィンドウに.tjaファイルをD&D
3. 変換完了！

複数ファイルやフォルダーごとの変換にも対応しています。

ウィンドウを開かずとも、そのまま.tjaファイルをD&Dで変換できます（その場合は処理が終わると自動的に閉じます）。

## 機能
- .tjaをD&Dで変換（参照も可能）
- 全難易度に対応
- Koiotoで未対応のヘッダーや命令文は変換時に通知されます
- 2人用の譜面にも対応
- フォルダーをD&Dするとフォルダー内の.tjaを全て変換（変換前に確認を挟みます）
- その他細かい設定が可能
  - offsetの補正値
    - 変換後ファイルのoffsetを調整できます
  - creatorの格納
    - 事前に設定しておいたcreatorを格納できます
  - backgroundの優先順位
    - 常にBGMOVIE:を優先させたりできます
- 独自ヘッダーに対応
  - tjaファイルに以下のヘッダーを書いておくと、変換時にtci,tccファイルに格納されます

|独自ヘッダー|格納先|
|---|---|
|ARTIST:|.tciファイル "artist"|
|CREATOR:|.tciファイル "creator"|
|SCORESHINUCH:|.tccファイル "scoreshinuch"<br>COURSEごとに定義できます|

## 実装予定の機能
- 出力先の指定
- 出力ファイルの微整形
- 出力ファイルの名前指定
- SUBTITLE:の振り分け先指定
- .tcmへの対応
- SIDE:1,SIDE:2への対応

実装されるかどうかは気分次第です。

## 仕様
- 未対応の命令文でも.tccには記載されます
- SUBTITLE:とSUBTITLE:++はそのままsubtitleに格納されますが、SUBTITLE:--だった場合はartistに格納されます
- 設定はjsonファイルで保存されます
- 以下の変数に重複がある場合は自動的に削除されます（大文字小文字は区別しない）
	- artist
	- creator

## 不具合
- /が\\/と出力される（動作には影響なし）
