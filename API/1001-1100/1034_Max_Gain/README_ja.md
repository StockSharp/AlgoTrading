# 最大利益戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Max Gainは、振り返り期間中の最安値から現在の高値までの割合距離と、最高値から現在の安値までの割合距離を比較します。潜在的な利益が調整後の損失を上回る場合はロング、そうでない場合はショートします。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: Max gain > adjusted max loss.
  - **ショート**: Adjusted max loss > max gain.
- **エグジット条件**: 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `PeriodLength` = 30
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: ロングとショート
  - インジケーター: Highest, Lowest
  - 複雑さ: 低
  - リスクレベル: 中
