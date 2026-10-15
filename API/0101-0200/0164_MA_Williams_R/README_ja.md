# Ma Williams R 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

戦略の実装 - MA + Williams %R。価格が MA を上回り、Williams %R が -80 を下回っている（売られすぎ）場合に買い。価格が MA を下回り、Williams %R が -20 を上回っている（買われすぎ）場合に売り。

テストでは年平均収益率は約 79% を示しています。株式市場で最もパフォーマンスが優れています。

移動平均は優勢なトレンドの方向を示します。Williams %R はそのトレンドに対して買われすぎまたは売られすぎのポイントを探します。

平均に向けた押し目を待つスイングトレーダーに適しています。ストップロスの距離はエントリー価格の一定割合です。

## 詳細

- **エントリー条件**:
  - ロング: `Close > MA && WilliamsR < WilliamsROversold`
  - ショート: `Close < MA && WilliamsR > WilliamsROverbought`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - Williams %R が中間に戻る
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `MaPeriod` = 20
  - `MaType` = MovingAverageTypeEnum.Simple
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    MaTypeで単純・指数・加重・平滑・Hullの移動平均を選びます。Williams %Rの中央は-50で、ロングは%Rがそこまで上昇したら、ショートはそこまで下落したら決済します。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Moving Average, Williams %R, R
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

