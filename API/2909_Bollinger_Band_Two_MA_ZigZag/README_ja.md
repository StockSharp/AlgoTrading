# Bollinger Band Two MA ZigZag 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bollinger Band のリバーサル、上位時間軸の2本の移動平均線、ZigZag 検出器のスイングポイントを組み合わせたハイブリッドなトレンドフォロー・システム。シグナルごとに2つのポジションを開く：一方は計算されたテイクプロフィット目標を持ち、もう一方はトレーリングとブレイクイーブン・ロジックに依存する「ランナー」ポジションである。

## 詳細

- **エントリー条件**:
  - **ロング**: 直前のバーが2本前に下降 Bollinger Band を下回った後、前の下降バンドより上でクローズし、現在の終値もその下降バンドより上にあり、価格が両方の上位時間軸移動平均線より上にある。
  - **ショート**: 直前のバーが2本前に上昇 Bollinger Band を上回った後、前の上昇バンドより下でクローズし、現在の終値もその上昇バンドより下にあり、価格が両方の上位時間軸移動平均線より下にある。
- **ポジション管理**:
  - シグナルごとに `First Volume`（テイクプロフィットあり）と `Second Volume`（ランナー）の2つのポジションを開く。
  - ストップは最新の ZigZag スイング極値から `Pivot Offset (pts)` を引いた/加えた値に固定される。
  - ブレイクイーブン保護は、未実現利益が `Break-even Threshold (pts)` + `Break-even Offset (pts)` を超えた時点でストップをエントリー価格プラスオフセットに移動させる。
  - トレーリングストップは、価格が既存ストップを超えて `Trailing Step (pts)` 進んだ後に動き、`Trailing Stop (pts)` の距離を維持する。
- **テイクプロフィット**:
  - 最初のポジションのテイクプロフィットはエントリーとストップ間の距離の割合（`Take Profit %`）として計算される。
  - ランナーポジションには固定目標がなく、ストップ、トレーリング、または逆方向シグナルで決済される。
- **追加ロジック**:
  - 逆方向シグナルは新規取引を開く前に、反対方向の未決済ポジションを即座に閉じる。
  - シグナル処理は確定した足を使用し、部分データは無視される。
- **デフォルト値**:
  - `First Volume` = 0.1
  - `Second Volume` = 0.1
  - `Take Profit %` = 50
  - `Pivot Offset (pts)` = 10
  - `Use Break-even Move` = true
  - `Break-even Offset (pts)` = 80
  - `Break-even Threshold (pts)` = 10
  - `Trailing Stop (pts)` = 80
  - `Trailing Step (pts)` = 120
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `Base Candle` = 1時間足
  - `MA1 Candle` = 日足
  - `MA2 Candle` = 4時間足
  - `MA1 Period` = 20
  - `MA2 Period` = 20
  - `ZigZag Depth` = 12
  - `ZigZag Deviation (pts)` = 5
  - `ZigZag Backstep` = 3
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Bollinger Bands、Moving Averages、ZigZag
  - ストップ: はい（スイングストップ、ブレイクイーブン、トレーリング）
  - 複雑さ: 上級
  - 時間軸: マルチ時間軸（1h ベース、Daily + 4h フィルター）
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

## 注意事項

- この戦略はフィルターを評価しエグジットを管理するために、3つの異なる時間軸でのローソク足サブスクリプションが必要である。
- スイング検出は、ピボットレベルを更新する前に最小深度、偏差、バックステップのルールを適用することで MetaTrader ZigZag ロジックを近似する。
- ボリュームは独立して調整でき、テイクプロフィット・レッグとランナー・レッグのサイズ比率を調整できる。
