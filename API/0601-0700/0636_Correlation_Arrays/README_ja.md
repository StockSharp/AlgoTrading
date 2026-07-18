# 相関配列戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は最大6つの銘柄のローリング相関行列を計算します。設定可能なしきい値を使用して相関レベルを記録し、資産間の関係を評価するのに役立てます。この戦略は分析専用であり、取引は実行されません。

## 詳細
- **エントリー条件**: なし（分析のみ）
- **ロング/ショート**: なし
- **エグジット条件**: なし
- **ストップ**: なし
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `LookbackPeriod` = 100
  - `PositiveWeak` = 0.3
  - `PositiveMedium` = 0.5
  - `PositiveStrong` = 0.7
  - `NegativeWeak` = -0.3
  - `NegativeMedium` = -0.5
  - `NegativeStrong` = -0.7
- **フィルター**:
  - カテゴリ: 統計分析
  - 方向: なし
  - インジケーター: 相関
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
