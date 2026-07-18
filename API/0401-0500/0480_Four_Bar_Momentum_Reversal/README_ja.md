# 4バー・モメンタム・リバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

4バー・モメンタム・リバーサル戦略は、選択した時間ウィンドウ内で終値が`Lookback`バー前の終値を少なくとも`BuyThreshold`本連続で下回った場合にロングエントリーします。価格が前のローソク足の高値を上回ると、ポジションを閉じます。

## 詳細

- **エントリー条件**: 時間ウィンドウ内で`BuyThreshold`本連続する終値が`Lookback`バー前の終値を下回る。
- **エグジット条件**: 終値が前のローソク足の高値を上回る。
- **ストップ**: なし。
- **デフォルト値**:
  - `BuyThreshold` = 4
  - `Lookback` = 4
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
