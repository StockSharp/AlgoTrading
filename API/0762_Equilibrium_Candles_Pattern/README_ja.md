# 均衡ローソク足パターン戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

均衡ローソク足を使って短期トレンドを検出し、プルバックでエントリーする戦略です。均衡とは、ルックバック期間における最高値と最安値の中間点です。強気または弱気の連続後、価格が均衡線を再び突き抜けるとエントリーが発動されます。ATRはオプションのストップ/目標値に使用され、異常に大きなローソク足での退出にも使用されます。

## 詳細

- **エントリー条件**:
  - **ロング**: 強気トレンド後に価格が均衡線を下回った場合。
  - **ショート**: 弱気トレンド後に価格が均衡線を上回った場合。
- **ロング/ショート**: 両方
- **ストップ**: ATRベースのストップロスとテイクプロフィット（オプション）
- **デフォルト値**:
  - `EquilibriumLength` = 9
  - `CandlesForTrend` = 7
  - `MaxPullbackCandles` = 2
  - `AtrPeriod` = 14
  - `StopMultiplier` = 2
  - `UseTpSl` = true
  - `UseBigCandleExit` = true
  - `BigCandleMultiplier` = 1
  - `UseReverse` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
