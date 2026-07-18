# BykovTrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert das klassische MetaTrader-System "Bykov Trend" mit der StockSharp High-Level-API. Der ursprüngliche Indikator kombiniert den Williams %R-Oszillator mit einem einfachen Trenderkennungsmechanismus. Wenn der Trend von bärisch zu bullisch wechselt, wird eine Long-Position eröffnet. Wenn der Trend von bullisch zu bärisch wechselt, wird eine Short-Position eröffnet.

Das System handelt ein einzelnes Instrument auf einem ausgewählten Zeitrahmen. Es wird immer nur eine Position gehalten; entgegengesetzte Signale kehren die Position um.

## Details

- **Einstiegskriterien**  
  - **Long**: Williams %R steigt über `-K`, nachdem er unter `-100 + K` war (`K = 33 - Risk`).  
  - **Short**: Williams %R fällt unter `-100 + K`, nachdem er über `-K` war.
- **Long/Short**: Beide Richtungen.  
- **Ausstiegskriterien**  
  - Das entgegengesetzte Signal schließt die aktuelle Position und eröffnet eine neue in der anderen Richtung.  
- **Stops**: Keine.  
- **Standardwerte**  
  - `Risk` = 3 (`K = 30`).  
  - `SSP` = 9 (Williams %R-Rückblick).  
  - `CandleType` = 1-Stunden-Kerzen.  
- **Filter**  
  - Kategorie: Trendfolge  
  - Richtung: Beide  
  - Indikatoren: Einzeln (Williams %R)  
  - Stops: Nein  
  - Komplexität: Einfach  
  - Zeitrahmen: Flexibel  
  - Saisonalität: Nein  
  - Neuronale Netze: Nein  
  - Divergenz: Nein  
  - Risikolevel: Mittel
