# Rawstocks 15分モデル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Rawstocks 15 Minute Modelはスイングオーダーブロックとフィボナッチリトレースメントレベルを使用して、日次セッション内で取引を行います。

## 仕組み
- ATRフィルターでスイング高値と安値を検出します。
- 強気・弱気のオーダーブロックを構築し、61.8%と79%のフィボナッチレベルを計算します。
- 価格が強気オーダーブロックに触れ、エントリーカットオフ時間前にフィボナッチレベルを上回って終値がついた場合にロングエントリー。
- 価格が弱気オーダーブロックをテストし、フィボナッチレベルを下回って終値がついた場合にショートエントリー。
- 全ポジションを16:30 ETにクローズ。

## パラメーター
- Start Hour
- Start Minute
- Last Entry Hour
- Last Entry Minute
- Force Close Hour
- Force Close Minute
- Fib Level (%)
- Min Swing Size (%)
- Risk/Reward

### インジケーター
- Average True Range
