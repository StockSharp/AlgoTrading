# OsMA Vier-Farben-Pfeilstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie bildet das Verhalten des MetaTrader-Expertenberaters „OsMA Four Colors Arrow“ innerhalb des StockSharp-Frameworks nach. Der ursprüngliche EA reagiert auf die farbigen Pfeile, die vom begleitenden Indikator erzeugt werden, wann immer das OsMA (MACD-Histogramm) die Phase ändert. In der StockSharp-Version wird das gleiche Verhalten durch die Überwachung von Nulldurchgängen des MACD-Histogramms modelliert: Ein bullischer Cross (das Histogramm bewegt sich von negativ nach positiv) löst Long-Einstiege aus, während ein bärischer Cross Short-Einstiege auslöst. Der optionale Umkehrmodus stellt die Logik für Absicherungs- oder Mean-Reversion-Tests auf den Kopf.

Die Vorlage funktioniert nur mit fertigen Kerzen und kann eine tägliche Handelssitzung erzwingen, ähnlich dem Zeitfilter, der von der MQL-Version angeboten wird. Das integrierte Geldmanagement umfasst ein konfigurierbares Handelsvolumen, eine Obergrenze für die Anzahl der aggregierten Positionen und einen automatischen Stop-Loss-/Take-Profit-/Trailing-Schutz, ausgedrückt in Pips.

## Handelslogik

1. Abonnieren Sie den ausgewählten Zeitrahmen und berechnen Sie ein MACD-Histogramm (OsMA) mit konfigurierbaren schnellen, langsamen und Signallängen von EMA.
2. Wenn eine Kerze schließt, überprüfen Sie das Histogrammzeichen:
   - Histogramm kreuzt über Null → bullischer Pfeil → Kaufsignal.
   - Histogramm kreuzt unter Null → rückläufiger Pfeil → Verkaufssignal.
3. Wenden Sie optionale Funktionen an, bevor Sie eine Bestellung senden:
   - Richtungsfilter (nur lang, nur kurz oder beides).
   - Reverse-Modus zum Invertieren von Signalen.
   - Schließen Sie das bestehende Gegenrisiko, bevor Sie den neuen Trade eröffnen.
   - Beschränken Sie sich auf eine aktive Position oder akkumulieren Sie bis zur konfigurierten maximalen Belichtung.
4. Marktaufträge werden mit der konfigurierten Losgröße gesendet. `StartProtection` wandelt Pip-Eingaben in absolute Preis-Offsets um, um Stop-Loss, Take-Profit und Trailing-Management automatisch durchzuführen.
5. Trades werden außerhalb der zulässigen Intraday-Sitzung ignoriert, wenn der Zeitfilter aktiviert ist.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `CandleType` | Zeitrahmen, der für Berechnungen und Signalgenerierung verwendet wird. |
| `FastPeriod` / `SlowPeriod` / `SignalPeriod` | EMA Längen für das MACD Histogramm (OsMA). |
| `StopLossPips` / `TakeProfitPips` | Risikoziele in Pips. Zum Deaktivieren auf Null setzen. |
| `TrailingActivatePips` | Erforderlicher Gewinn (in Pips), bevor sich der Trailing Stop bewegen kann. |
| `TrailingStopPips` | Nachlaufdistanz in Pips. Null deaktiviert das nachgestellte Modul. |
| `TrailingStepPips` | Zusätzliche Pips, die gewonnen werden müssen, bevor der Trailing Stop wieder verschärft wird. |
| `MaxPositions` | Maximale aggregierte Positionseinheiten (`TradeVolume` Vielfache). Null bedeutet unbegrenzt. |
| `ReverseSignals` | Einstiegsrichtung umkehren (kaufen ↔ verkaufen). |
| `DirectionMode` | Beschränken Sie die Signale auf Nur-Long-Signale, Nur-Short-Signale oder beides. |
| `CloseOppositePositions` | Schließen Sie alle gegenüberliegenden Belichtungen, bevor Sie auf das neue Signal reagieren. |
| `OnlyOnePosition` | Bei `true` wird verhindert, dass eine bereits offene Position in die gleiche Richtung erweitert wird. |
| `UseTimeControl` | Aktivieren Sie den Intraday-Handelssitzungsfilter. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Sitzungsgrenzen (das Ende kann früher als der Beginn liegen, um Nachtsitzungen abzudecken). |
| `TradeVolume` | Bestellvolumen in Losen. |

## Notizen

- Trailing-Stop-Eingaben ahmen den EA nach: Trailing wird erst nach `TrailingActivatePips` verfügbar und bewegt sich in durch `TrailingStepPips` definierten Schritten.
- Die Strategie erfordert, dass das Wertpapier über einen gültigen `PriceStep` und `Decimals` verfügt, um Pips in Preis-Offsets umzuwandeln. Sofern das Instrument diese nicht bereitstellt, fallen die Vorgaben auf eine absolute Preiseinheit zurück.
- Wenn `MaxPositions` größer als eins ist, kann die Strategie durch wiederholtes Hinzufügen von `TradeVolume` unter Berücksichtigung des maximalen Expositionslimits schrittweise erweitert werden.
- Wenn `UseTimeControl` aktiviert ist und die Start- und Endzeiten übereinstimmen, ist der Handel deaktiviert, um mehrdeutige Sitzungen zu vermeiden.
- Die Logik wirkt nur auf geschlossene Kerzen; Es erfolgt keine Übermittlung einer Intra-Bar-Order, was dem Verhalten der Vorlage MQL entspricht.
