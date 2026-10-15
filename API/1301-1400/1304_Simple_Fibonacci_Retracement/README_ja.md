# シンプル Fibonacci リトレースメント戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ルックバック期間中の最高値と最安値から導出した Fibonacci リトレースメントレベルを使用します。価格が選択した Fibonacci レベルを突破すると、戦略はポジションをオープンし、固定ピップスベースのテイクプロフィットとストップロス注文を配置します。

## 詳細

- **エントリー**: 選択した Fibonacci レベルを上または下に突破。
- **イグジット**: 固定テイクプロフィットまたはストップロス。
- **インジケーター**: Highest、Lowest。
- **ストップ**: あり。
- **デフォルト値**:
  - `LookbackPeriod` = 100
  - `TakeProfitPips` = 50
  - `StopLossPips` = 20
- **方向**: 両方。
