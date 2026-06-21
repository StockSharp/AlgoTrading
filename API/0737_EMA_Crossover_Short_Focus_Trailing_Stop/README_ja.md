# EMAクロスオーバー・ショート重視・トレーリングストップ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、13 EMAが33 EMAを上回りショートポジションが存在しない場合にロングポジションを取ります。13 EMAが33 EMAを下回りロングポジションが開かれていない場合はショートポジションを取ります。13 EMAが逆のEMAをクロスするとポジションは決済され、トレーリングストップが直近の極値に従います。

## 詳細
- **エントリー条件:**
  - **ロング:** 13 EMA ≥ 33 EMA かつポジション ≤ 0。
  - **ショート:** 13 EMA ≤ 33 EMA かつポジション ≥ 0。
- **ロング/ショート:** 両方。
- **エグジット条件:** ロングは 13 EMA < 33 EMA で決済; ショートは 13 EMA > 25 EMA で決済。
- **ストップ:** `TrailDistance` の距離と `TrailOffset` のオフセットを持つトレーリングストップ。
- **デフォルト値:** short EMA = 13、mid EMA = 25、long EMA = 33、trail distance = 10、trail offset = 2。
