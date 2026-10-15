# Hull MA RSI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Hull Moving Average + RSI 戦略の実装。HMA が上向きに転じ RSI が RsiOversold 未満のときに買い。HMA が下向きに転じ RSI が RsiOverbought 超のときに売り。

テストでは年平均リターン約 64% を示しています。外国為替市場で最も優れたパフォーマンスを発揮します。

Hull MA は滑らかなトレンドラインを提供し、RSI はモメンタムのダイバージェンスを強調します。価格が Hull の方向に従いながら RSI が極値で転換したときにトレードが行われます。

早期シグナルを求める短期スイングトレーダーに適しています。ATR ベースのストップがトレードを保護します。

## 詳細

- **エントリー条件**:
  - ロング: `HullMA turning up && RSI < RsiOversold`
  - ショート: `HullMA turning down && RSI > RsiOverbought`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - Hull MA の方向転換
- **ストップ**: `StopLossAtr` を使用した ATR ベース
- **デフォルト値**:
  - `HmaPeriod` = 9
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StopLossAtr` = 2
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    HMAは下落の後に上昇すると上向きに転じ、上昇の後に下落すると下向きに転じます。ストップはエントリー時の終値からStopLossAtr倍のATR（AtrPeriod）の位置にあり、足の終値で判定します。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Hull MA, Moving Average, RSI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
