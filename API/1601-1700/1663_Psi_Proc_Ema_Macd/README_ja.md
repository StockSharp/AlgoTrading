# Psi Proc EMA MACD戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はオリジナルのMQLエキスパート`e-PSI@PROC.mq4`のT4システムを再現します。複数の指数移動平均線の並びとMACDフィルターに基づいて取引します。

## 戦略ロジック

1. 各受信ローソク足でEMA(200)、EMA(50)、EMA(10)を計算する。
2. パラメーター12、26、9でMACDを計算する。
3. 以下の条件でロングに入る:
   - EMA200が上昇中でEMA50 > EMA200。
   - EMA50が上昇中でEMA10 > EMA50。
   - MACDが上昇中で`LimitMACD`を上回る。
4. 以下の条件でショートに入る:
   - EMA200が下降中でEMA50 < EMA200。
   - EMA50が下降中でEMA10 < EMA50。
   - MACDが下降中で`-LimitMACD`を下回る。
5. 価格がEMA50を下回って引けるとロングを決済する。
6. 価格がEMA50を上回って引けるとショートを決済する。

オプションのテイクプロフィットとトレーリングストップの保護がサポートされています。

## パラメーター

| 名前 | 説明 |
| ---- | ---- |
| `LimitMACD` | エントリーを許可する最小絶対MACDレベル。 |
| `TakeProfitPoints` | 価格ポイント単位のテイクプロフィットレベル。 |
| `TrailStopPoints` | 価格ポイント単位のトレーリングストップレベル。 |
| `CandleType` | 戦略が使用するローソク足の時間軸。 |

## 注意事項

- 取引は成行注文で建てられます。
- 完了したローソク足のみが処理されます。
- 戦略は単一の銘柄で動作します。
