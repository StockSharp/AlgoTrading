# Fibonacci トレンドリバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は直近の高値と安値を使用してFibonacciチャネルを構築します。価格がブレイクアウト方向に50%レベルをクロスしたときにポジションを開きます。リスク管理はATRベースのストップロスとリスク/リワードのテイクプロフィットで行い、部分決済もオプションで設定できます。

## パラメータ
- **Candle Type** — ローソク足シリーズ。
- **Sensitivity** — チャネル計算の基本感度。
- **ATR Period** — ストップロス用ATRの長さ。
- **ATR Multiplier** — ストップロス用ATR倍率。
- **Risk Reward** — リスクに対する利益の倍数。
- **Use Partial TP** — 最初のターゲットでポジションの半分を決済する。
- **Trade Direction** — 許可される取引方向。
