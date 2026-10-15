# Color Bears Gap-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert eine Strategie basierend auf dem Color Bears Gap-Indikator. Der Indikator vergleicht zwei geglättete Lücken zwischen dem Hochpreis und geglätteten Eröffnungs-/Schlusswerten. Wenn die Differenz die Nulllinie kreuzt, werden Positionen in der neuen Richtung geöffnet und entgegengesetzte Positionen geschlossen.

## Details
- **Einstiegskriterien**: Indikator kreuzt unter null -> kaufen; kreuzt über null -> verkaufen.
- **Long/Short**: Über Parameter konfigurierbar.
- **Ausstiegskriterien**: Entgegengesetzte Nulllinienkreuzung.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length1` = 12
  - `Length2` = 5
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = 8-Stunden-Zeitrahmen
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Color Bears Gap
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: 8-stündig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
