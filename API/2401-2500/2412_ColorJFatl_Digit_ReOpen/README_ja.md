# ColorJFatl Digit ReOpen 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は Jurik Moving Average (JMA) を使用してトレンド方向を識別します。JMA が上向きに転換するとロングポジションを開き、すべてのショートポジションを閉じます。JMA が下向きに転換するとショートポジションを開き、すべてのロングポジションを閉じます。最大数に達するまで、取引方向に固定ポイント数だけ価格が動くたびに追加ポジションが加えられます。

## 詳細

- **エントリー**:
  - JMA が上向きに方向転換 → ロングを開き、ショートを閉じる。
  - JMA が下向きに方向転換 → ショートを開き、ロングを閉じる。
- **再エントリー**:
  - 最初のポジション後、`MaxPositions` に達するまで取引方向への `PriceStep` ポイントごとに新しいポジションが開きます。
- **エグジット**:
  - 反対方向の JMA 転換が現在のポジションを閉じます。
- **パラメーター**:
  - `JmaLength` – JMA の期間。
  - `PriceStep` – 再エントリーに必要な価格移動（ポイント）。
  - `MaxPositions` – 同時ポジションの最大数。
  - `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` – アクションの有効化または無効化。
  - `CandleType` – 計算のための時間軸。
- **インジケーター**: Jurik Moving Average。
- **タイプ**: トレンドフォロー。
- **時間軸**: デフォルト4時間。
