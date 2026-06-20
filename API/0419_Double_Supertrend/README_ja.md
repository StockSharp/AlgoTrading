# Double Supertrend 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Double Supertrend は、異なる期間と乗数を持つ2つのATRベースの移動平均を使用します。
最初のラインがトレードの方向を設定し、2番目のラインがターゲットまたはトレーリング
エグジットとして機能します。この組み合わせにより、定義された利益とリスクのパラメーターで
柔軟なトレンドフォローが可能になります。

価格が両方のラインを上回り、戦略がロング取引に設定されていると、ポジションが開かれます。
ショート取引の場合、条件は反転します。エグジットは選択した利確タイプまたはパーセンテージ
ストップロスに依存します。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**: 価格が許可された `Direction` でSupertrend ラインをクロスする。
- **エグジット条件**: 反対ラインのブレイク、利確（`TPType`/`TPPercent`）またはストップロス（`SLPercent`）。
- **ストップ**: `SLPercent` に基づくパーセンテージストップ。
- **デフォルト値**:
  - `ATRPeriod1` = 10
  - `Factor1` = 3.0
  - `ATRPeriod2` = 20
  - `Factor2` = 5.0
  - `Direction` = "Long"
  - `TPType` = "Supertrend"
  - `TPPercent` = 1.5
  - `SLPercent` = 10.0
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 設定可能
  - インジケーター: ATR‑based Supertrend
  - 複雑さ: 上級
  - リスクレベル: 中
