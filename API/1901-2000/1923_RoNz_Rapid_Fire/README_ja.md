# RoNz 速射戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は移動平均とParabolic SARインジケーターを組み合わせて、急速なトレンド変化を検出します。終値が移動平均を上回り、Parabolic SARが価格の下に反転するとロングポジションを開きます。逆の条件でショートポジションを開きます。トレンドが継続する場合、オプションでポジションを平均化できます。

## 仕組み
- **ロングエントリー**: 終値 > SMA かつ Parabolic SARが価格の下に切り替わる。
- **ショートエントリー**: 終値 < SMA かつ Parabolic SARが価格の上に切り替わる。
- **決済**: 選択したモードに応じて、ストップロス/テイクプロフィットまたは逆方向シグナルで決済。
- **平均化**: トレンドが継続する場合に新しいポジションを追加。
- **トレーリングストップ**: 取引が利益方向に進むにつれてストップ価格を調整。

## パラメーター
- `Volume` – 取引量。
- `StopLoss` – ストップロス（ティック単位）。
- `TakeProfit` – テイクプロフィット（ティック単位）。
- `TrailingStop` – トレーリングストップ（ティック単位）。
- `Averaging` – ポジション平均化を有効にする。
- `MaPeriod` – 移動平均の期間。
- `PsarStep` – Parabolic SARのステップ。
- `PsarMax` – Parabolic SARの最大値。
- `CloseType` – `SlClose`はストップのみ使用、`TrendClose`は逆トレンドで決済。
- `CandleType` – 計算に使用するローソク足シリーズ。

## 注記
- StockSharpがサポートする任意の銘柄で動作します。
- 選択した`CandleType`の過去ローソク足データが必要です。
