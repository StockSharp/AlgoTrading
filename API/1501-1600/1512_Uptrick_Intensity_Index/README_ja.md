# Uptrick Intensity Index戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は3本の移動平均からトレンド強度指数（TII）を算出し、TIIと自身の移動平均のクロスオーバーで売買する。

## 詳細

- **エントリー条件**: TIIがSMAを上抜け（買い）または下抜け（売り）
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `Ma3Length` = 50
  - `TiiMaLength` = 50
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA, TII
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
