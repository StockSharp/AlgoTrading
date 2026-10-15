# ナイト・スキャルパー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はボリンジャーバンドを使って夕方のセッション周辺でトレードします。バンド幅が狭く、価格がバンドの外に突破した際、指定された開始時刻以降のみポジションを建てます。

## 詳細

- **エントリー条件**:
  - **ロング**: `Start Hour` 以降、価格がボリンジャーバンド下限を下回って終値がつき、バンド幅が `Range Threshold` 未満。
  - **ショート**: `Start Hour` 以降、価格がボリンジャーバンド上限を上回って終値がつき、バンド幅が `Range Threshold` 未満。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 翌日の `Start Hour` より前の時間になった場合、ポジションをクローズ。
  - `StartProtection` によって管理される保護的なストップロスとテイクプロフィット。
- **ストップ**: 固定のストップロスとテイクプロフィットのオフセットを持つ `StartProtection` を使用。
- **デフォルト値**:
  - `BB Period` = 40
  - `BB Deviation` = 1
  - `Range Threshold` = 450
  - `Stop Loss` = 370
  - `Take Profit` = 20
  - `Start Hour` = 19
  - `Candle Type` = 1h
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Bollinger Bands
  - ストップ: はい
  - 複雑さ: 低
  - 時間軸: 短期
