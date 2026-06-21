# SMAトレンドフィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

3つの時間軸（15m、1h、4h）で5本の単純移動平均（期間5、8、13、21、34）の傾きを分析するマルチタイムフレーム戦略です。各時間軸の強気スコアと弱気スコアを計算し、すべての時間軸が一方向に一致したときに取引します。

## 詳細

- **エントリー条件**:
  - ロング: 3つの時間軸すべてで少なくとも50%のSMAが上昇している
  - ショート: 3つの時間軸すべてで少なくとも50%のSMAが下落している
- **ロング/ショート**: 両方
- **エグジット条件**: 終値レベルに基づく反対シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `OpenLevel` = 0
  - `CloseLevel` = 0
  - `CandleType1` = TimeSpan.FromMinutes(15).TimeFrame()
  - `CandleType2` = TimeSpan.FromHours(1).TimeFrame()
  - `CandleType3` = TimeSpan.FromHours(4).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: マルチタイムフレーム
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
