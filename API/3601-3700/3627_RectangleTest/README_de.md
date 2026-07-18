# Rechteckteststrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Rechtecktest-Strategie reproduziert den MetaTrader-Experten „RectangleTest“ unter Verwendung des übergeordneten API von StockSharp. Es erkennt Seitwärtsbereiche in einem Intraday-Zeitrahmen, prüft, ob zwei gleitende Durchschnitte und der aktuelle Preis innerhalb des erkannten Bereichs bleiben, und handelt dann Ausbrüche vom Rechteck weg in Richtung des schnelleren EMA. Die gesamte Logik wird auf abgeschlossene Kerzen ausgeführt, die von einer konfigurierbaren Kerzenquelle empfangen werden.

## Handelslogik
1. Abonnieren Sie den primären Kerzenstream (Standard: 1-Stunden-Zeitrahmen) und speisen Sie ihn in die folgenden Indikatoren ein:
   - **ExponentialMovingAverage (EMA)** mit konfigurierbarer Länge `EmaPeriod`.
   - **SimpleMovingAverage (SMA)** mit konfigurierbarer Länge `SmaPeriod`.
   - **Höchste** und **Tiefste**-Indikatoren mit der Länge `RangeCandles`, konfiguriert zum Lesen von Kerzenhochs und -tiefs. Sie stellen die Rechteckgrenzen bereit, die die MetaTrader-Array-basierten Berechnungen emulieren.
2. Sobald alle Indikatoren gebildet sind, berechnen Sie die Höhe des Rechtecks in Prozent relativ zur oberen Grenze. Nur Kerzen, deren Höhe kleiner als `RectangleSizePercent` ist, gelten als gültige Konsolidierungen.
3. Erfordern, dass EMA, SMA und die Kerze geschlossen sind, um innerhalb des Rechtecks zu bleiben. Dies reproduziert den Seitwärtsfilter aus der MQL-Version.
4. **Kurze Einrichtung**:
   - EMA liegt über SMA.
   - Der Schlusskurs liegt über EMA (entspricht der Bedingung „Ask > EMA“ von MetaTrader bei abgeschlossenen Kerzen).
   - Die optionale Liquidation einer bestehenden Long-Position erfolgt zunächst, danach wird eine Short-Market-Order gesendet.
5. **Lange Einrichtung**:
   - EMA liegt unter SMA.
   - Der Schlusskurs liegt unter EMA (entspricht der Regel „Gebot < EMA“).
   - Bestehende Short-Positionen werden vor der Eröffnung der Long-Position liquidiert.
6. Bei jedem Eintrag werden der erwartete Einstiegspreis und das erwartete Einstiegsvolumen erfasst. Wenn die Position Null erreicht, vergleicht die Strategie den Ausstiegspreis mit dem gespeicherten Einstiegspreis. Verlierende Trades erhöhen den täglichen Verlustzähler und erzwingen den `MaxLosingTradesPerDay`-Filter genau wie den MQL-Helfer `Loss()`.

## Geld- und Risikomanagement
- Die Strategie kann in zwei Modi funktionieren:
  - **Risikobasierter Modus** (`UseRiskMoneyManagement = true`): Das Positionsvolumen wird anhand des Kontowerts, des `RiskPercent` und des konfigurierten `StopLossPoints` ermittelt. Die Berechnung verwendet `Security.PriceStep`, `Security.StepPrice` und `Security.VolumeStep`, um die Routine zur Losgrößenbestimmung von MetaTrader widerzuspiegeln.
  - **Fester Volumenmodus** (`UseRiskMoneyManagement = false`): Trades verwenden den Parameter `FixedVolume`.
- Nachdem sich die Nettoposition von flach auf ungleich Null geändert hat, registrieren `SetStopLoss` und `SetTakeProfit` Schutzaufträge mit `StopLossPoints` und `TakeProfitPoints` (ausgedrückt in Preisschritten), die den SL/TP-Distanzen entsprechen, die im ursprünglichen Experten an `m_trade.Sell/Buy` übergeben wurden.
- `MaxLosingTradesPerDay` stoppt neue Signale für den Rest des Tages, sobald die angegebene Anzahl verlorener Trades erkannt wurde.

## Zeitmanagement
- Der Handel ist nur zwischen `TradeStartTime` und `TradeEndTime` zulässig. Der Helfer verarbeitet Intervalle, die sich über Mitternacht erstrecken, sowie Sitzungen tagsüber.
- Wenn `EnableTimeClose` wahr ist, werden alle offenen Positionen nach `TimeClose` liquidiert, wodurch die Eingaben MetaTrader „TimeCloseTrue“ und `TimeClose` repliziert werden.

## Unterschiede zur MetaTrader-Version
- Der ursprüngliche Indikator erzeugte grafische Rechtecke im Diagramm. StockSharp erstellt keine Zeichnungsobjekte; Stattdessen wird derselbe Bereich intern über Höchst-/Tiefstindikatoren berechnet.
- Verlierergeschäfte werden anhand der Schlusskurse der Signalkerze gezählt. Dies entspricht der Absicht von `Loss()` (Zählung verlorener Bestellungen pro Tag) und bleibt dabei innerhalb der StockSharp-Abstraktionen auf hoher Ebene.
- Auftragsabwicklungsmerkmale wie `ORDER_FILLING_FOK/IOC` werden von der Umgebung von StockSharp verarbeitet, sodass keine explizite Konfiguration des Abfüllmodus erforderlich ist.

## Parameter
| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `EmaPeriod` | 45 | Periode des schnellen EMA. |
| `SmaPeriod` | 200 | Zeitraum der langsamen SMA. |
| `RangeCandles` | 10 | Anzahl der Kerzen, die das Rechteck bilden. |
| `RectangleSizePercent` | 0,5 | Maximal zulässige Rechteckhöhe für den Handel. |
| `StopLossPoints` | 250 | Stop-Loss-Distanz in Preisschritten. |
| `TakeProfitPoints` | 750 | Take-Profit-Distanz in Preisschritten. |
| `UseRiskMoneyManagement` | wahr | Wechseln Sie zwischen risikobasiertem und festem Volumen. |
| `RiskPercent` | 1 | Prozentsatz des Kontokapitals, das pro Trade riskiert wird. |
| `FixedVolume` | 1 | Festes Volumen, wenn die risikobasierte Größenanpassung deaktiviert ist. |
| `MaxLosingTradesPerDay` | 1 | Tägliche Obergrenze für den Verlust von Trades. |
| `TradeStartTime` | 03:00 | Tageszeit, zu der Zutritte erlaubt sind. |
| `TradeEndTime` | 22:50 | Uhrzeit, nach der keine neuen Einträge mehr generiert werden. |
| `EnableTimeClose` | falsch | Ermöglicht die Liquidation am Tagesende. |
| `TimeClose` | 23:00 | Tageszeit zum Schließen aller Positionen. |
| `CandleType` | 1-Stunden-Kerzen | Primäre Kerzendatenquelle. |

## Diagramme
Wenn ein Diagrammbereich verfügbar ist, zeichnet die Strategie die Preiskerzen, schnelle EMA, langsame SMA und eigene Trades, um Bereichsausbrüche und Trade-Timing zu visualisieren.
