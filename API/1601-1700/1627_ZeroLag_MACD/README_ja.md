# ZeroLag MACDクロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMACD線とシグナル線のクロスオーバーに基づいて取引します。MetaTraderエキスパートアドバイザー **ZeroLagEA-AIP v0.0.4** から変換されました。戦略は設定されたセッション時間中のみ動作し、オプションで現在のバーにクロスオーバーが発生することを要求できます。

## 詳細

- **エントリー条件**:
  - **ロング**: MACD線がシグナル線を上方向にクロス。
  - **ショート**: MACD線がシグナル線を下方向にクロス。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆方向のクロスオーバーまたは指定された日時での強制エグジット。
- **ストップ**: なし。
- **フィルター**:
  - `StartHour` と `EndHour` で定義されたセッション時間。
  - オプションの新鮮なクロスオーバー要件 (`UseFreshSignal`)。

## パラメーター

- `FastEmaLength` = 2
- `SlowEmaLength` = 34
- `SignalEmaLength` = 2
- `UseFreshSignal` = true
- `Volume` = 2
- `StartHour` = 9
- `EndHour` = 15
- `KillDay` = 5
- `KillHour` = 21
- `CandleType` = 1分足ローソク足
