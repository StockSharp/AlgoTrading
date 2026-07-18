# Gann + Laplace-geglätteter Volumenzonenoszillator Feinabstimmungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen durch exponentielle gleitende Durchschnitte geglätteten Volumenoszillator.
Eine Long-Position wird eröffnet, wenn der geglättete Oszillator über den Schwellenwert steigt.
Eine Short-Position wird eröffnet, wenn er unter den negativen Schwellenwert fällt.
Wenn Signale verschwinden und **Close All** aktiviert ist, wird jede offene Position geschlossen.

## Parameter
- **Fast Volume EMA** – Periode für den schnellen Volumendurchschnitt.
- **Slow Volume EMA** – Periode für den langsamen Volumendurchschnitt.
- **Smooth Length** – Glättungsperiode für den Oszillator.
- **Threshold** – Signalniveau für Einstiege.
- **Close All** – Position schließen, wenn kein Signal vorhanden.
- **Candle Type** – Kerzentyp für Berechnungen.
