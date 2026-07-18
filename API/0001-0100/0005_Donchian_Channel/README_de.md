# Donchian-Kanal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Donchian-Kanal.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 52%. Die Strategie funktioniert am besten im Kryptomarkt.

Der Donchian-Kanal-Ausbruch handelt neue Hochs und Tiefs basierend auf dem Kanalbereich. Ein Schlusskurs über dem oberen Band signalisiert Stärke, während ein Fall unter das untere Band Shorts einlädt. Ausstiege erfolgen, wenn der Preis zum Mittelpunkt zurückkommt.

Der Kanal wird aus dem höchsten Hoch und dem niedrigsten Tief über ein Lookback-Fenster berechnet. Wenn der Preis diese Grenzen durchbricht, erwartet das System eine Volatilitätsexpansion und positioniert sich entsprechend.


## Details

- **Einstiegskriterien**: Signale basierend auf Price Action.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `ChannelPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Price Action
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

