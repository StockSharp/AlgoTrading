# Color Schaff RVIトレンドサイクル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はStockSharpのハイレベルAPIを使用してColor Schaff RVI Trend Cycleを実装します。インジケーターは高速・低速のRelative Vigor Index値の差にダブルストキャスティクスプロセスを適用し、結果を平滑化します。

## パラメーター
- `FastRviLength` – 高速RVI計算の期間（デフォルト23）。
- `SlowRviLength` – 低速RVI計算の期間（デフォルト50）。
- `CycleLength` – ストキャスティクスサイクルの長さ（デフォルト10）。
- `HighLevel` – 強気条件の検出に使用する上限しきい値（デフォルト60）。
- `LowLevel` – 弱気条件の検出に使用する下限しきい値（デフォルト-60）。
- `CandleType` – 戦略が処理するローソク足タイプ（デフォルト4時間足）。

## 取引ロジック
1. 高速・低速のRVI値を計算する。
2. RVI差からSchaff Trend Cycleを構築する。
3. STC値が上限レベルを上回り上昇しているときに**買い**。
4. STC値が下限レベルを下回り下降しているときに**売り**。

## 注意事項
- 戦略は完成したローソク足のみを処理します。
- 開始時にポジション保護が有効になります。
- このサンプルは教育目的のみで提供されており、投資アドバイスではありません。
