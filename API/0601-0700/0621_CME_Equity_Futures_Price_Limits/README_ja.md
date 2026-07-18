# CME 株式先物価格制限戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、CME 株式先物の日次価格制限レベルを計算します。指定した時刻に参照価格を取得し、上限/下限 (+/-5%) および -7%、-13%、-20% の下限レベルを計算します。結果は監視のためログに出力されます。

## パラメーター

- **ManualReference** – 手動参照価格の上書き（0 で無効）。
- **ShowLimitDownLevels** – -7/-13/-20% レベルのログ記録を有効にする。
- **OffsetHour** – 参照価格を取得する時刻 (0-23)。
- **CandleType** – 処理するローソク足の種類（デフォルト: 1 分）。
