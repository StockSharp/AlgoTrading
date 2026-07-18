# Fisher Org v1 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Fisher Transformインジケーターを使用してトレンド転換を捉えます。インジケーターが局所的な最小値を形成するとロングポジションが開かれ、局所的な最大値が現れるとショートポジションが開かれます。逆シグナルは既存のポジションを閉じます。

## ルール
- **ロング**: `Fisher[t-2] > Fisher[t-1]` かつ `Fisher[t-1] <= Fisher[t]`
- **ショート**: `Fisher[t-2] < Fisher[t-1]` かつ `Fisher[t-1] >= Fisher[t]`

## パラメーター
- `Fisher Length` – Fisher Transformの期間（デフォルト7）
- `Candle Type` – 計算に使用するローソク足の時間軸

## インジケーター
- Fisher Transform
