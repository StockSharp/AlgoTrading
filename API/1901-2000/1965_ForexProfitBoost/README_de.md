# ForexProfitBoost-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **ForexProfitBoost**-Strategie ist ein reversal-basiertes Handelssystem, das einen schnellen Exponential Moving Average (EMA) und einen langsamen Simple Moving Average (SMA) kombiniert. Die Strategie wartet darauf, dass der schnelle EMA den langsamen SMA kreuzt, und handelt dann entgegen der Kreuzungsrichtung in Erwartung einer Preiskorrektur. Optionale Stop-Loss- und Take-Profit-Niveaus in absoluten Preispunkten können für das Risikomanagement konfiguriert werden.

## Indikatoren
- **EMA (schnell)**: Standardperiode 7.
- **SMA (langsam)**: Standardperiode 21.

## Handelsregeln
1. Abonnieren des ausgewählten Kerzen-Zeitrahmens.
2. Berechnung von EMA und SMA auf jeder abgeschlossenen Kerze.
3. Wenn der schnelle EMA den langsamen SMA **nach unten** kreuzt:
   - Short-Positionen schließen.
   - Neue Long-Position öffnen.
4. Wenn der schnelle EMA den langsamen SMA **nach oben** kreuzt:
   - Long-Positionen schließen.
   - Neue Short-Position öffnen.
5. Stop-Loss- und Take-Profit-Niveaus relativ zum Einstiegspreis anwenden, sofern angegeben.

## Parameter
| Name | Beschreibung | Standard |
|------|-------------|---------|
| `FastPeriod` | Periode für den schnellen EMA. | 7 |
| `SlowPeriod` | Periode für den langsamen SMA. | 21 |
| `StopLoss` | Stop-Loss-Abstand in Preispunkten. | 1000 |
| `TakeProfit` | Take-Profit-Abstand in Preispunkten. | 2000 |
| `CandleType` | Zeitrahmen für Berechnungen. | 1 Stunde |

## Hinweise
- Die Strategie verwendet die High-Level StockSharp API und speichert keine historischen Sammlungen.
- Trades werden ausschließlich mit Marktaufträgen nach Abschluss einer Kerze ausgeführt.
- Alle Kommentare im Quellcode sind auf Englisch verfasst, wie gefordert.
