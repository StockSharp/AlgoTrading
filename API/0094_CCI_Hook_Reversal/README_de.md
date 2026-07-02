# Strategie CCI Hook Reversal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die CCI Hook Reversal-Strategie verwendet den Commodity Channel Index als Auslöser, wenn er sich von einem extremen Wert wegbewegt. Nachdem der Indikator über +100 oder unter -100 gedrückt hat, schnellt er häufig schnell zurück, da der Schwung nachlässt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 169%. Die Strategie funktioniert am besten am Kryptomarkt.

Long-Trades treten auf, wenn CCI aus dem überverkauften Bereich nach oben dreht, während der Preis noch ein marginales neues Tief druckt. Shorts werden eingeleitet, wenn CCI aus dem überkauften Bereich dreht, während der Preis neue Hochs erreicht.

Jeder Trade trägt einen kleinen festen Stop und wird beendet, wenn der CCI in die entgegengesetzte Richtung hakt oder der Stop erreicht wird.

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
  - Indikatoren: CCI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
