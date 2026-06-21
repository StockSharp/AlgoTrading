# Syndicate Trader戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、`MQL/12351` フォルダにある元のMetaTraderスクリプト **Syndicate_Trader_v_1_04.mq4** をStockSharpに移植したものです。

ボリュームスパイクの確認を伴う、速いと遅い指数移動平均線のクロスオーバーに基づいて取引します。オプションのセッションフィルターで取引を特定の時間帯に制限できます。シンプルなテイクプロフィットとストップロスレベルでリスクを管理します。

## 詳細

- **エントリー条件**:
  - **ロング**: 速いEMAが遅いEMAを上抜けし、出来高が移動平均出来高に設定可能な係数を乗じた値を超える場合。
  - **ショート**: 速いEMAが遅いEMAを下抜けし、同じ出来高確認条件を満たす場合。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 反対のクロスオーバー。
  - ストップロスまたはテイクプロフィット到達。
  - 許可されたセッションウィンドウ外。
- **ストップ**: 価格ポイント単位の固定ストップロスとテイクプロフィット。
- **フィルター**:
  - 出来高スパイクフィルター。
  - オプションのセッション時間フィルター。

## パラメーター

| 名前 | 説明 |
|------|------|
| `FastEmaLength` | 速いEMAの期間。 |
| `SlowEmaLength` | 遅いEMAの期間。 |
| `VolumeMaLength` | 出来高を平均化する期間。 |
| `VolumeMultiplier` | スパイクを定義するために平均出来高に乗じる係数。 |
| `TakeProfitPoints` | 価格ポイント単位のテイクプロフィット。 |
| `StopLossPoints` | 価格ポイント単位のストップロス。 |
| `UseSessionFilter` | セッションフィルターを有効または無効にする。 |
| `SessionStartHour/SessionStartMinute` | 取引セッションの開始時刻。 |
| `SessionEndHour/SessionEndMinute` | 取引セッションの終了時刻。 |
| `CandleType` | ローソク足のタイプと時間軸。 |
