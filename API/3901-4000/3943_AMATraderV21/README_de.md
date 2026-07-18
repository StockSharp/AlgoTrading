# AMA Trader v2.1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die AMA Trader v2.1-Strategie ist eine Konvertierung des MetaTrader 4 Expert Advisors **AMA_TRADER_v2_1.mq4**, der Kaufmans Adaptive Moving Average (AMA)-Bursts mit einem doppelt geglätteten Heiken Ashi-Filter und RSI Momentum-Checks kombiniert.

## Kernlogik

1. **Adaptiver Trendfilter** – Eine benutzerdefinierte AMA-Engine reproduziert den ursprünglichen Indikator, einschließlich der Schnell-/Langsam-Konstanten, des Effizienzverhältnisses und des Leistungsparameters. Der Algorithmus achtet auf Momentumausbrüche, bei denen der AMA-Wert im Vergleich zum vorherigen Balken um mehr als `AmaThreshold` Preisschritte ansteigt.
2. **Heiken Ashi-Bestätigung** – Preiskerzen werden zweimal geglättet: zuerst durch einen konfigurierbaren gleitenden Durchschnitt der rohen OHLC-Preise, dann durch einen zweiten gleitenden Durchschnitt der Heiken Ashi-Puffer. Ein bullischer (Schlusskurs über dem Eröffnungskurs) geglätteter Balken ermöglicht Long-Trades, während ein bärischer Balken Short-Trades zulässt.
3. **RSI Momentum Check** – Ein klassischer RSI mit konfigurierbarem Zeitraum bestätigt das Momentum: Long-Positionen erfordern, dass sich RSI von einem vorherigen Wert zurückzieht und dabei unter 70 bleibt, Short-Positionen erfordern einen Sprung, während der Oszillator über 30 bleibt.
4. **Positionsmanagement** – Die Strategie eröffnet jeweils eine einzelne Position, wendet optionale Stop-Loss- und Take-Profit-Abstände (in Preisschritten) an und kann den Stop nachziehen, sobald sich der Preis in die Handelsrichtung bewegt. Wenn RSI die 70/30-Extreme überschreitet, wird eine optionale teilweise Schließung durchgeführt, bevor bei der nächsten Kreuzung ein vollständiger Ausstieg erfolgt.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 15-Minuten-Kerzen | Zeitrahmen für alle Berechnungen. |
| `TradeVolume` | 0,1 | Basis-Market-Order-Volumen. |
| `AmaLength` | 9 | Vom adaptiven gleitenden Durchschnitt verwendeter Lookback. |
| `AmaFastPeriod` | 2 | Schnelle Konstante in Balken für die AMA-Glättung. |
| `AmaSlowPeriod` | 30 | Langsame Konstante in Balken für die AMA-Glättung. |
| `AmaPower` | 2 | Auf die Glättungskonstante angewendeter Exponent (entspricht `G` im MQ4-Code). |
| `AmaThreshold` | 2 Schritte | Minimale AMA-Änderung (in Preisschritten), um ein Signal auszulösen. |
| `FirstMaMethod` | Geglättet | Erste Glättungsmethode für die Heiken-Ashi-Konstruktion. |
| `FirstMaPeriod` | 6 | Länge des ersten gleitenden gleitenden Durchschnitts. |
| `SecondMaMethod` | LinearGewichtet | Zweite Glättungsmethode, die auf die Heiken Ashi-Puffer angewendet wird. |
| `SecondMaPeriod` | 2 | Länge des zweiten gleitenden gleitenden Durchschnitts. |
| `RsiPeriod` | 14 | RSI Zeitraum, der vom Impulsfilter verwendet wird. |
| `PartialClosePercent` | 70 % | Teil der aktiven Position, der geschlossen werden soll, wenn RSI ein Extremwert überschreitet. Zum Deaktivieren auf `0` setzen. |
| `StopLossSteps` | 50 | Stop-Loss-Abstand, ausgedrückt in Instrumentenpreisschritten. Zum Deaktivieren auf `0` setzen. |
| `TakeProfitSteps` | 100 | Take-Profit-Distanz, ausgedrückt in Preisschritten. Zum Deaktivieren auf `0` setzen. |
| `TrailingSteps` | 30 | Trailing-Stop-Distanz in Preisschritten. Auf `0` setzen, um das Nachstellen zu deaktivieren. |

## Handelsregeln

- **Long Entry** – Wenn der AMA-Sprung positiv ist und `AmaThreshold` überschreitet, ist die letzte geglättete Heiken Ashi-Kerze bullisch und RSI zieht sich zurück (vorheriger Wert größer als der aktuelle Wert), während er bei oder unter 70 bleibt.
- **Short Entry** – Wenn der AMA-Sprung über `AmaThreshold` hinaus negativ ist, ist die geglättete Heiken Ashi-Kerze bärisch und RSI steigt (vorheriger Wert kleiner als aktuell), während er bei oder über 30 bleibt.
- **Teilweise Schließung** – Wenn aktiviert, schließen Sie `PartialClosePercent` der Position, wenn RSI über 70 (Longs) oder unter 30 (Shorts) kreuzt.
- **Vollständiger Ausstieg** – Schließen Sie die gesamte Position am entgegengesetzten RSI-Extrem, bei Stop-Loss, Take-Profit oder wenn der Trailing-Stop erreicht wird.

Die Implementierung verwendet das übergeordnete StockSharp API: Ein Kerzenabonnement speist den benutzerdefinierten AMA-Rechner, die Heiken Ashi-Glättungspipeline und den Indikator RSI. Alle Kommentare im Quellcode sind in englischer Sprache und spiegeln die Anforderungen der Konvertierungsrichtlinien wider.
