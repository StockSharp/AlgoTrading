# Strategie Bullish Harami
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Bullish Harami ist ein Zwei-Kerzen-Muster, bei dem ein kleiner Körper innerhalb des Bereichs der vorherigen bärischen Kerze liegt. Es deutet darauf hin, dass der Verkaufsschwung gestoppt hat und Käufer möglicherweise wieder einsteigen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 40%. Die Strategie funktioniert am besten am Kryptomarkt.

Diese Strategie steigt long ein, sobald die zweite Kerze innerhalb der ersten schließt, und erwartet eine Fortsetzung nach oben auf dem nächsten Balken.

Ein prozentualer Stop unterhalb des Musters bietet Schutz, und der Trade wird beendet, wenn der Preis wieder unter das Setup fällt.

## Details

- **Einstiegskriterien**: Mustererkennung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 Minuten
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
