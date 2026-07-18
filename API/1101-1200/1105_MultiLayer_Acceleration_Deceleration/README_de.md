# MultiLayer Acceleration/Deceleration-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie schichtet bis zu fünf Long-Einstiege mithilfe des Acceleration/Deceleration-Oszillators auf. Jedes Mal, wenn der Schwung in Richtung des durch Fraktale und die Alligator-Zähne identifizierten Trends zunimmt, wird eine Buy-Stop-Order über dem Hoch der Kerze platziert. Wenn der Oszillator schwächer wird oder der Trend dreht, werden alle ausstehenden Orders storniert und die Position geschlossen.

## Details

- **Einstiegskriterien**:
  - Aufwärtstrend bestätigt, wenn der Kurs ein Aufwärtsfraktal über den Alligator-Zähnen bricht.
  - AC-Oszillator zeigt ein grünes Balkenmuster und der Schlusskurs liegt über dem EMA-Filter.
  - Bis zu fünf Stop-Orders werden auf dem Aktivierungsniveau platziert.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Trend dreht nach unten.
  - Oszillator wird negativ.
- **Stops**: Verwendet fraktalbasierten Stop-Loss.
- **Standardwerte**:
  - `EMA Length` = 100.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Komplex
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
