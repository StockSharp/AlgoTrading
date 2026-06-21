# Lux Clara EMA + VWAP-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Lux Clara EMA + VWAP-Strategie handelt den Crossover einer schnellen und einer langsamen EMA, gefiltert durch VWAP und ein Zeitfenster. Eine Long-Position wird eröffnet, wenn die schnelle EMA die langsame EMA von unten kreuzt, während die langsame EMA über dem VWAP liegt und die aktuelle Zeit innerhalb der Sitzung liegt. Eine Short-Position wird unter umgekehrten Bedingungen eröffnet. Positionen werden geschlossen, wenn die EMAs in die entgegengesetzte Richtung kreuzen.

## Details

- **Einstiegskriterien**:
  - Schnelle EMA kreuzt die langsame EMA von unten, langsame EMA über VWAP und aktuelle Zeit innerhalb der Sitzung.
  - Short: Schnelle EMA kreuzt die langsame EMA von oben, langsame EMA unter VWAP und aktuelle Zeit innerhalb der Sitzung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Entgegengesetzter EMA-Crossover.
- **Stops**: Keine.
- **Standardwerte**:
  - `FastEmaLength` = 8
  - `SlowEmaLength` = 50
  - `StartTime` = 07:30
  - `EndTime` = 14:30
  - `CandleType` = 5 Minuten
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long und Short
  - Indikatoren: EMA, VWAP
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
