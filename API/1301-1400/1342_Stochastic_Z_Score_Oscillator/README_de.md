# Stochastic Z-Score-Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert einen skalierten Stochastic-Oszillator mit einem Preis-Z-Score. Ein Trade wird geöffnet, wenn ihr Durchschnitt einen Schwellenwert kreuzt, und geschlossen, wenn der Z-Score auf null zurückkehrt. Abkühlzähler verhindern häufige Signale.

## Details

- **Einstiegskriterien**: Durchschnitt aus skaliertem Stochastic %K und Preis-Z-Score kreuzt nach Abkühlzeit den Schwellenwert nach oben/unten
- **Long/Short**: Beide
- **Ausstiegskriterien**: Z-Score kreuzt null
- **Stops**: Nein
- **Standardwerte**:
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `StochLength` = 14
  - `StochSmooth` = 7
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Stochastic, SMA, StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
