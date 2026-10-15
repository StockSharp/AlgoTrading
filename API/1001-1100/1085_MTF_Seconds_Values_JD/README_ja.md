# MTF Seconds Values JD 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は指定した秒数間隔に基づくカスタムのマルチタイムフレームローソク足の処理を示します。集計されたローソク足に対して単純移動平均を計算し、価格が平均をクロスしたときに取引します。

## パラメーター

- `SecondsTimeframe` – ローソク足集計の秒数間隔。
- `AverageLength` – 単純移動平均の期間。
