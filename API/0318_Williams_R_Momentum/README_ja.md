# Williams %R モメンタム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Williams R Momentum** 戦略は、Williams %R とモメンタムフィルターを中心に構築されています。

インジケーターがイントラデイ (5m) データ上のモメンタムの転換を Williams が確認したときにシグナルが発動します。これにより、この手法はアクティブトレーダーに適しています。

ストップは ATR の倍数と WilliamsRPeriod、MomentumPeriod などの要素に依存します。リスクとリワードのバランスをとるためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件については実装を参照してください。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `WilliamsRPeriod = 14`
  - `MomentumPeriod = 14`
  - `WilliamsROversold = -80m`
  - `WilliamsROverbought = -20m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Williams %R
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
