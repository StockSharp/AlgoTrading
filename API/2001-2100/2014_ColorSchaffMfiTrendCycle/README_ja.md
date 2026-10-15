# Color Schaff MFIトレンドサイクル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMQL5エキスパート `Exp_ColorSchaffMFITrendCycle` の翻訳版です。
**Color Schaff MFI Trend Cycle** インジケーターを使用しており、
マネーフローインデックス値とダブルストキャスティクス計算を組み合わせます。インジケーターは
モメンタムと過買い/過売りゾーンを表す8つのカラー状態を生成します。

取引ロジック:

- 前のインジケーターの色が**緑**（インデックス6-7）で、現在の色が強い上昇トレンドゾーンを下回ると、戦略はショートポジションを閉じて
  新しいロングポジションを開きます。
- 前のインジケーターの色が**オレンジ**（インデックス0-1）で、現在の色が強い下降トレンドゾーンを上回ると、戦略はロングポジションを閉じて
  新しいショートポジションを開きます。

パラメーター:

- `FastMfiPeriod` – 高速MFIの期間。
- `SlowMfiPeriod` – 低速MFIの期間。
- `CycleLength` – インジケーターで使用するサイクルバッファーの長さ。
- `HighLevel` / `LowLevel` – STC値の過買い・過売りしきい値。
- `CandleType` – 入力ローソク足の時間軸（デフォルト1時間）。

この戦略はStockSharpのハイレベルAPIを使用し、完成したローソク足のみを処理します。
