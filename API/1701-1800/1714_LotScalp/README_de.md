# LotScalp-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet täglich einen einzelnen Trade zu einer festgelegten Stunde basierend auf der Differenz zwischen vergangenen Kerzenöffnungen.

## Funktionsweise

1. **Warten auf den Handelszeitpunkt**: Die Strategie überwacht die Öffnungszeiten der Kerzen. Sobald die Stunde größer als `TradeTime` ist, wird der Handel beim nächsten Auftreten dieser Stunde erlaubt.
2. **Signalgenerierung**:
   - Wenn die aktuelle Stunde `TradeTime` entspricht, vergleicht die Strategie den Eröffnungspreis von vor `t1` Bars mit dem Eröffnungspreis von vor `t2` Bars.
   - Wenn die Differenz `Open[t1] - Open[t2]` `DeltaShort` Punkte überschreitet, wird eine Short-Position eröffnet.
   - Wenn die Differenz `Open[t2] - Open[t1]` `DeltaLong` Punkte überschreitet, wird eine Long-Position eröffnet.
3. **Positionsmanagement**:
   - Bei Long-Positionen verlässt die Strategie, wenn der Preis `TakeProfitLong` über dem Einstieg oder `StopLossLong` darunter erreicht.
   - Bei Short-Positionen verlässt sie, wenn der Preis `TakeProfitShort` unter oder `StopLossShort` über dem Einstieg liegt.
   - Positionen werden auch geschlossen, wenn sie länger als `MaxOpenTime` Stunden offen bleiben.

Die Strategie handelt mit festem Volumen und tritt bis zum nächsten Tag in keine neuen Trades ein.

## Parameter

| Name | Beschreibung |
| ---- | ------------ |
| `CandleType` | Kerzenquelle für die Strategie. |
| `Volume` | Ordervolumen. |
| `TakeProfitLong` | Take-Profit in Punkten für Long-Trades. |
| `StopLossLong` | Stop-Loss in Punkten für Long-Trades. |
| `TakeProfitShort` | Take-Profit in Punkten für Short-Trades. |
| `StopLossShort` | Stop-Loss in Punkten für Short-Trades. |
| `TradeTime` | Tagesstunde, zu der Signale ausgewertet werden. |
| `T1` | Anzahl der Bars zurück für den ersten Eröffnungspreis. |
| `T2` | Anzahl der Bars zurück für den zweiten Eröffnungspreis. |
| `DeltaLong` | Mindestdifferenz (in Punkten) zwischen `Open[t2]` und `Open[t1]` zum Eröffnen eines Long-Trades. |
| `DeltaShort` | Mindestdifferenz (in Punkten) zwischen `Open[t1]` und `Open[t2]` zum Eröffnen eines Short-Trades. |
| `MaxOpenTime` | Maximale Haltedauer in Stunden. |

## Hinweise

- Es werden nur abgeschlossene Kerzen verarbeitet.
- Die Strategie verwendet den Preisschritt des Instruments, um punktbasierte Schwellenwerte in absolute Preise umzurechnen.
- Es werden keine zusätzlichen Indikatoren verwendet.
