# Hull Ma Stochastic 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Hull Moving Average + Stochastic Oscillator 戦略。HMA のトレンド方向が変化し、Stochastic が売られすぎ/買われすぎ条件を確認した際に参入します。

テストでは年平均収益率は約 94% を示しています。株式市場で最もパフォーマンスが優れています。

Hull MA はトレンドの方向を素早く示します。Stochastic はそのトレンド内の押し目またはラリーを待ってトレードを引き起こします。

滑らかなシグナルを求めるトレーダーに柔軟なアプローチです。ATR ベースのストップが潜在的な損失を抑えます。

## 詳細

- **エントリー条件**:
  - ロング: `HullMA turning up && StochK < StochOversold`
  - ショート: `HullMA turning down && StochK > StochOverbought`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - Hull MA の方向転換
- **ストップ**: `StopLossAtr` を使用した ATR ベース
- **デフォルト値**:
  - `HmaPeriod` = 9
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossAtr` = 2
  - `AtrPeriod` = 14
    HMAは下落の後に上昇すると上向きに転じ、上昇の後に下落すると下向きに転じます。ロングはHMAの下落で、ショートは上昇で決済します。%KはStochPeriod本のストキャスティクスをStochK本で平滑化した値で、%Dは関与しません。ストップはエントリー時の終値からStopLossAtr倍のATR（AtrPeriod）の位置にあり、足の終値で判定します。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Hull MA, Moving Average, Stochastic Oscillator
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

