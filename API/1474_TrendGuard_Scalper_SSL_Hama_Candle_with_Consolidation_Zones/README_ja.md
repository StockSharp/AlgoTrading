# TrendGuard Scalper SSL + Hama Candle とコンソリデーションゾーン戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はシンプルな SSL チャネルと Hama ロウソク足の方向を組み合わせます。終値が SSL 平均を上回り、Hama 終値 (EMA 20) が Hama 長期ライン (EMA 100) を上回り、価格が Hama 終値より上に保たれているときにロングポジションを開きます。ショートトレードは逆の条件を使います。ATR は低ボラティリティ期間を潜在的なコンソリデーションゾーンとしてマークするために使用されます。

## 詳細
- **エントリー**: SSL と Hama のトレンドが一致し、価格が確認する。
- **エグジット**: 固定のテイクプロフィットとストップロスのパーセンテージ。
- **インジケーター**: SMA, EMA, ATR。
- **フィルター**: コンソリデーション検出は分析専用。
