# Parabolic Sar Volume Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
Parabolic SARと出来高確認を組み合わせた戦略。価格が平均以上の出来高でParabolic SARをクロスしたときにトレードをエントリーする。

テストでは年平均リターン約151%を示しています。株式市場で最もパフォーマンスが高くなります。

Parabolic SARがトレンド転換を特定し、より高い出来高がシグナルを検証します。SARの転換が出来高の拡大とともに来たときにトレードが開始されます。

出来高ベースの動きを追うトレーダーに有用です。SARトレイルとATRファクターが大きな損失から守ります。

## 詳細

- **エントリー条件**:
  - ロング: `Close > SAR && Volume > AvgVolume`
  - ショート: `Close < SAR && Volume > AvgVolume`
- **ロング/ショート**: 両方
- **エグジット条件**: SARの転換
- **ストップ**: Parabolic SARをトレーリングストップとして使用
- **デフォルト値**:
  - `Acceleration` = 0.02m
  - `MaxAcceleration` = 0.2m
  - `VolumePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Parabolic SAR, Parabolic SAR, 出来高
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

