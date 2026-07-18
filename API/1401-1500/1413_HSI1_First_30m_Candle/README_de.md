# HSI1 Erste-30m-Kerze-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt Ausbrüche aus der ersten 30-Minuten-Range auf einem 15-Minuten-Chart, wobei nur ein Trade pro Tag erlaubt ist.

## Details

- **Einstiegskriterien**: Der Preis bricht über/unter das Hoch/Tief der ersten 30 Minuten während der Sitzung.
- **Long/Short**: Beide Richtungen, wählbar.
- **Ausstiegskriterien**: Take Profit oder Stop Loss basierend auf der Range.
- **Stops**: Ja.
- **Standardwerte**:
  - `RiskReward` = 1
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Preis
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
