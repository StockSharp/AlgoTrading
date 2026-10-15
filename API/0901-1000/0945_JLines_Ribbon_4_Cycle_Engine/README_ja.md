# J-Lines リボン 4サイクルエンジン戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

J-Lines Ribbon 4-Cycle Engine戦略は、EMAのリボンとAverage Directional Indexを使用して市場をCHOP、LONG、SHORTサイクルに分類します。エントリーは新しいサイクルの検出とキーEMAからのリバウンドで発生し、エグジットは逆方向のクロスまたはスイングブレイクでトリガーされます。

## 詳細

- **エントリー条件**:
  - **ロング**: 新しいLONGサイクル、またはEMA72がEMA89より上にある状態でEMA72/EMA126の上でリバウンド。
  - **ショート**: 新しいSHORTサイクル、またはEMA72がEMA89より下にある状態でEMA72/EMA126の下でリバウンド。
- **ストップ**: 直近のスイング高値/安値。
- **デフォルト値**:
  - `DmiLength` = 8
  - `AdxFloor` = 12
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, ADX
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
