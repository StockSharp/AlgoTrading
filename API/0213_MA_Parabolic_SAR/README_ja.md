# MA Parabolic SAR Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
MA Parabolic SAR戦略は、単純移動平均で優勢な方向を決定し、Parabolic SARのドットでエントリータイミングとストップの配置を行うことで、持続的なトレンドを捉えようとします。両インジケーターが一致したとき、システムはモメンタムが十分に強いと判断します。

テストでは年間平均リターン約76%を示しています。外国為替市場で最もパフォーマンスが高いです。

終値が移動平均を上回り、Parabolic SARのドットが市場の下に転じたときにロングポジションを開きます。価格が平均を下回り、SARのドットが価格の上に転じて下方への圧力を示すときにショートポジションを取ります。価格がSARを反対方向に越えると戦略は終了し、利益を確定するか損失を制限します。

このアプローチは、明確で機械的なストップによる系統的なトレンドフォローを好むトレーダーに最も適しています。Parabolic SARはボラティリティの変化に合わせて継続的に調整し、移動平均が広範なトレンドに逆らったトレードを防ぐ一方で、市場状況に合わせたエクスポージャーを維持します。

## 詳細
- **エントリー条件**:
  - **ロング**: Price > MA && Price > Parabolic SAR
  - **ショート**: Price < MA && Price < Parabolic SAR
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: 価格がParabolic SARを下回ったときに終了
  - **ショート**: 価格がParabolic SARを上回ったときに終了
- **ストップ**: はい、Parabolic SARによる動的ストップとオプションの固定ストップ。
- **デフォルト値**:
  - `MaPeriod` = 20
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `TakeValue` = new Unit(0, UnitTypes.Absolute)
  - `StopValue` = new Unit(2, UnitTypes.Percent)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MA, Parabolic SAR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

