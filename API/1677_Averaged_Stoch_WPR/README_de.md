# Gemittelte Stoch & WPR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den Stochastic-Oszillator mit Williams %R, um extreme Marktbedingungen zu erkennen.
Eine Long-Position wird eröffnet, wenn der Stochastic-Wert unter 0.1 fällt und Williams %R unter -90 liegt, was starken Überverkaufsdruck signalisiert.
Eine Short-Position wird eröffnet, wenn der Stochastic über 99.9 steigt und Williams %R -5 überschreitet, was starke Überkauftbedingungen anzeigt.

Die Strategie funktioniert mit jedem Instrument und Zeitrahmen, der vom gewählten Kerzentyp unterstützt wird. Sie kann sowohl Long- als auch Short-Positionen handeln und bietet einen optionalen prozentualen Stop-Loss für das Risikomanagement.

## Details

- **Einstiegskriterien**:
  - **Long**: Stochastic < 0.1 und Williams %R < -90.
  - **Short**: Stochastic > 99.9 und Williams %R > -5.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal oder ausgelöster Stop-Loss.
- **Stops**: Optionaler prozentualer Stop-Loss.
- **Indikatoren**:
  - Stochastic-Oszillator (Standardperiode 26).
  - Williams %R (Standardperiode 26).

## Parameter

- `StochPeriod` – Berechnungsperiode des Stochastic.
- `WprPeriod` – Berechnungsperiode von Williams %R.
- `StopLossPercent` – Größe des prozentualen Stop-Loss.
- `CandleType` – Kerzentyp für Indikatorberechnungen.
