# ゴールド・スキャルピング BOS & CHoCH 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はゴールドにおける構造ブレイク（BOS）とキャラクター変化（CHoCH）のパターンを取引します。短期的なサポートとレジスタンスレベルを導出し、BOSの直後にCHoCHが発生したときにエントリーし、動的なストップロスとテイクプロフィットを使用します。

## 詳細

- **エントリー条件**:
  - **ロング**: `High > LastSwingHigh` かつ `Close` が `LastSwingLow` を上抜け
  - **ショート**: `Low < LastSwingLow` かつ `Close` が `LastSwingHigh` を下抜け
- **ロング/ショート**: 両方向
- **エグジット条件**: ストップロスまたはテイクプロフィット
- **ストップ**: ダイナミック
- **デフォルト値**:
  - `RecentLength` = 10
  - `SwingLength` = 5
  - `TakeProfitFactor` = 2
- **フィルター**:
  - カテゴリ: スキャルピング
  - 方向: 両方
  - インジケーター: Highest, Lowest
  - ストップ: はい
  - 複雑さ: 中
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
