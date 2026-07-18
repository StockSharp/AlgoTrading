# MAMACD Keine Volatilitätsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
MAMACD No Volatility ist eine direkte Portierung des MetaTrader 4 Expertenberaters `MAMACD_novlt.mq4`. Die Strategie kombiniert drei gleitende Durchschnitte, die anhand von Kerzentiefs und -schlüssen berechnet werden, mit einem MACD-Momentumfilter. Es wartet, bis der schnelle EMA unter (für Long-Positionen) fällt oder über (für Short-Positionen) zwei niedrigbasierte LWMA-Filter steigt, aktiviert ein ausstehendes Setup und löst einen Einstieg erst aus, nachdem die MACD-Hauptlinie die Momentumverschiebung bestätigt.

## Indikatoren
- **Schneller EMA** (`FastEmaPeriod`), berechnet auf Basis der Schlusskurse.
- **Erster LWMA** (`FirstLowWmaPeriod`), berechnet zu niedrigen Preisen.
- **Zweiter LWMA** (`SecondLowWmaPeriod`), berechnet zu niedrigen Preisen.
- **MACD Hauptzeile** mit schneller Periode `FastSignalEmaPeriod` und langsamer Periode `SlowEmaPeriod`.

Alle Indikatoren arbeiten in dem durch `CandleType` definierten Zeitrahmen (Standard: 5-Minuten-Kerzen).

## Parameter
| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `FirstLowWmaPeriod` | Zeitraum des ersten LWMA, der aus Kerzentiefs aufgebaut wurde. | 85 |
| `SecondLowWmaPeriod` | Periode des zweiten LWMA, aufgebaut aus Kerzentiefs. | 75 |
| `FastEmaPeriod` | Zeitraum des schnellen EMA, der ab Kerzenschluss aufgebaut wird. | 5 |
| `SlowEmaPeriod` | Langsamer Zeitraum von EMA für die Berechnung von MACD. | 26 |
| `FastSignalEmaPeriod` | Schneller Zeitraum von EMA für die Berechnung von MACD. | 15 |
| `StopLossPoints` | Stop-Loss-Distanz in Preisschritten (0 deaktiviert den Stop-Loss). | 15 |
| `TakeProfitPoints` | Take-Profit-Distanz in Preisschritten (0 deaktiviert den Take-Profit). | 15 |
| `TradeVolume` | Für Markteintritte verwendetes Ordervolumen. | 0,1 |
| `CandleType` | Für alle Indikatoren verwendete Kerzenserie. | Zeitrahmen von 5 Minuten |

## Handelsregeln
1. **Arm-Long-Setup**: Fast EMA liegt unter beiden LWMA-Filtern.
2. **Arm-Short-Setup**: Fast EMA liegt über beiden LWMA-Filtern.
3. **Geben Sie lang ein**:
   - Schneller EMA kreuzt zurück über beide LWMAs,
   - Eine lange Anlage wurde zuvor scharfgeschaltet,
   - MACD Hauptlinie ist positiv oder hat im Vergleich zum vorherigen Wert zugenommen,
   - Die aktuelle Nettoposition ist nicht lang.
4. **Kurz eingeben**:
   - Schnelles EMA kreuzt wieder unter beide LWMAs,
   - Ein kurzes Setup wurde zuvor scharfgeschaltet,
   - Die Hauptlinie von MACD ist negativ oder hat im Vergleich zum vorherigen Wert abgenommen.
   - Die aktuelle Nettoposition ist nicht short.
5. **Risikomanagement**: Optionale Take-Profit- und Stop-Loss-Werte werden durch den integrierten Schutzdienst automatisch angewendet.

Die Strategie implementiert kein dediziertes Ausstiegssignal; Positionen werden durch die konfigurierten Stop-Loss-/Take-Profit-Levels oder manuelle Eingriffe verwaltet.

## Notizen
- Die MACD-Bestätigung repliziert die MQL-Logik: Die Hauptlinie muss entweder über Null liegen oder steigen (für Long-Positionen) oder unter Null liegen oder fallen (für Short-Positionen).
- Die LWMA-Berechnungen verwenden Kerzentiefpreise, um die ursprüngliche Indikatorkonfiguration widerzuspiegeln.
- Die Volumenskalierung spiegelt den ursprünglichen EA wider, indem für jede Bestellung der Parameter `TradeVolume` verwendet wird.
