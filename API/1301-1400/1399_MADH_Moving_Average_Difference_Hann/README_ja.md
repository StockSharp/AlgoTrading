# MADH 移動平均差分・Hann戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

John EhlersによるMADHインジケーターを実装します。インジケーターがゼロを上回るとロング、下回るとショートとなります。

## 詳細
- **エントリー条件**: MADH > 0 でロング、MADH < 0 でショート。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルで反転。
- **ストップ**: なし。
- **デフォルト値**:
  - `ShortLength` = 8
  - `DominantCycle` = 27
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: MADH
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
