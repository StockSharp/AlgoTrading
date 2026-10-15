# Color Schaff JCCX トレンドサイクル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MQL5エキスパート`Exp_ColorSchaffJCCXTrendCycle`のC#変換です。
JCCXアルゴリズムに基づいた**Schaff Trend Cycle (STC)**オシレーターを採用しています。

## トレードロジック

* 各完了ローソク足でSchaff Trend Cycleを計算する。
* オシレーターが`High Level`を上回った後に下回ると、ロングポジションを開きショートポジションを決済する。
* オシレーターが`Low Level`を下回った後に上回ると、ショートポジションを開きロングポジションを決済する。

## パラメーター

| 名前 | 説明 |
|------|-------------|
| Fast JCCX | インジケーターで使用する高速JCCX期間。 |
| Slow JCCX | インジケーターで使用する低速JCCX期間。 |
| Smoothing | JCCXのJJMA平滑化係数。 |
| Phase | JJMA位相値。 |
| Cycle | Schaff Trend計算のサイクル長。 |
| High Level | オシレーターの上側トリガーレベル。 |
| Low Level | オシレーターの下側トリガーレベル。 |
| Open Long | ロングポジションの開設を許可する。 |
| Open Short | ショートポジションの開設を許可する。 |
| Close Long | 既存のロングポジションの決済を許可する。 |
| Close Short | 既存のショートポジションの決済を許可する。 |

## 注記

この戦略はStockSharpの高レベルAPIを使用し、ローソク足データを購読します。**完了した**ローソク足にのみ反応します。資金管理とリスク管理はデモンストレーション目的でシンプルに保たれています。
