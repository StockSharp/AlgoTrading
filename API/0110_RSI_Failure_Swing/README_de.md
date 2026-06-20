# RSI Failure Swing Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
RSI Failure Swing ist eine klassische Umkehrtechnik, bei der der RSI ein höheres Tief im überverkauften Bereich oder ein niedrigeres Hoch im überkauften Bereich bildet.
Dieses Versagen, einen neuen Extremwert zu erreichen, geht oft einer Trendwende voraus.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 67%. Die Strategie funktioniert am besten am Aktienmarkt.

Die Strategie kauft, wenn der RSI über seinem vorherigen Tief bleibt und dann über 30 kreuzt, oder verkauft, wenn er ein vorheriges Hoch nicht überschreitet und unter 70 fällt.

Ein prozentualer Stop begrenzt den Nachteil, und Positionen werden geschlossen, wenn der RSI das entgegengesetzte Niveau kreuzt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

