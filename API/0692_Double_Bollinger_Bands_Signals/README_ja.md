# ダブル Bollinger Bands シグナル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は2つのBollinger Bandsセットを使用します。価格が3標準偏差の下限バンドを上抜けた時に買い、3標準偏差の上限バンドを下抜けた時に売ります。ポジションは反対側の2標準偏差バンドで決済されます。

## 詳細

- **エントリー条件**:
  - ロング: 終値が3 SD下限バンドを上抜け
  - ショート: 終値が3 SD上限バンドを下抜け
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: 終値が2 SD上限バンドを上抜け
  - ショート: 終値が2 SD下限バンドを下抜け
- **ストップ**: なし
- **デフォルト値**:
  - `Length` = 20
  - `Width1` = 2m
  - `Width2` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Bollinger Bands
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
