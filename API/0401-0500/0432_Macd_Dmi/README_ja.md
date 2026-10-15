# MACD + DMI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Moving Average Convergence DivergenceとDirectional Movement Indexを組み合わせて、トレンドの強さが確認されたときのみ取引します。システムはMACDのクロスオーバーを待ち、優勢な方向線が反対の線を超え、ADXがキーレベルを上回っていることを確認します。

この戦略はロングおよびショートポジション向けに設計されています。モメンタムとトレンドフィルターを組み合わせることで、横ばい相場でのダマシを避けることを目指します。ボラティリティに基づく保護ストップがリスクを抑制します。

## 詳細

- **エントリー条件**:
  - **ロング**: MACD線がシグナル線を上にクロス、+DI > -DI、ADXがキーレベル以上。
  - **ショート**: MACD線がシグナル線を下にクロス、-DI > +DI、ADXがキーレベル以上。
- **エグジット条件**:
  - 逆シグナルまたはボラティリティストップ発動。
- **インジケーター**:
  - MACD (fast 12, slow 26, signal 9)
  - Directional Movement Index (length 14, ADX smoothing 14)
- **ストップ**: StartProtectionによる組み込みストップロスとテイクプロフィットを使用。
- **デフォルト値**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `DmiLength` = 14
  - `AdxSmoothing` = 14
  - `KeyLevel` = 20
- **フィルター**:
  - トレンドフォロー
  - 複数の時間軸で動作
  - インジケーター: MACD, DMI
  - ストップ: はい
  - 複雑さ: 中程度
