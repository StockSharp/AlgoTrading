# RSI CCI Williams %R 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はRSI、CCI、Williams %Rを組み合わせてリバーサルの機会を捉えます。3つのインジケーターがすべて売られすぎのレベルに達したときに買い、すべて買われすぎのレベルに達したときに売ります。各取引にはパーセントベースのテイクプロフィットとストップロス保護を使用します。

## 詳細

- **エントリー条件**:
  - **ロング**: `RSI < RSI 売られすぎ` && `CCI < CCI 売られすぎ` && `Williams %R < Williams 売られすぎ`
  - **ショート**: `RSI > RSI 買われすぎ` && `CCI > CCI 買われすぎ` && `Williams %R > Williams 買われすぎ`
- **エグジット条件**: テイクプロフィットまたはストップロスでポジションをクローズ。
- **タイプ**: リバーサル
- **インジケーター**: RSI, CCI, Williams %R
- **時間軸**: 45分（デフォルト）
- **ストップ**: パーセントベースのテイクプロフィットとストップロス
