# マルチレイヤー Awesome Oscillator ソーサー
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Awesome Oscillator のソーサーパターンとフラクタルによるトレンド検出に基づく強気のマルチレイヤー戦略です。連続するソーサーシグナルをカウントし、価格の上に最大 5 つの段階的な買いストップ注文を発注します。トレンドが反転したときにポジションを決済します。

## パラメーター
- **EMA Length** – EMA フィルターの期間。
- **Candle Type** – ローソク足のタイプ。
- **Trade Start** – 取引期間の開始。
- **Trade Stop** – 取引期間の終了。
