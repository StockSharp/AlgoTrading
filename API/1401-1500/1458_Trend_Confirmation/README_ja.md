# トレンド確認戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SuperTrend、MACD、VWAP を組み合わせてトレンドを確認する戦略。

## 詳細
- **エントリー条件**: MACD 確認と VWAP に対する価格位置を伴った SuperTrend の方向。
- **ロング/ショート**: 両方。
- **エグジット条件**: MACD がポジションに逆らってシグナルラインをクロス。
- **ストップ**: なし。
- **デフォルト値**: ATR 長さ 10、ファクター 3、MACD 短期 12、長期 26、シグナル 9。
- **フィルター**: SuperTrend と VWAP。
