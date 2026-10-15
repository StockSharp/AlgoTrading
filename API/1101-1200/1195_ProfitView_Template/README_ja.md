# ProfitView戦略テンプレート
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ProfitViewテンプレートから派生した、設定可能なスムージングタイプを持つ基本的な移動平均クロスオーバー戦略。

## 詳細

- **エントリー条件**:
  - **ロング**: MA1がMA2を上抜く。
  - **ショート**: MA1がMA2を下抜く。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対方向のクロスオーバー。
- **ストップ**: なし。
- **デフォルト値**:
  - `MA1 Type` = SMA, `MA1 Length` = 10
  - `MA2 Type` = SMA, `MA2 Length` = 100
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 移動平均
  - ストップ: なし
  - 複雑さ: 基本
