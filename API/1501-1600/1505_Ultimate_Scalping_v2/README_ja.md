# Ultimate スキャルピング戦略 v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

速い・遅いEMAとVWAPを組み合わせたスキャルピングシステム。オプションの包み足フィルターと出来高急増フィルターがエントリーを絞り込む。ポジションはATRベースのストップを使用し、反対シグナルで決済可能。

## 詳細

- **ロング**: 速いEMAが遅いEMAを上抜け、かつ価格がVWAPより上。
- **ショート**: 速いEMAが遅いEMAを下抜け、かつ価格がVWAPより下。
- **インジケーター**: EMA, VWAP, ATR, SMA。
- **ストップ**: ATRの倍数。
