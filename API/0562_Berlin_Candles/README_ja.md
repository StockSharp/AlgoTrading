# Berlin Candles戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

平滑化されたHeikin Ashi値から導出されたカスタムBerlinローソク足を使用した戦略です。強気のBerlinローソク足がDonchianベースラインを上回るときにロングポジションを開きます。弱気のBerlinローソク足がベースラインを下回るときにショートポジションを開きます。

## 詳細

- **エントリー条件**:
  - **ロング**: Berlin終値 > Berlin始値 かつ Berlin終値 > ベースライン。
  - **ショート**: Berlin終値 < Berlin始値 かつ Berlin終値 < ベースライン。
- **ロング/ショート**: 両方
- **ストップ**: デフォルトではなし
- **デフォルト値**:
  - `Smoothing` = 1
  - `BaselinePeriod` = 26
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
