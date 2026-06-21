# Ultimate Stochastics 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はStochasticオシレーターのクロスオーバーを使って売買する。ロングとショートの両方が可能で、反対シグナルでのオプション決済と、パーセンテージベースの利益確定・損切りに対応する。

## 詳細

- **ロング**: 売られすぎゾーンで%Kが%Dを上抜ける。
- **ショート**: 買われすぎゾーンで%Kが%Dを下抜ける。
- **インジケーター**: Stochastic Oscillator。
- **ストップ**: オプションのパーセントTP/SL。
