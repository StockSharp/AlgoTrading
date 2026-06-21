# Serien-basierte Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verfolgt aufeinanderfolgende Gewinn- und Verlustkerzen. Nachdem die angegebene Serie erreicht ist, tritt die Strategie in die entgegengesetzte Richtung ein und hält die Position für eine feste Anzahl von Kerzen. Doji-Kerzen werden basierend auf der Körpergröße ignoriert.

## Details

- **Einstiegskriterien**: Gegenseite nach Erreichen der Gewinn-/Verlustserie.
- **Long/Short**: Konfigurierbar (`TradeDirection`).
- **Ausstiegskriterien**: Nach `HoldDuration` Kerzen.
- **Stops**: Nein.
- **Standardwerte**:
  - `TradeDirection` = Long
  - `StreakThreshold` = 8
  - `HoldDuration` = 7
  - `DojiThreshold` = 0.01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Konfigurierbar
  - Indikatoren: Price Action
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
