# Good Mode RSI v2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はRSIの極値をカスタムの利確およびトレーリングストップしきい値で取引します。RSIが高いレベルを超えると売り、RSIが利確値まで下落すると決済します。RSIが低いレベルまで下落すると買い、RSIが利益目標まで上昇すると決済します。いずれの場合も、トレーリングストップが最も有利な価格を追跡して利益を保護します。

## 詳細

- **エントリー条件**:
  - **ロング**: `RSI < buy level`.
  - **ショート**: `RSI > sell level`.
- **ロング/ショート**: 両方.
- **エグジット条件**:
  - **ロング**: `RSI > take profit level buy` またはトレーリングストップ発動.
  - **ショート**: `RSI < take profit level sell` またはトレーリングストップ発動.
- **ストップ**: ティック単位のトレーリングストップ.
- **デフォルト値**:
  - `RSI Period` = 2
  - `Sell Level` = 96
  - `Buy Level` = 4
  - `Take Profit Level Sell` = 20
  - `Take Profit Level Buy` = 80
  - `Trailing Stop Offset` = 100
- **フィルター**:
  - カテゴリ: Momentum
  - 方向: 両方
  - インジケーター: 単一
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
