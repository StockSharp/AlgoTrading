# Strategie der Falschen Ausbruch-Falle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Falsche Ausbruch-Falle zielt darauf ab, von Ausbrüchen zu profitieren, die über wichtige Unterstützungs- oder Widerstandsniveaus hinaus nicht standhalten.
Trader springen oft in einen Ausbruch, nur um zu sehen, wie der Preis schnell umkehrt und sie in der Falle sitzen lässt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 52%. Die Strategie funktioniert am besten am Kryptomarkt.

Diese Strategie wartet auf dieses Scheitern und tritt in die entgegengesetzte Richtung ein, sobald der Preis wieder innerhalb der Range schließt.

Die Stop-Platzierung ist eng, knapp jenseits des gescheiterten Ausbruchniveaus, um sicherzustellen, dass Verluste gering bleiben, wenn die Umkehr ausbleibt.

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
  - Indikatoren: Price Action
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

