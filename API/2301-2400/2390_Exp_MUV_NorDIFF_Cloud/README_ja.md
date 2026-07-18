# Exp MUV NorDIFF Cloud 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SMAとEMAの正規化モメンタムに基づく戦略。
SMAまたはEMAのモメンタムが+100に達したときにロングでエントリーし、-100に達したときにショートでエントリーします。

## パラメーター
- `MaPeriod` – 移動平均の期間。
- `MomentumPeriod` – モメンタム計算に使用するバー数。
- `KPeriod` – モメンタム極値の正規化ウィンドウ。
- `CandleType` – ローソク足の時間軸。

## 注記
この戦略はSMAとEMAの値を計算し、そのモメンタムを測定して直近の範囲内で正規化することで取引シグナルを生成します。
