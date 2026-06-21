# ColorSchaff JJRSX トレンドサイクル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、JJRSX平均に基づくSchaff Trend Cycleオシレーターを使用します。オシレーターがユーザー定義のレベルをクロスしたときにロングまたはショートポジションを開きます。

## 詳細

- **エントリー条件**:
  - Schaff Trend Cycleが`HighLevel`を上抜けたときに買い。既存のショートポジションを先に決済します。
  - Schaff Trend Cycleが`LowLevel`を下抜けたときに売り。既存のロングポジションを先に決済します。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のエントリーシグナルが発生したときにポジションを決済します。
- **ストップ**: なし。
- **デフォルト値**:
  - `Fast` = 23
  - `Slow` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
