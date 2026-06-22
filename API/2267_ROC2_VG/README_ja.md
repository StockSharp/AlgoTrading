# ROC2 VG 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

StockSharpでMetaTraderの**Exp_ROC2_VG**エキスパートを再現します。  
設定可能な期間と計算タイプを持つ2本の変化率ラインを比較します。  
1本目のラインが2本目を下抜けするとロングポジションを開き、  
逆のクロスオーバーではショートポジションを開きます。`Invert`オプションでラインを入れ替えます。

## 詳細

- **ロングエントリー**: 前回の up > 前回の down かつ 現在の up <= 現在の down。
- **ショートエントリー**: 前回の up < 前回の down かつ 現在の up >= 現在の down。
- **エグジット**: 反転シグナルで成行注文を使ってポジションを即座に反転。
- **時間軸**: パラメーター化されたローソク足タイプ、デフォルトは4時間。
- **インジケーター**: 各ラインはMomentumまたはROCスタイルの計算を使用可能:
  - Momentum = `価格 - 前回価格`
  - ROC = `((価格 / 前回) - 1) * 100`
  - ROCP = `(価格 - 前回) / 前回`
  - ROCR = `価格 / 前回`
  - ROCR100 = `(価格 / 前回) * 100`
- **デフォルトパラメーター**:
  - `RocPeriod1` = 8, `RocType1` = Momentum
  - `RocPeriod2` = 14, `RocType2` = Momentum
  - `Invert` = false

シグナルが変化するとポジションサイズを反転させます。
