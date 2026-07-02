# Sitzungsstrategie für Tokio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Tokyo Session Strategy repliziert die Logik des MetaTrader-Expertenberaters *TokyoSessionEA_v2.8* in StockSharp. Die
Die Strategie ist für den Intraday-Breakout- oder Mean-Reversion-Handel rund um die asiatische (Tokio) Sitzung konzipiert. Es fängt ein
Referenzkerze zu einer konfigurierbaren Stunde, baut aus dieser Kerze einen Preiskanal auf und bewertet Ausbruch oder Erholung
Bedingungen zu einer anderen Zielstunde. Abhängig vom gewählten Signalmodus kann die Strategie entweder gegensätzlich handeln
Level-Ausbruch (Fade-Bewegungen, die über den Referenzbereich hinausgehen) oder entlang der Ausbruchsrichtung.

Der StockSharp-Port konzentriert sich auf die Verwendung von API auf hoher Ebene. Alle Signalberechnungen werden innerhalb des Kerzenabonnements durchgeführt
Handler, Stopps werden über `StartProtection` verwaltet und jede Aktion wird über `LogInfo` protokolliert, um das Verhalten beizubehalten
transparent bei Backtests und Live-Handel.

## Handelslogik

1. **Referenzkerze** – um `TimeSetLevels` (Brokerstunde) zeichnet die Strategie den Höchst-, Tief- und Schlusskurs der Kerze auf. Diese
Werte definieren den Sitzungskanal und setzen die internen Validierungsflags zurück.
2. **Kanalvalidierung** – jede fertige Kerze zwischen der Referenzstunde und der Eintrittsstunde kann die ungültig machen
anstehendes Signal je nach Konfiguration:
   - `CheckAllBars`: Wenn aktiviert, muss der Schlusskurs zwischen dem erfassten Hoch und Tief liegen.
   - `ReCheckPrices`: Bei `TimeRecheckPrices` wird der Schlusskurs der Kerze mit dem laufenden Durchschnitt verglichen, um die Dynamik zu bestätigen.
3. **Einstiegsbewertung** – Wenn die Kerze vor `TimeCheckLevels` schließt, vergleicht die Strategie ihren Schlusskurs
mit den Kanalgrenzen. Liegt der Schluss innerhalb des konfigurierten Distanzbereichs, wird die entsprechende Position geöffnet.
4. **Exits** – Positionen können durch drei Mechanismen geschlossen werden:
   - `CloseInSignal` schließt einen Handel, sobald der Preis innerhalb des Kanals zurückkehrt (die Logik spiegelt die ursprüngliche EA wider).
   - `CloseOrdersOnTime` flacht bei `TimeCloseOrders` ab, um zu vermeiden, dass das Risiko in der nächsten Sitzung bestehen bleibt.
   - Schutzstopps, Trailing Stops und Break-Even-Handhabung werden an das StockSharp-Schutzsubsystem delegiert.

## Parameter

### Allgemein

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Für die Analyse verwendete Kerzenserie (standardmäßig H1). |
| `BrokerOffset` | Differenz zwischen Brokerzeit und GMT in Stunden. |

### Signale

| Parameter | Beschreibung |
|-----------|-------------|
| `TypeOfSignals` | `ContraryTrend` repliziert das Verblassen des Ausbruchs; `AccordingTrend` folgt der Ausbruchsrichtung. |
| `TimeSetLevels` | Stunde (0–23), in der die Referenzkerze erfasst wird. |
| `TimeCheckLevels` | Stunde, in der die Ausbruchsbedingungen bewertet werden. |
| `TimeRecheckPrices` | Zusätzliche Momentum-Check-Stunde. |
| `MinDistanceOfLevel` | Mindestabstand (in Pips) zwischen dem Schlusskurs und dem Kanal, bevor ein Handel zugelassen wird. |
| `MaxDistanceOfLevel` | Maximaler Abstand (in Pips) vom Level. Null deaktiviert das Limit. |
| `ReCheckPrices` | Aktiviert/deaktiviert den zusätzlichen Impulsfilter. |
| `CheckAllBars` | Erfordert, dass alle Zwischenschlüsse innerhalb des Kanals bleiben. |

