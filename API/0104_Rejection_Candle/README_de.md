# Rejection Candle Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Eine Rejection Candle bildet sich, wenn der Preis ein Niveau testet, aber nicht darüber hinaus halten kann, und dabei einen langen Docht und einen kleinen Körper hinterlässt.
Solche Kerzen zeigen an, dass ein Versuch, sich in eine Richtung zu bewegen, vom Markt entschieden abgelehnt wurde.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 49%. Die Strategie funktioniert am besten am Kryptomarkt.

Die Strategie tritt in die entgegengesetzte Richtung des Dochts ein, sobald die Kerze schließt, und erwartet, dass der Preis durch die Range zurückkehrt.

Stops werden außerhalb des abgelehnten Hochs oder Tiefs gesetzt, um das Risiko zu begrenzen, und Trades werden beendet, wenn der Impuls ausbleibt.

## Details

- **Einstiegskriterien**: Mustererkennung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
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

