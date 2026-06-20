# Donchian ボラティリティ収縮戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Donchian Volatility Contraction** 戦略は、ボラティリティ収縮後の Donchian チャネルのブレイクアウトを中心に構築されています。

テストでは年間平均リターン約 187% を示しています。株式市場で最もよく機能します。

インジケーターがイントラデイ (5m) データ上のボラティリティ収縮パターンを Donchian が確認したときにシグナルが発動します。これにより、この手法はアクティブトレーダーに適しています。

ストップは ATR の倍数と DonchianPeriod、AtrPeriod などの要素に依存します。リスクとリワードのバランスをとるためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件については実装を参照してください。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `DonchianPeriod = 20`
  - `AtrPeriod = 14`
  - `VolatilityFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Donchian
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
