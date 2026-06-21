# Malr チャネルブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、カスタムMALR（Moving Average Linear Regression）チャネルのブレイクアウトを取引します。MALRインジケーターは単純移動平均と線形加重移動平均を組み合わせて中心線を形成します。この線に対する価格の標準偏差が2つの外側バンドを作成します。

上側のブレイクアウトバンドが終値を下抜けすると、上方向のブレイクアウトを示すロングポジションが建てられます。下側のブレイクアウトバンドが終値を上抜けすると、下方向のブレイクアウトを示すショートポジションが建てられます。

## パラメーター

- `MaPeriod` – 移動平均と標準偏差の期間。
- `ChannelReversal` – 標準偏差で測定した内側MALRチャネルの幅。
- `ChannelBreakout` – 外側ブレイクアウトチャネルの追加幅。
- `CandleType` – 計算に使用するローソク足の種類。

## 動作原理

1. 終値のSMAとLWMAを計算します。
2. MALRライン`FF = 3 * LWMA - 2 * SMA`を計算します。
3. 同じ期間で`close - FF`の標準偏差を測定します。
4. ブレイクアウトバンドを導出：`FF ± StdDev * (ChannelReversal + ChannelBreakout)`。
5. 上側バンドが上から下へ終値を下抜けしたときにロングを建てます。
6. 下側バンドが下から上へ終値を上抜けしたときにショートを建てます。

この戦略は新しいポジションを建てる前に常に反対方向のポジションを決済します。
