# FON60DK
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Tillson T3ラインがOptimized Trend Tracker（OTT）の上限バンドを上回り、Williams %Rが強気モメンタムを確認したときにロングポジションを建てます。Tillson T3が反対側のOTTバンドを下回り、Williams %Rが売られ過ぎゾーンに入るとポジションを決済します。

## 詳細

- **エントリー条件**: `T3 > OTT_up` && `Williams %R > -20`
- **エグジット条件**: `T3_SAT < OTT_dn_SAT` && `Williams %R < -70`
- **タイプ**: トレンドフォロー
- **インジケーター**: Tillson T3, OTT, Williams %R
- **時間軸**: 1分（デフォルト）
- **ストップ**: なし
