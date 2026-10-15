# MESA Stochastic Multi Length Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet vier MESA Stochastic Oszillatoren mit unterschiedlichen Rückblicklängen. Eine Long-Position wird eröffnet, wenn alle vier Oszillatoren über ihrem gleitenden Durchschnitts-Trigger liegen. Eine Short-Position wird eröffnet, wenn alle vier Oszillatoren unter ihre Trigger fallen.

## Parameter
- `Length1` – Rückblick für den ersten Oszillator.
- `Length2` – Rückblick für den zweiten Oszillator.
- `Length3` – Rückblick für den dritten Oszillator.
- `Length4` – Rückblick für den vierten Oszillator.
- `TriggerLength` – Glättungsperiode für die Trigger-gleitenden Durchschnitte.
- `CandleType` – Kerzen-Zeitrahmen.
