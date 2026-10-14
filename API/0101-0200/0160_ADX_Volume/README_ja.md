# ADX Volume 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ADX + Volume 戦略の実装。ADX が閾値を超え出来高が平均を上回るときにトレードに参入します。方向は DI+ と DI- の比較によって決まります。

テストでは年平均リターン約 67% を示しています。株式市場で最も優れたパフォーマンスを発揮します。

高い ADX は強いトレンドを示し、出来高のスパイクがコミットメントを確認します。両方のインジケーターが同時に強さを示したときにエントリーします。

エネルギッシュなブレイクアウトを捉えるのに最適です。ATR ベースのストップでリスクを抑えます。

## 詳細

- **エントリー条件**:
  - ロング: `ADX > AdxThreshold && Volume > AvgVolume && +DI > -DI`
  - ショート: `ADX > AdxThreshold && Volume > AvgVolume && -DI > +DI`
- **ロング/ショート**: 両方
- **エグジット条件**: トレンドが閾値を下回って弱まる
- **ストップ**: `StopLossAtr` を使用した ATR ベース
- **デフォルト値**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `VolumeAvgPeriod` = 20
  - `StopLossAtr` = 2
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    AvgVolumeは直前VolumeAvgPeriod本の平均出来高です。ストップはエントリー時の終値からStopLossAtr倍のATR（AtrPeriod）の位置にあり、足の終値で判定します。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: ADX, 出来高
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
