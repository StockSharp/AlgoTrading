# アルトコイン指数相関戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、取引対象商品と参照指数のEMAトレンドを比較します。両方の高速EMAが低速EMAを上回ったときにロングを開き、両方が下回ったときにショートを開きます。オプションの逆ロジックにより、指数トレンドに逆らって取引したり、指数を完全にスキップしたりできます。

## 詳細

- **エントリー条件**:
  - 両方の商品で高速EMAが低速EMAを上回る（逆の場合はその反対）。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 反対のクロスオーバー条件。
- **ストップ**: なし。
- **デフォルト値**:
  - `FastEmaLength` = 47
  - `SlowEmaLength` = 50
  - `IndexFastEmaLength` = 47
  - `IndexSlowEmaLength` = 50
  - `SkipIndexReference` = false
  - `InverseSignal` = false
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
