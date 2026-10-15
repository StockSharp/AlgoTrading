# Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis einfacher gleitender Durchschnitte der Kerzen-Eröffnungspreise. Sie vergleicht den letzten 5-Minuten-Schlusskurs mit zwei gleitenden Durchschnitten, die auf dem Eröffnungspreis berechnet werden:

- **Schneller SMA (4 Perioden)** – wird als Schwellenwert für Short-Einstiege verwendet.
- **Langsamer SMA (5 Perioden)** – wird als Schwellenwert für Long-Einstiege verwendet.

## Funktionsweise

1. Bei jeder abgeschlossenen 5-Minuten-Kerze aktualisiert die Strategie zwei SMAs des Kerzen-Eröffnungspreises.
2. Wenn keine aktive Position vorhanden ist:
   - **Long** einsteigen, wenn der letzte Schlusskurs unter dem langsamen SMA liegt.
   - **Short** einsteigen, wenn der letzte Schlusskurs über dem schnellen SMA liegt.
3. Nach dem Einstieg setzt die Strategie feste Stop-Loss- und Take-Profit-Niveaus in Preiseinheiten.
4. Die Position wird geschlossen, wenn Take-Profit oder Stop-Loss erreicht wird.

Die Logik verwendet die StockSharp High-Level-API und ist für Bildungszwecke gedacht.

## Parameter

| Name | Beschreibung | Standard |
|------|--------------|----------|
| `FastLength` | Periode des schnellen SMA. | `4` |
| `SlowLength` | Periode des langsamen SMA. | `5` |
| `TakeProfitLong` | Take-Profit-Abstand für Long-Trades in Preiseinheiten. | `25` |
| `TakeProfitShort` | Take-Profit-Abstand für Short-Trades in Preiseinheiten. | `26` |
| `StopLossLong` | Stop-Loss-Abstand für Long-Trades in Preiseinheiten. | `25` |
| `StopLossShort` | Stop-Loss-Abstand für Short-Trades in Preiseinheiten. | `3` |
| `CandleType` | Kerzentyp für die Analyse. | `TimeFrame(5m)` |

Alle Parameter können über den StockSharp-Optimierer optimiert werden.
