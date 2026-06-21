# ETH/USDT EMAクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は追加フィルターを備えたEMAクロスオーバーを使用してETH/USDTを取引します。

20期間EMAが50期間EMAを上抜き、価格が200期間EMAを上回り、RSIが30超、ATRで測定したボラティリティがその移動平均を上回り、出来高が平均を上回るときにロングポジションをオープンします。ショートポジションは逆の条件でオープンします。

反対のシグナルが現れると、ポジションが反転します。明示的なストップロスやテイクプロフィットは使用しません。

## 詳細

- **エントリー条件**:
  - **ロング**: `EMA20がEMA50を上抜く` && `Close > EMA200` && `RSI > 30` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
  - **ショート**: `EMA20がEMA50を下抜く` && `Close < EMA200` && `RSI < 70` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
- **ロング/ショート**: 両側
- **エグジット条件**:
  - 反転シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `EMA200 Length` = 200
  - `EMA20 Length` = 20
  - `EMA50 Length` = 50
  - `RSI Length` = 14
  - `ATR Length` = 14

- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA, RSI, ATR
  - ストップ: なし
  - 複雑さ: 中程度
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
