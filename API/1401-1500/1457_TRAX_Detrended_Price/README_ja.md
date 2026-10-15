# TRAX 価格デトレンド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

TRAX と DPO オシレーターを使用してトレンドリバーサルを取引する戦略。

## 詳細
- **エントリー条件**: TRAX の符号と SMA フィルターを伴って DPO が TRAX をクロス。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のクロスオーバーシグナル。
- **ストップ**: なし。
- **デフォルト値**: TRAX 長さ 12、DPO 長さ 19、SMA 確認長さ 3。
- **フィルター**: TRAX 符号と確認 SMA。
