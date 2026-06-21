# Color Schaff モメンタムトレンドサイクル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はColor Schaff Momentum Trend Cycle（STC）を使用し、インジケーターが過買いまたは過売りゾーンを出るときにトレンド転換を検出します。

## 詳細

- **エントリー条件**:
  - 前のSTC色が上位ゾーン（>5）にあり、現在の色が6を下回った場合に買い、ショートポジションを閉じます。
  - 前のSTC色が下位ゾーン（<2）にあり、現在の色が1を上回った場合に売り、ロングポジションを閉じます。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルが反対のポジションを閉じます。
- **ストップ**: 明示的なストップロスやテイクプロフィットなし。
- **デフォルト値**:
  - `FastMomentum` = 23
  - `SlowMomentum` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true

