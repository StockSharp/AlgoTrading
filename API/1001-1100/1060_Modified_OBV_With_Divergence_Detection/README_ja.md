# ダイバージェンス検出付き改良OBV戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は選択可能な移動平均でOn-Balance Volume（OBV）を平滑化し、シグナルラインを生成します。平滑化されたOBVがシグナルと交差したときに取引が発生します。さらに、フラクタル検出を使用して価格とOBV間の通常ダイバージェンスおよび隠れダイバージェンスを記録します。

## 詳細

- **エントリー条件**: OBV-Mがシグナルラインを上方/下方にクロスしたとき。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆方向のクロスオーバー。
- **ストップ**: なし。
- **デフォルト値**:
  - `MaType` = Exponential
  - `ObvMaLength` = 7
  - `SignalLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: ダイバージェンス
  - 方向: 両方
  - インジケーター: OBV、MA
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい
  - リスクレベル: 中
