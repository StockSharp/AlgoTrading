# トレンドシステムのための出来高確認戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はトレンド・スラスト・インジケーター（TTI）、出来高価格確認インジケーター（VPCI）、ADXを使用してロングトレンドを確認します。

## 詳細
- **エントリー条件**:
  - **ロング**: ADX > 30、TTI > シグナル、VPCI > 0。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - VPCI < 0。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `ADX Length` = 14
  - `ADX Smoothing` = 14
  - `TTI Fast Average` = 13
  - `TTI Slow Average` = 26
  - `TTI Signal Length` = 9
  - `VPCI Short Avg` = 5
  - `VPCI Long Avg` = 25
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: ADX, TTI, VPCI
  - ストップ: いいえ
  - 複雑さ: 中
  - 時間軸: 中期
