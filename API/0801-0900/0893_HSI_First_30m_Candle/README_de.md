# HSI Erste-30m-Kerze-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erfasst das Hoch und Tief der ersten 30 Minuten nach Eröffnung der Hongkonger Sitzung und handelt Ausbrüche auf einem 5-Minuten-Chart. Pro Tag ist nur ein Trade erlaubt.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurs bricht über das 30-Minuten-Hoch während der Sitzung aus.
  - **Short**: Kurs fällt unter das 30-Minuten-Tief während der Sitzung.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Stop-Loss auf der gegenüberliegenden Seite der Range.
  - Take-Profit bei Range-Größe multipliziert mit `RiskReward` vom Einstieg.
- **Stops**: Ja.
- **Standardwerte**:
  - `RiskReward` = 1.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Price action
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
