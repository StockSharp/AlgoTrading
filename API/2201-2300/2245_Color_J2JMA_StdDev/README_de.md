# Color J2JMA StdDev-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet die Steigung eines Jurik Moving Average (JMA) und vergleicht sie mit der Standardabweichung der jüngsten Steigungen. Die Idee ist, starke gerichtete Bewegungen zu erfassen, wenn die Steigung ein Vielfaches ihrer jüngsten Volatilität überschreitet.

Eine neue Long-Position wird eröffnet, wenn die JMA-Steigung über den hohen Schwellenwert steigt (K2 × Standardabweichung). Eine neue Short-Position wird eröffnet, wenn die Steigung unter den negativen hohen Schwellenwert fällt. Bestehende Positionen werden geschlossen, wenn die Steigung den entgegengesetzten niedrigen Schwellenwert kreuzt (K1 × Standardabweichung). Stop-Loss- und Take-Profit-Niveaus werden in Punkten vom Einstiegspreis angewendet.

Parameter:
- **JMA Length** – Periode des Jurik Moving Average.
- **StdDev Period** – Anzahl der jüngsten Steigungen für die Standardabweichung.
- **K1** – Multiplikator für den niedrigen Schwellenwert zum Schließen von Positionen.
- **K2** – Multiplikator für den hohen Schwellenwert zum Öffnen von Positionen.
- **Candle Type** – Zeitrahmen der Kerzen für Berechnungen.
- **Stop Loss** – Schutz-Stop in Punkten.
- **Take Profit** – Gewinnziel in Punkten.
