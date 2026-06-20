# ATR フィルター付き三陰線 / 三陽線戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ATR が 30 期間 SMA を上回っている場合に、3 本連続する弱気ローソク足の後にロングを建てます。3 本の陽線が出るか、最大トレード期間に達した際に決済します。

## パラメーター

- **CandleType**: ローソク足の種類。
- **MaxTradeDuration**: ポジションを保有する最大バー数。
- **UseGreenExit**: 3 本の陽線が出た後に決済するかどうか。
- **AtrPeriod**: ATR 計算の期間（0 でフィルターを無効化）。