### Risikomanagement

| Parameter | Beschreibung |
|-----------|-------------|
| `CloseInSignal` | Verlassen Sie den Kurs, sobald der Preis wieder die Kanalgrenze überschreitet. |
| `CloseOrdersOnTime` | Reduzieren Sie die Positionen nach `TimeCloseOrders`. |
| `TimeCloseOrders` | Stunde, die vom zeitbasierten Exit verwendet wird. |
| `UseTakeProfit`, `TakeProfit` | Aktivieren und konfigurieren Sie einen festen Take-Profit (Pips). |
| `UseStopLoss`, `StopLoss` | Aktivieren und konfigurieren Sie einen schützenden Stop-Loss (Pips). |
| `UseTrailingStop`, `TrailingStop`, `TrailingStep` | Aktivieren Sie die Trailing-Stop-Verwaltung (Pips) von StockSharp. |
| `UseBreakEven`, `BreakEven`, `BreakEvenAfter` | Bewegen Sie den Stop-Loss auf die Gewinnschwelle, sobald der Gewinn die Auslösedistanz erreicht. |

### Handel

| Parameter | Beschreibung |
|-----------|-------------|
| `Volume` | Grundauftragsvolumen. Bei Richtungsumkehr wird automatisch die Gegenposition geschlossen. |
| `MaxOrders` | Maximal zulässige Anzahl von `Volume` Blöcken in eine Richtung. Auf 0 setzen, um keine Begrenzung zu erhalten. |

## Arbeitsablauf

1. Setzen Sie die Strategie auf einem Instrument mit einem gültigen Preisschritt (`Security.PriceStep`) ein.
2. Wählen Sie den gewünschten Zeitrahmen aus und konfigurieren Sie die Stundenversätze des Brokers, um den Tagesplan an die Börse anzupassen.
3. Passen Sie die Distanz- und Validierungsfilter an, um sie an das Verhalten des ursprünglichen EA anzupassen oder um sie an verschiedene Märkte anzupassen.
4. Risikoparameter konfigurieren. Der StockSharp-Port verwaltet Stopps und Trailing-Logik nativ über `StartProtection`.
5. Starten Sie die Strategie. Protokollierungsnachrichten melden die erfassten Level, eröffneten Trades und Ausstiegsentscheidungen.

## Unterschiede zur MetaTrader-Version

- Gleitkommaeinträge basierend auf `UseFloatingPoint` und `PipsFloatingPoint` werden nicht implementiert, da StockSharp
führt Marktaufträge zum Zeitpunkt der Signalgenerierung aus.
- Spread- und Slippage-Filter werden weggelassen, da High-Level-Candle-Abonnements keine Bid/Ask-Daten auf Tick-Ebene liefern.
- Die automatische Geldverwaltung (`AutoLotSize`, `RiskFactor`, Rückgewinnungslose, voreingestellte Symbolumschaltung) wird durch die ersetzt
einfachere Parameter `Volume` und `MaxOrders`. Die Positionsgröße sollte direkt in den Strategieeinstellungen angepasst werden.
- Ton- und Druckbenachrichtigungen werden durch `LogInfo` Nachrichten ersetzt.

Alle anderen Signalbedingungen, Validierungs-Gates und zeitbasierten Exits spiegeln das Verhalten des ursprünglichen EA wider.

## Notizen

- Die Standardkonfiguration ist auf den vom ursprünglichen Fachberater empfohlenen H1-Zeitrahmen abgestimmt. Andere Zeitrahmen
kann verwendet werden, aber die stundenbasierte Logik geht davon aus, dass die Kerzendauer eine Stunde gleichmäßig aufteilt.
- Stellen Sie sicher, dass der Datenfeed kontinuierlich Kerzen für den ausgewählten Zeitraum liefert. Fehlende Kerzen können die ungültig machen
Durchschnitts- und Kanalprüfungen.
- Da die Strategie Positionen durch das Senden von Marktaufträgen schließt, benötigen Broker Limitaufträge oder einen Mindestbestand
Die Zeiten können zusätzliche Anpassungen erfordern.
