# MACDサンプル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMetaTraderの古典的なMACDサンプルエキスパートを再現します。
MACDクロスとEMAトレンドフィルターを組み合わせ、ロングとショートそれぞれ個別のテイクプロフィットとストップロスのレベルを設定し、オプションのトレーリングストップも使用します。取引は設定可能な時間ウィンドウ内のみ許可されます。

## 詳細

- **エントリー条件**:
  - **ロング**: MACDラインがゼロより下でシグナルラインを上抜けし、EMAが上昇中。
  - **ショート**: MACDラインがゼロより上でシグナルラインを下抜けし、EMAが下降中。
- **エグジット条件**:
  - 逆方向のMACDクロス。
  - 個別のテイクプロフィットまたはストップロス目標に到達。
  - トレーリングストップに到達。
- **ロング/ショート**: 両方。
- **デフォルト値**:
  - `EMA Period` = 26
  - `MACD Open Level` = 3
  - `MACD Close Level` = 2
  - `Take Profit Long` = 50
  - `Take Profit Short` = 75
  - `Stop Loss Long` = 80
  - `Stop Loss Short` = 50
  - `Trailing Stop` = 30
  - 取引時間: 4時～19時 UTC
- **インジケーター**: MACD, EMA
- **時間軸**: デフォルトで1時間足
