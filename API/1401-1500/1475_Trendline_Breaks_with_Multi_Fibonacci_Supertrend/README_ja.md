# マルチ Fibonacci Supertrend によるトレンドライン・ブレイク戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は Fibonacci 乗数 (0.618、1.618、2.618) を使った 3 つの SuperTrend 計算を平均し、その結果を EMA でスムージングします。ATR から導出した傾きを使い、スウィング高値と安値から動的なトレンドラインを構築します。価格が上部トレンドラインを上抜け、スムージングされた SuperTrend が上昇し、+DI 値が −DI を超えるとロングトレードが開かれます。ショートトレードはこれらのルールを逆にミラーリングします。

## 詳細
- **エントリー**: DMI 確認と SuperTrend の一致によるトレンドラインのブレイクアウト。
- **エグジット**: スムージングされたトレンドを再び越えるか、ATR‑ベースのストップ/ターゲットに到達。
- **インジケーター**: SuperTrend、ATR、Average Directional Index。
- **タイプ**: ブレイクアウト、ロングとショート。
