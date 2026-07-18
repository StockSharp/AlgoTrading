# Drei-EMA-Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Three EMA Cross-Strategie kombiniert einen klassischen schnellen/langsamen
gleitenden Durchschnitt-Crossover mit einem längeren Trendfilter. Nachdem die schnelle
EMA die langsame EMA von unten kreuzt, wartet die Strategie auf einen Pullback zur
schnellen EMA, während der Schlusskurs über einer breiteren Trend-EMA verbleibt. Dieses
Setup versucht, Fortsetzungsbewegungen nach einer kurzen Korrektur innerhalb des
vorherrschenden Trends zu erfassen.

Positionen werden beendet, wenn das Momentum nachlässt und die schnelle EMA wieder
unter die langsame EMA fällt. Ein prozentualer Stop Loss schützt die Position, wenn
der Kurs gegen den Trade läuft. Die Technik funktioniert gut auf Märkten mit
anhaltenden Trends und vermeidet tendenziell choppy Ranges.

## Details

- **Einstiegskriterien**:
  - Aktueller schneller EMA-Cross über langsamer EMA innerhalb der letzten *N* Bars.
  - Aktueller Schlusskurs ≥ schnelle EMA und Sitzungstief ≤ schnelle EMA.
  - Trend-EMA ≤ aktueller Schlusskurs.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Schnelle EMA fällt unter langsame EMA.
- **Stops**: Stop Loss bei `stop_loss_percent` des Einstiegspreises.
- **Standardwerte**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 20
  - `TrendEmaLength` = 100
  - `StopLossPercent` = 2.0
  - `CrossBackBars` = 10
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
