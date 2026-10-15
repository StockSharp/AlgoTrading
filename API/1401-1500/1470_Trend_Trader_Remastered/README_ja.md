# Trend Trader Remastered 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はパラボリック SAR インジケーターを使用してトレンドに追従します。価格が SAR を上回ると買い注文を出し、価格が SAR を下回ると売り注文を出します。逆のクロスが現在のポジションを決済します。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格が PSAR を上抜ける。
  - **ショート**: 価格が PSAR を下抜ける。
- **エグジット**: 逆の PSAR クロスでトレードを決済。
- **ストップ**: 追加のストップなし。
- **デフォルト値**:
  - `Start` = 0.02
  - `Increment` = 0.02
  - `Max` = 0.2
