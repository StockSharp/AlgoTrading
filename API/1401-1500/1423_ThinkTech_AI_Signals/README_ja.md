# ThinkTech AIシグナル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はセッションの最初の15分足ローソク足のブレイクアウトを取引します。ATRベースのストップロスとテイクプロフィットレベルを使用し、オプションのトレンドおよびRSIフィルターを適用できます。

## 詳細

- **エントリー条件**:
  - **ロング**: トレンドとRSIフィルターが満たされた状態で、価格が最初のローソク足の高値を上抜ける。
  - **ショート**: トレンドとRSIフィルターが満たされた状態で、価格が最初のローソク足の安値を下抜ける。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - テイクプロフィットまたはストップロスレベルへの到達。
- **ストップ**: はい、ATRベース。
- **デフォルト値**:
  - `RiskRewardRatio` = 2
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
