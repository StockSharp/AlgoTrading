# Geo-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die kauft, wenn das Hoch-/Tief-Verhältnis der Kerze nahe dem goldenen Schnitt liegt.

## Details

- **Einstiegskriterien**: Hoch-/Tief-Verhältnis innerhalb der Toleranz von phi.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetzte Bedingung.
- **Stops**: Nein.
- **Standardwerte**:
  - `Tolerance` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candle ratio
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
