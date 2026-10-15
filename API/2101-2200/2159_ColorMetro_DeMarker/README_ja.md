# ColorMetro DeMarker戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**ColorMetro DeMarker戦略**はMQL5エキスパートアドバイザー`Exp_ColorMETRO_DeMarker`のStockSharp実装です。
DeMarkerインジケーターをステップレベルと組み合わせてトレードシグナルを生成します。

## パラメーター
- **DeMarker Period** – DeMarkerインジケーターの期間。
- **Fast Step** – 高速レベル（MPlus）構築に使用するステップサイズ。
- **Slow Step** – 低速レベル（MMinus）構築に使用するステップサイズ。
- **Candle Type** – 分析に使用するローソク足の時間軸。
- **Enable Buy Open** – ロングポジションのオープンを許可する。
- **Enable Sell Open** – ショートポジションのオープンを許可する。
- **Enable Buy Close** – ロングポジションのクローズを許可する。
- **Enable Sell Close** – ショートポジションのクローズを許可する。

## トレードロジック
1. DeMarker値は0〜100にスケールされ、高速・低速ステップサイズを使用して2つの動的レベル（MPlusとMMinus）が計算されます。
2. 前の高速レベルが低速レベルを上回っており、現在の高速レベルが低速レベルを下抜けた場合、戦略は買いを入れ、オプションでショートポジションを閉じます。
3. 前の高速レベルが低速レベルを下回っており、現在の高速レベルが低速レベルを上抜けた場合、戦略は売りを入れ、オプションでロングポジションを閉じます。
4. すべての計算は完成したローソク足のみを使用します。

このアプローチにより、階段状のDeMarkerレベルが示すトレンド転換を追跡できます。
