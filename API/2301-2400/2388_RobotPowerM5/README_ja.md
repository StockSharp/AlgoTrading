# RobotPower M5戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は5分チャートでBulls PowerとBears Powerのインジケーターを組み合わせています。
強気と弱気の合算モメンタムがゼロをクロスしたときにポジションを建て、固定ターゲットとトレーリングストップでエグジットを管理します。

## 仕組み
- **インジケーター**: Bulls PowerとBears Power（共通期間 `BullBearPeriod`）。
- **時間軸**: デフォルトは5分足（`CandleType`）。

### エントリールール
- **ロングエントリー**: `BullsPower + BearsPower > 0` かつポジションなしのとき、成行で買い。
- **ショートエントリー**: `BullsPower + BearsPower < 0` かつポジションなしのとき、成行で売り。

### エグジットルール
- **テイクプロフィット**: 取引方向に価格が `TakeProfit` 単位動いたらポジションをクローズ。
- **ストップロス**: ポジションに対して逆方向に価格が `StopLoss` 単位動いたらポジションをクローズ。
- **トレーリングストップ**: エントリー後、価格がその距離の2倍以上進んだら、ストップロスが `TrailingStep` だけ追随します。

### パラメーター
- `BullBearPeriod` – Bulls PowerとBears Powerの計算期間。
- `TrailingStep` – トレーリングストップ調整時のステップサイズ。
- `TakeProfit` – エントリーからテイクプロフィットレベルまでの距離。
- `StopLoss` – エントリーからストップロスレベルまでの距離。
- `CandleType` – シグナル計算用のローソク足の時間軸。

### ポジションサイズ
注文サイズには戦略の `Volume` プロパティを使用します。

## 注記
教育目的で設計されており、MQL戦略をStockSharp APIに変換する例として機能します。
