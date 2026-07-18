# Strategie "Öffnen und Schließen zur Tageszeit v2"
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine zeitbasierte Strategie, die Trades zu einer bestimmten Zeit öffnet und sie später am Tag schließt. Die Handelsrichtung wird durch den Vergleich eines schnellen und eines langsamen exponentiellen gleitenden Durchschnitts bestätigt. Stop-Loss- und Take-Profit-Niveaus werden in Ticks ausgedrückt.

## Details

- **Einstiegskriterien**: Zum Zeitpunkt `OpenTime` Long gehen, wenn der schnelle EMA über dem langsamen EMA liegt; Short gehen, wenn er darunter liegt. Die Richtung hängt von `TradeMode` ab.
- **Long/Short**: Konfigurierbar (kaufen, verkaufen oder beides).
- **Ausstiegskriterien**: Positionen werden zum Zeitpunkt `CloseTime` oder durch Schutz-Stops geschlossen.
- **Stops**: Ja, sowohl Stop-Loss als auch Take-Profit in Ticks.
- **Standardwerte**:
  - `OpenTime` = 05:00
  - `CloseTime` = 21:01
  - `SlowPeriod` = 200
  - `FastPeriod` = 50
  - `StopLossTicks` = 30
  - `TakeProfitTicks` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Zeitbasiert
  - Richtung: Konfigurierbar
  - Indikatoren: EMA
  - Stops: Fest
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
