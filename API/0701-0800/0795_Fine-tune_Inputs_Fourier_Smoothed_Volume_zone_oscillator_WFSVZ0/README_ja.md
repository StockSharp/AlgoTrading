# Fourier平滑化ボリュームゾーンオシレーター WFSVZ0戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Fourier平滑化されたVolume Zone Oscillatorを使用した戦略です。オシレーターが閾値を上回ると買い、負の閾値を下回ると売りエントリーします。シグナルがない場合にオプションでポジションをクローズします。

## 詳細

- **エントリー条件**: オシレーターが閾値を上回る / 負の閾値を下回る。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナルまたはオプションの全決済。
- **ストップ**: なし。
- **デフォルト値**:
  - `VzoLength` = 2
  - `SmoothLength` = 2
  - `Threshold` = 0m
  - `CloseAllPositions` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 出来高
  - 方向: 両方
  - インジケーター: Volume Zone Oscillator
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
