# Knux Multi-Indikator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert Trendstärke-Indikatoren und Momentum-Oszillatoren, um Ausbrüche zu handeln. Sie wartet auf einen bullischen oder bärischen Crossover zweier gleitender Durchschnitte, während der Average Directional Index (ADX) einen starken Trend signalisiert. Der Relative Vigor Index (RVI), der Commodity Channel Index (CCI) und Williams %R dienen als Filter, um sicherzustellen, dass das Momentum die Bewegung bestätigt und der Markt nicht überdehnt ist.

Das System kann sowohl Long- als auch Short-Positionen eröffnen. Es hält die Position, bis ein entgegengesetztes Signal erscheint, und verwendet keinen dedizierten Stop-Loss. Alle Parameter wie Indikatorperioden und Schwellenwerte sind konfigurierbar.

## Details

- **Einstiegskriterien**:
  - **Long**: Schneller SMA kreuzt oberhalb des langsamen SMA, `ADX > AdxLevel`, `RVI` steigend, `CCI < -CciLevel`, und `WPR <= -100 + WprBuyRange`.
  - **Short**: Schneller SMA kreuzt unterhalb des langsamen SMA, `ADX > AdxLevel`, `RVI` fallend, `CCI > CciLevel`, und `WPR >= -WprSellRange`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal (Crossover in die andere Richtung).
- **Stops**: Kein expliziter Stop-Loss.
- **Standardwerte**:
  - `FastMaLength` = 5
  - `SlowMaLength` = 20
  - `AdxPeriod` = 14
  - `AdxLevel` = 15
  - `RviPeriod` = 20
  - `CciPeriod` = 40
  - `CciLevel` = 150
  - `WprPeriod` = 60
  - `WprBuyRange` = 15
  - `WprSellRange` = 15
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Keine
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
