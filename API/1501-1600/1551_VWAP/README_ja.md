# VWAP戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

エントリーバンドと複数のエグジットモードを備えたVWAPを使用します。価格が下限バンドより上で引けたときに買い、上限バンドより下で引けたときに売ります。VWAPまたは偏差バンドによるエグジット、および連続する逆行ローソク足後のオプションの安全エグジットをサポートします。

## パラメーター

- **StopPoints**: シグナルバーからのストップバッファ。
- **ExitModeLong**: ロングポジションのエグジットモード。
- **ExitModeShort**: ショートポジションのエグジットモード。
- **TargetLongDeviation**: ロング目標の偏差乗数。
- **TargetShortDeviation**: ショート目標の偏差乗数。
- **EnableSafetyExit**: 逆行バー後の安全エグジットを有効化。
- **NumOpposingBars**: 安全エグジットのための逆行バー数。
- **AllowLongs**: ロングトレードを許可。
- **AllowShorts**: ショートトレードを許可。
- **MinStrength**: 最小シグナル強度。
- **CandleType**: ローソク足の種類。
