# マルチ時間軸 Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

複数の時間軸からの Parabolic SAR シグナルを組み合わせます。パラメーターで選択した SAR レベルより価格が上にある場合にロングを建てます。選択した SAR より価格が下落したときにショートが発生します。オプションのストップロス、トレーリングストップ、利食いが利用できます。

## 詳細

- **エントリー条件**:
  - **ロング**: `LongSource` の設定に従い SAR より価格が上。
  - **ショート**: `ShortSource` の設定に従い SAR より価格が下。
- **エグジット条件**:
  - 逆方向の SAR クロスオーバーまたは保護のトリガー。
- **インジケーター**:
  - 現在の時間軸の Parabolic SAR
  - 上位および下位時間軸のオプション Parabolic SAR
- **ストップ**: StartProtection によるオプションのストップロス、トレーリングストップ、利食い。
- **デフォルト値**:
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `StopLossPercent` = 1
  - `TrailingPercent` = 0.5
  - `TakeProfitPercent` = 2
- **フィルター**:
  - 時間軸: メイン 5m、上位 1d、下位 1m
  - インジケーター: Parabolic SAR
  - ストップ: オプション
  - 複雑さ: 中程度
