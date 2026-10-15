# ボラティリティ・アクション戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、4時間足で計算されたBill Williamsの**Alligator**トレンドフィルターと短期のボラティリティブレイクアウトを組み合わせます。

## トレードルール
- **ロングエントリー**の条件:
  - 期間1のATRが、*Volatility Coef*と期間*ATR Period*のATRの積より大きい。
  - ローソク足が陽線で24本の高値を更新する。
  - Alligatorラインが上向きに整列（Lips > Teeth > Jaw）し、始値と終値の両方がTeeth線より上にある。
- **ショートエントリー**: 上記条件が反対方向に当てはまるとき。

エントリー時に、戦略はATR(1)の倍数としてストップロスとテイクプロフィットのレベルを設定します:
- ストップロス = エントリー価格 ± *Stop Coef* × ATR(1)
- テイクプロフィット = エントリー価格 ± *Profit Coef* × ATR(1)

## パラメーター
- **Volatility Coef** – 高速ATRと低速ATRを比較する乗数。
- **ATR Period** – 低速ATRの期間。
- **Stop Coef** – ストップロス用ATR乗数。
- **Profit Coef** – テイクプロフィット用ATR乗数。
- **Candle Type** – メイン分析の時間軸（Alligatorは4H足を使用）。
