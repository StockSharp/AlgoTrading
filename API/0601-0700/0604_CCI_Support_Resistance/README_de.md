# CCI-Unterstützung-Widerstand-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet CCI-Pivots, um dynamische Unterstützungs- und Widerstandsniveaus aufzubauen. Vor dem Handel von Ausbrüchen dieser Niveaus wird ein Trendfilter auf Basis eines EMA-Kreuzes oder -Gefälles angewendet.

## Details

- **Einstiegskriterien**:
  - Long: Preis schließt nach Berührung über dem CCI-basierten Unterstützungsniveau und der Trend ist bullisch.
  - Short: Preis schließt nach Berührung unter dem CCI-basierten Widerstandsniveau und der Trend ist bärisch.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - ATR-basierter Stop-Loss und Take-Profit.
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `CciLength` = 50
  - `LeftPivot` = 50
  - `RightPivot` = 50
  - `Buffer` = 10
  - `TrendMatter` = true
  - `TrendType` = Cross
  - `SlowMaLength` = 100
  - `FastMaLength` = 50
  - `SlopeLength` = 5
  - `Ksl` = 1.1
  - `Ktp` = 2.2
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: CCI, EMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
