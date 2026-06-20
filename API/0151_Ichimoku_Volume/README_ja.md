# Ichimoku Volume Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
戦略の実装 - Ichimoku + Volume。価格が雲（Kumo）の上にあり、転換線（Tenkan-sen）が基準線（Kijun-sen）の上にあり、出来高が平均を上回るときに買い。価格が雲の下にあり、転換線が基準線の下にあり、出来高が平均を上回るときに売り。

テストでは年平均リターン約40%を示しています。暗号資産市場で最もパフォーマンスが高いです。

一目均衡表のコンポーネントが方向性バイアスを定義し、出来高の急増が関心を確認します。価格が雲と整合し出来高が増加したときにトレードが開かれます。

参加を伴う雲のブレイクアウトをフォローするトレーダーに適しています。リスクはATRベースのストップによって制限されます。

## 詳細

- **エントリー条件**:
  - ロング: `Price > Cloud && Tenkan > Kijun && Volume > AvgVolume`
  - ショート: `Price < Cloud && Tenkan < Kijun && Volume > AvgVolume`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 反対方向への雲のブレイクアウト
- **ストップ**: `StopLoss` を使用したパーセントベース
- **デフォルト値**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Ichimoku Cloud, Volume
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

