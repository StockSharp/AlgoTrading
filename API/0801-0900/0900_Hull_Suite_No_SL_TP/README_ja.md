# Hull Suite SL/TPなし
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Hull Suite SL/TPなしは、Hull Moving Averageのバリエーションに基づくトレンドフォロー戦略です。Hull線が2本前のローソク足と比較して方向を変えたときにポジションを反転させます。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: Hullの値が2本前のローソク足より大きい。
  - **ショート**: Hullの値が2本前のローソク足より小さい。
- **エグジット条件**: 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length` = 55
  - `Mode` = `Hma`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング/ショート
  - インジケーター: Hull Moving Average
  - 複雑さ: 低
  - リスクレベル: 低
