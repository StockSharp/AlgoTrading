# Gap Fill Reversal Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Gap Fill Reversal-Strategie nutzt Overnight-Gaps, die in der nächsten Sitzung schnell zurückverfolgt werden. Wenn der Preis vom vorherigen Schlusskurs aufgappt, aber sofort zurückkehrt, um diesen Leerraum zu füllen, signalisiert dies oft eine Erschöpfung der anfänglichen Bewegung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 181%. Die Strategie funktioniert am besten am Kryptomarkt.

Die Strategie steigt ein, sobald das Gap vollständig geschlossen ist, und sucht nach einer Umkehr in die entgegengesetzte Richtung der Eröffnung. Sie zielt darauf ab, den Rückprall zu erfassen, der auftritt, wenn gefangene Trader ihre Positionen schließen.

Ein prozentualer Stop definiert das Risiko, und Positionen schließen, wenn der Schwung nachlässt oder der Stop ausgelöst wird.

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
  - Indikatoren: Gap
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
