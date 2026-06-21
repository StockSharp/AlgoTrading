# Stochastic エグジットアラート戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Stochasticの%K線が売られすぎゾーンで%Dを上抜けたときにロングエントリーし、買われすぎゾーンで%Kが%Dを下抜けたときにショートエントリーします。ポジションは固定のストップロスとティック単位のテイクプロフィットで保護されます。極端なゾーン外で逆方向のクロスオーバーが発生した場合、ポジションは転換せずにクローズされます。

## パラメーター
- `StochLength` – Stochasticオシレーターのメイン期間。
- `KLength` – %Kラインの平滑化期間。
- `DLength` – %Dラインの平滑化期間。
- `StopLossTicks` – ストップロスのティック距離。
- `TakeProfitTicks` – テイクプロフィットのティック距離。
- `CandleType` – ロウソク足の時間軸。
