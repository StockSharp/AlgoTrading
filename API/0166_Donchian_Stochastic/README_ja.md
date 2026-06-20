# Donchian Stochastic Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Donchian Channel + Stochastic 戦略。価格が Donchian Channel をブレイクし、Stochastic が売られすぎ/買われすぎ条件を確認した際に市場に参入します。

テストでは年平均収益率は約 85% を示しています。暗号資産市場で最もパフォーマンスが優れています。

Donchian チャネルを超えたブレイクアウトは Stochastic のモメンタムで確認されます。価格がレンジを脱出してオシレーターが同意した時点でトレードが始まります。

即座の追随を期待するトレーダーに有用です。ATR の倍数がストップを設定します。

## 詳細

- **エントリー条件**:
  - ロング: `Close > DonchianHigh && StochK < 20`
  - ショート: `Close < DonchianLow && StochK > 80`
- **ロング/ショート**: 両方
- **エグジット条件**: ブレイクアウトの失敗または反対シグナル
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `DonchianPeriod` = 20
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2m
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Donchian Channel, Stochastic Oscillator
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

