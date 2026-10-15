# トレンドキャッチャー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Trend Catcher**戦略は、Parabolic SARと複数の単純移動平均を組み合わせ、方向性のある動きを捉えます。優勢な速い平均の方向にParabolic SARを価格が突破するのを待ち、動的なストップロスとトレーリングルールを使ってポジションを管理します。

最新のローソク足が前のローソク足とは反対側のParabolic SARを超えて終値をつけ、速い移動平均が動きを確認したときにトレードが開かれます。初期ストップロスはSARポイントまでの距離から計算され、最小・最大の制限で範囲が決まります。利益目標はストップ距離の倍数として定義されます。価格が指定された量だけ進んだ後、ストップは小さなオフセットを伴って損益分岐点に移動し、その後価格をトレールします。

## 詳細

- **エントリー条件**:
  - **ロング**: `Close[0] > SAR && Close[1] < SAR_prev && FastMA > SlowMA && Close > FastMA2`.
  - **ショート**: `Close[0] < SAR && Close[1] > SAR_prev && FastMA < SlowMA && Close < FastMA2`.
- **エグジット条件**:
  - ストップロスまたはテイクプロフィットレベルに到達。
  - 利益閾値後にトレーリングストップが有効化。
  - 逆シグナルが既存ポジションを決済。
- **ストップ**: SARに基づく動的ストップロス（オプションの損益分岐点調整とトレーリング付き）。
- **デフォルト値**:
  - `SlowMaPeriod = 200`
  - `FastMaPeriod = 50`
  - `FastMa2Period = 25`
  - `SarStep = 0.004`
  - `SarMax = 0.2`
  - `SlMultiplier = 1`
  - `TpMultiplier = 1`
  - `MinStopLoss = 10`
  - `MaxStopLoss = 200`
  - `ProfitLevel = 500`
  - `BreakevenOffset = 1`
  - `TrailingThreshold = 500`
  - `TrailingDistance = 10`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Parabolic SAR, SMA
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
