# Strategie Williams %R Hook Reversal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Williams %R Hook Reversal-Strategie folgt dem Williams %R-Indikator, wenn er schnell von einer Extremzone zurückschnappt. Wenn der Wert über -20 oder unter -80 steigt und dann zur Mitte hakt, ist der vorherige Schub wahrscheinlich erschöpft.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 172%. Die Strategie funktioniert am besten am Devisenmarkt.

Die Strategie kauft, wenn %R aus dem überverkauften Bereich nach oben dreht, während der Preis neue Tiefs drückt, und verkauft, wenn er aus dem überkauften Bereich nach unten hakt, während neue Hochs gebildet werden.

Ein enger prozentualer Stop kontrolliert das Risiko, und Trades werden beendet, sobald %R in die entgegengesetzte Richtung hakt oder der Stop ausgelöst wird.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 Minuten
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Williams %R
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
