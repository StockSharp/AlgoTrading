# Exp Slow Stoch Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp High-Level-Port des MetaTrader 5-Expertenberaters **Exp_Slow-Stoch_Duplex**. Sie kombiniert zwei langsame stochastische Oszillatoren, die auf unabhängigen Zeitrahmen arbeiten, um koordinierte Long- und Short-Signale zu erzeugen. Jeder Oszillator liefert seine eigenen Kreuzungssignale, sodass die Strategie gerichtete Positionen öffnen oder schließen kann, während die Schutzorders das ursprüngliche Stop-Loss- und Take-Profit-Management emulieren.

## Handelsregeln

- **Long-Modul**
  - Bewertet den Long-Stochastik auf dem `LongCandleType`-Zeitrahmen.
  - Wendet die konfigurierte Glättungsmethode auf die %K- und %D-Werte an und verschiebt sie um `LongSignalBar` Balken.
  - Öffnet eine Long-Position, wenn %K über %D kreuzt (`previousK <= previousD` und `currentK > currentD`).
  - Schließt eine bestehende Long-Position, wenn %K wieder unter %D fällt (`currentK < currentD`).
- **Short-Modul**
  - Bewertet den Short-Stochastik auf dem `ShortCandleType`-Zeitrahmen.
  - Öffnet eine Short-Position, wenn %K unter %D kreuzt (`previousK >= previousD` und `currentK < currentD`).
  - Schließt eine bestehende Short-Position, wenn %K wieder über %D steigt (`currentK > currentD`).
- Orders werden als Marktorders ausgeführt. Das gesendete Volumen entspricht `TradeVolume` plus dem Absolutwert der aktuellen Position, damit Umkehrungen die vorherige Exposition zuerst flatten.
- Ein schützender Take-Profit und Stop-Loss in Preispunkten werden über `StartProtection` angehängt, um die MT5-Orderparameter nachzuahmen.

## Parameter

| Parameter | Typ | Standard | Beschreibung |
|-----------|-----|----------|--------------|
| `LongCandleType` | `DataType` | 8-Stunden-Kerzen | Zeitrahmen für den Long-Stochastik-Oszillator. |
| `LongKPeriod` | `int` | 5 | %K-Berechnungsperiode für den Long-Stochastik. |
| `LongDPeriod` | `int` | 3 | %D-Glättungsperiode für den Long-Stochastik. |
| `LongSlowing` | `int` | 3 | Zusätzliche Verlangsamung innerhalb der stochastischen Berechnung. |
| `LongSignalBar` | `int` | 1 | Anzahl geschlossener Balken für die Kreuzungsbewertung. |
| `LongSmoothingMethod` | `SmoothingMethod` | `Smoothed` | Sekundäre Glättung für %K und %D (None, Simple, Exponential, Smoothed, Weighted). |
| `LongSmoothingLength` | `int` | 5 | Länge des sekundären Glättungsfilters für den Long-Oszillator. |
| `LongEnableOpen` | `bool` | `true` | Erlaubt Long-Positionen zu öffnen. |
| `LongEnableClose` | `bool` | `true` | Erlaubt Long-Positionen zu schließen. |
| `ShortCandleType` | `DataType` | 8-Stunden-Kerzen | Zeitrahmen für den Short-Stochastik-Oszillator. |
| `ShortKPeriod` | `int` | 5 | %K-Berechnungsperiode für den Short-Stochastik. |
| `ShortDPeriod` | `int` | 3 | %D-Glättungsperiode für den Short-Stochastik. |
| `ShortSlowing` | `int` | 3 | Zusätzliche Verlangsamung innerhalb der stochastischen Berechnung. |
| `ShortSignalBar` | `int` | 1 | Anzahl geschlossener Balken für die Short-Kreuzungsbewertung. |
| `ShortSmoothingMethod` | `SmoothingMethod` | `Smoothed` | Sekundäre Glättung für die Short-%K- und %D-Werte. |
| `ShortSmoothingLength` | `int` | 5 | Länge des sekundären Glättungsfilters für den Short-Oszillator. |
| `ShortEnableOpen` | `bool` | `true` | Erlaubt Short-Positionen zu öffnen. |
| `ShortEnableClose` | `bool` | `true` | Erlaubt Short-Positionen zu schließen. |
| `TradeVolume` | `decimal` | 0.1 | Basisvolumen für Positionseintritte. |
| `TakeProfitPoints` | `decimal` | 2000 | Take-Profit-Abstand in Preispunkten. |
| `StopLossPoints` | `decimal` | 1000 | Stop-Loss-Abstand in Preispunkten. |

## Hinweise

- Die zusätzliche `SmoothingMethod` ahmt die optionale JJMA-basierte Glättung des ursprünglichen Indikators mithilfe der in StockSharp verfügbaren Standard-Moving-Averages nach. Wählen Sie `None`, um diese Stufe zu deaktivieren, wenn keine exakte Replikation erforderlich ist.
- Long- und Short-Module sind unabhängig; Sie können jede Seite über die entsprechenden boolean Flags aktivieren oder deaktivieren.
- Da StockSharp mit Nettopositionen arbeitet, schließt die Strategie immer die entgegengesetzte Exposition, wenn ein neues Signal die Richtung umkehrt.
