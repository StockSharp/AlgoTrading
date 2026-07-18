# Good Gbbi-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet täglich eine einzelne Position zu einer bestimmten Tagesstunde, basierend auf der Differenz zwischen historischen Eröffnungspreisen.

## Logik

* Arbeitet standardmäßig mit Stundenkerzen.
* Zur `TradeTime`-Stunde vergleicht die Strategie den Eröffnungspreis von vor `T1` Bars mit dem von vor `T2` Bars.
* Wenn der ältere Eröffnungspreis den neueren um `DeltaShort` Punkte übersteigt, wird eine Short-Position eröffnet.
* Wenn der neuere Eröffnungspreis den älteren um `DeltaLong` Punkte übersteigt, wird eine Long-Position eröffnet.
* Pro Tag ist nur ein Trade erlaubt. Der Handel wird wieder freigegeben, wenn die Stunde größer als `TradeTime` ist.
* Jede Position ist durch individuelle Take-Profit- und Stop-Loss-Niveaus geschützt und kann nach `MaxOpenTime` Stunden zwangsgeschlossen werden.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `TakeProfitLong` | Take-Profit-Abstand in Punkten für Long-Positionen. |
| `StopLossLong` | Stop-Loss-Abstand in Punkten für Long-Positionen. |
| `TakeProfitShort` | Take-Profit-Abstand in Punkten für Short-Positionen. |
| `StopLossShort` | Stop-Loss-Abstand in Punkten für Short-Positionen. |
| `TradeTime` | Tagesstunde, zu der die Einstiegsbedingungen geprüft werden. |
| `T1` | Anzahl der zurückliegenden Bars für den ersten Eröffnungspreis. |
| `T2` | Anzahl der zurückliegenden Bars für den zweiten Eröffnungspreis. |
| `DeltaLong` | Erforderliche Differenz in Punkten zum Eröffnen einer Long-Position. |
| `DeltaShort` | Erforderliche Differenz in Punkten zum Eröffnen einer Short-Position. |
| `MaxOpenTime` | Maximale Haltedauer der Position in Stunden; 0 deaktiviert die Prüfung. |
| `CandleType` | Zu verarbeitender Kerzentyp. |

## Hinweise

Die ursprüngliche Idee stammt aus dem MetaTrader Expert Advisor *GoodG@bi*. Diese Portierung verwendet die StockSharp High-Level-API und verarbeitet ausschließlich abgeschlossene Kerzen. Stellen Sie sicher, dass der `PriceStep` des Instruments korrekt konfiguriert ist, um Punktwerte zu interpretieren.
