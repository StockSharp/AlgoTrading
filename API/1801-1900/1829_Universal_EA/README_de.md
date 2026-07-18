# Universal EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie übersetzt aus MQL4 "Universal_EA".

Dieser Algorithmus verwendet den Stochastik-Oszillator zur Bestimmung von Einstiegspunkten.
Eine Long-Position wird eröffnet, wenn die %K-Linie die %D-Linie von unten kreuzt, während
beide unterhalb der Überverkauft-Schwelle liegen. Eine Short-Position wird eröffnet, wenn %K
die %D-Linie von oben kreuzt und beide oberhalb der Überkauft-Schwelle liegen. Signale werden
nur auf abgeschlossenen Kerzen geprüft und Positionen werden durch Marktaufträge eröffnet.

## Parameter
- **%K Period** – Basisperiode zur Berechnung von %K.
- **%D Period** – Glättungsperiode für die %D-Linie.
- **Slowing** – zusätzliche Glättung für %K.
- **Oversold** – Niveau, unterhalb dessen der Markt als überverkauft gilt.
- **Overbought** – Niveau, oberhalb dessen der Markt als überkauft gilt.
- **Candle Type** – Kerzen-Zeitrahmen oder -Typ für die Analyse.
