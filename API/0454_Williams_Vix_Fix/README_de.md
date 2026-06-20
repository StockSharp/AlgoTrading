# Williams VIX Fix-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Williams VIX Fix-Strategie passt Larry Williams' Volatilitätsindikator für
Instrumente an, denen ein veröffentlichter VIX fehlt. Sie berechnet einen synthetischen
VIX-Wert anhand der Distanz zwischen dem höchsten Schlusskurs über einen Rückblickzeitraum
und dem aktuellen Tief. Wenn dieser Wert über einen Bollinger-Band-Schwellenwert ansteigt
oder der Kurs unter die untere Bollinger Band schließt, betrachtet die Strategie dies als
überverkaufte Gelegenheit. Eine umgekehrte Berechnung misst überkaufte Extreme.

Der Ansatz sucht nach Mean Reversion nach Volatilitätsspitzen. Wenn der VIX Fix hohe
Angst signalisiert und der Kurs unter der unteren Band liegt, wird eine Long-Position
eröffnet. Umgekehrt, wenn der inverse VIX Fix auf extreme Selbstgefälligkeit hinweist
und der Kurs über der oberen Band liegt, werden bestehende Long-Positionen geschlossen.
Perzentil-Schwellenwerte kontrollieren die Empfindlichkeit.

## Details

- **Einstiegskriterien**:
  - VIX Fix ≥ obere Band oder Perzentil und Kurs < untere Bollinger Band.
- **Long/Short**: Long-Einstiege mit Ausstiegen bei entgegengesetztem Signal.
- **Ausstiegskriterien**:
  - Invertierter VIX Fix ≥ obere Band oder Perzentil und Kurs > obere Bollinger Band.
- **Stops**: Keine.
- **Standardwerte**:
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `WvfPeriod` = 20
  - `WvfLookback` = 50
  - `HighestPercentile` = 0.85
  - `LowestPercentile` = 0.99
- **Filter**:
  - Kategorie: Volatilitäts-Mean Reversion
  - Richtung: Long
  - Indikatoren: Bollinger Bands, Williams VIX Fix
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
