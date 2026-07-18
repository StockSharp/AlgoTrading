# Outside Bar Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Outside Bar entsteht, wenn die Handelsspanne einer Kerze die der vorherigen überschreitet und damit einen kurzen Volatilitätsschub erzeugt. Diese Strategie handelt gegen die Bewegung, wenn die Outside Bar in entgegengesetzter Richtung zum vorherigen Trend schließt, und erwartet eine Rückkehr zum Gleichgewicht.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 121%. Die Strategie eignet sich am besten für den Kryptomarkt.

Wenn eine Outside Bar entsteht, bestimmt der Algorithmus, ob die Kerze bullisch oder bearisch ist. Eine bullische Outside Bar nach einem Rückgang eröffnet eine Long-Position mit einem Stop unterhalb des Balken-Tiefs. Eine bearische Outside Bar nach einer Rally löst einen Short mit einem Stop oberhalb ihres Hochs aus. Trades werden beendet, wenn der Kurs anschließend durch diesen Extrempunkt bricht.

Das Setup sucht schnelle Umkehrungen nach einem erschöpfenden Impuls und eignet sich am besten in volatilen Märkten statt in stark trendenden.

## Details

- **Einstiegskriterien**: Outside Bar schließt entgegengesetzt der vorherigen Bewegung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Kurs bricht Outside Bar Hoch/Tief oder Stop-Loss.
- **Stops**: Ja, außerhalb des Musters platziert.
- **Standardwerte**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
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

