# Exp BrainTrend2 AbsolutelyNoLagLwma X2MACandle MMRec-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie bildet den Multi-Modul-MetaTrader-Expertenberater nach, indem drei Filter in der High-Level-API von StockSharp kombiniert werden:

1. **BrainTrend2-Inspiration** – ein Average True Range (ATR)-Kanal erkennt Volatilitätskontraktions- und -expansionsphasen.
2. **AbsolutelyNoLagLwma-Approximation** – ein linear gewichteter gleitender Durchschnitt (LWMA) verfolgt die dominante Richtung mit minimalem Lag.
3. **X2MACandle-Replik** – ein Paar aus schnellem und langsamem exponentiellen gleitenden Durchschnitt (EMA) bewertet die Kerzenfarbe zur Momentum-Validierung.

Eine Position wird nur dann eröffnet, wenn alle Filter in dieselbe Richtung zeigen. ATR-gesteuerte Stop-Loss- und Take-Profit-Ziele verwalten den Ausstiegsprozess und emulieren das originale MMRec-Geldverwaltungskonzept.

## Handelslogik
- **Bullisches Setup**: Die Kerze schließt über dem LWMA, während die schnelle EMA höher als die langsame EMA ist. Ein neuer Long-Einstieg ist nur erlaubt, nachdem die vorherige bullische Tendenz verschwunden ist, um mehrere Orders bei identischen Signalen zu vermeiden.
- **Bärisches Setup**: Die Kerze schließt unter dem LWMA, während die schnelle EMA niedriger als die langsame EMA ist. Short-Positionen gehorchen denselben Bestätigungs- und Abkühlregeln wie die Long-Seite.
- **Risikomanagement**: ATR definiert dynamische Ausstiegsniveaus. Sowohl Stop-Loss als auch Take-Profit skalieren mit der aktuellen Volatilität und werden bei jeder Kerze neu bewertet. Wenn der Markt eines der Niveaus berührt, schließt die Strategie die gesamte Position mit einer Marktorder.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen der arbeitenden Kerzenserie. Standardmäßig 6-Stunden-Kerzen, um die ursprünglichen EA-Standards zu spiegeln. |
| `AtrPeriod` | Lookback-Periode des ATR-Volatilitätsfilters. |
| `LwmaLength` | Periode des linear gewichteten gleitenden Durchschnitts als Trendfilter. |
| `FastMaLength` | Periode des schnellen EMA für die Kerzen-Färbung. |
| `SlowMaLength` | Periode des langsamen EMA für die Kerzen-Färbung. |
| `StopLossAtrMultiplier` | Multiplikator auf ATR zur Berechnung des Schutz-Stop-Abstands. |
| `TakeProfitAtrMultiplier` | Multiplikator auf ATR zur Bestimmung des Take-Profit-Abstands. |

Alle Parameter sind über `StrategyParam<T>` zugänglich, sodass sie innerhalb von StockSharp optimiert werden können.

## Hinweise
- Der ursprüngliche Expertenberater verlässt sich auf proprietäre Indikator-Puffer. Dieser Port verwendet Standard-StockSharp-Indikatoren, die dieselben Richtungshinweise reproduzieren, ohne auf externe Skripte angewiesen zu sein.
- Geldverwaltung wird auf vollständige Positions-Ausstiege vereinfacht, da StockSharp-Strategien typischerweise mit Portfolio-großen Orders arbeiten. Die ATR-gesteuerten Abstände liefern das adaptive Verhalten, das vom MMRec-Modul erwartet wird.
- Kommentare im Code sind auf Englisch, wie von den Konvertierungsrichtlinien vorgeschrieben.
