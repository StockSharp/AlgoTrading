# 時間フィルター付きブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

指定された取引セッション内で、価格が最近の高値を上回るか最近の安値を下回ったときにエントリーするブレイクアウト戦略です。オプションの移動平均フィルターが方向を確認します。ストップロスはATR、ローソク足の極値、または設定可能なリスク・リワード目標を持つ固定ポイントに基づくことができます。

## 詳細

- **エントリー条件**:
  - **ロング**: 終値 > `Length`期間の最高値 かつ時間ウィンドウ内；オプションで終値 > MA。
  - **ショート**: 終値 < `Length`期間の最安値 かつ時間ウィンドウ内；オプションで終値 < MA。
- **ロング/ショート**: 両方
- **ストップ**: ATR、ローソク足ベース、またはリスク・リワード目標付き固定ポイント
- **デフォルト値**:
  - `Length` = 5
  - `MaLength` = 99
  - `UseMaFilter` = false
  - `UseTimeFilter` = true (14:30–15:00)
  - `SlType` = Atr
  - `SlLength` = 0
  - `AtrLength` = 14
  - `AtrMultiplier` = 0.5
  - `PointsStop` = 50
  - `RiskReward` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
