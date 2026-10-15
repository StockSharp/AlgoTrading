# Parabolic SAR 早期買い・MA 決済戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はParabolic SARの反転を利用して取引し、SARが価格の上に反転し終値がN期間の移動平均を下回ったとき、ロングポジションを早期決済します。

## 詳細

- **エントリー条件**:
  - 価格とParabolic SARのクロス。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - ロングポジションの場合: SARが価格の上にあり、終値がMA (`MaPeriod`) を下回る。
  - ショートポジションの場合: 逆方向のSARクロス（エントリーロジックで処理）。
- **ストップ**: なし。
- **デフォルト値**:
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `MaPeriod` = 11
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング＆ショート
  - インジケーター: Parabolic SAR, SMA
  - ストップ: なし
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: なし
  - ニューラルネットワーク: なし
  - ダイバージェンス: なし
  - リスクレベル: 低
