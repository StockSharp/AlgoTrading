# CCI Failure Swing Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Der CCI Failure Swing basiert auf dem Commodity Channel Index, der ein niedrigeres Hoch oberhalb von +100 oder ein höheres Tief unterhalb von -100 bildet.
Diese Unfähigkeit, ein neues Extrem zu erreichen, signalisiert oft das Ende des vorherigen Trends.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 73%. Er funktioniert am besten auf dem Kryptomarkt.

Die Strategie geht long, wenn der CCI über -100 bleibt und nach oben dreht, oder short, wenn er nahe +100 scheitert und nach unten dreht.

Ein prozentualer Stop hält das Risiko gering, und Trades werden beendet, wenn der CCI wieder durch das vorherige Swing-Level kreuzt.

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
  - Indikatoren: CCI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

