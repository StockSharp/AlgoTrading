# TDS Global Pending-Strategie (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie portiert den MetaTrader 5 Expert Advisor **TDSGlobal** aus `MQL/23255/TDSGlobal.mq5` auf die StockSharp High-Level-API. Sie bewertet den Momentum auf Vier-Stunden-Kerzen durch die MACD-Linie, das MACD-Histogramm (OsMA) und den Force Index. Wenn die Indikator-Kombination eine potenzielle Umkehr signalisiert, sendet die Strategie ausstehende Limit-Orders rund um die Extrempunkte der vorherigen Kerze und verwaltet die resultierende Position mit optionaler Stop-Loss-, Take-Profit- und Trailing-Stop-Logik.

Die Implementierung reproduziert den ursprünglichen Workflow und passt ihn an idiomatische StockSharp-Konstrukte wie `StrategyParam<T>`, Kerzen-Abonnements über `SubscribeCandles` und asynchrone Orderverarbeitung durch die Strategie-Lifecycle-Events an.

## Handelslogik

1. **Indikatorberechnungen**
   - `MACD(12, 26, 9)` liefert sowohl die MACD-Linie als auch das Histogramm (OsMA).
   - `ForceIndex(24)` misst die Kraft der letzten abgeschlossenen Kerze.
   - Jeder Indikator wird beim Schließen des gewählten Kerzentyps aktualisiert (Standard: 4 Stunden).
2. **Signalerkennung**
   - Der Algorithmus wartet, bis zwei historische MACD- und OsMA-Werte verfügbar sind, um deren Steigung zu bestimmen.
   - Ein *Verkaufs*-Setup erfordert, dass OsMA steigt (`osma[1] > osma[2]`), während der Force Index der vorherigen Kerze negativ ist.
   - Ein *Kauf*-Setup erfordert, dass OsMA fällt (`osma[1] < osma[2]`), während der vorherige Force Index positiv ist.
3. **Order-Platzierung**
   - Sell-Limit-Orders werden leicht oberhalb des vorherigen Kerzenhochs platziert; Buy-Limit-Orders leicht unterhalb des vorherigen Kerzenunterteils.
   - Wenn der Preis nicht weit genug vom aktuellen Bid/Ask entfernt ist, wird der Order-Preis zum konfigurierten Offset-Puffer gezogen (`EntryOffsetPips`, Standard 16 Pips).
   - Die Strategie prüft, ob der Abstand zwischen dem Order-Preis und dem aktuellen Bid/Ask die Broker-Sicherheitsniveau-Approximation überschreitet (`MinDistancePips` oder der dynamische Spread-basierte Wert).
4. **Risikokontrollen**
   - Optionale Stop-Loss- und Take-Profit-Niveaus werden aus dem Order-Preis berechnet.
   - Wenn eine Position aktiv ist, kann ein Trailing Stop um den konfigurierten Schritt vorrücken, sobald der Preis den anfänglichen Trailing-Abstand überschreitet.
   - Wenn der Preis innerhalb einer Kerze die Schutzniveaus erreicht, wird die Position mit einer Marktorder geschlossen, um das MetaTrader-Verhalten nachzuahmen.
5. **Order-Wartung**
   - Ausstehende Orders werden storniert, wenn die OsMA-Steigung gegen das ursprüngliche Setup dreht, entsprechend der Bereinigungsroutine des Quell-EAs.
   - Das Füllen einer Seite storniert automatisch die entgegengesetzte ausstehende Order, um widersprüchliche Engagements zu vermeiden.

## Geldverwaltung

Zwei Positionsgrößenansätze sind verfügbar:

- **Festes Volumen** (Standard `OrderVolume = 1`) — verwendet das Basis-`Strategy.Volume` ohne Anpassungen.
- **Risikobasierte Größenbestimmung** — wenn `UseRiskSizing` aktiviert ist, schätzt die Strategie das Portfolio-Eigenkapital, wandelt den konfigurierten Risikoanteil in Währungsrisiko um und dividiert es durch den Stop-Loss-Abstand, um das Order-Volumen abzuleiten. Volumina werden am Volumen-Schritt des Instruments ausgerichtet, um ungültige Order-Größen zu vermeiden.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `OrderVolume` | Feste Order-Größe wenn Risiko-Sizing deaktiviert ist. | 1 |
| `UseRiskSizing` | Geldverwaltung basierend auf `RiskPercent` aktivieren. | true |
| `RiskPercent` | Prozentsatz des Portfolio-Eigenkapitals pro Trade riskiert. | 3 |
| `MacdFastPeriod` | Schnelle EMA-Länge für die MACD-Linie. | 12 |
| `MacdSlowPeriod` | Langsame EMA-Länge für die MACD-Linie. | 26 |
| `MacdSignalPeriod` | Signal-EMA-Länge für das MACD-Histogramm. | 9 |
| `ForceLength` | EMA-Glättungslänge für den Force Index. | 24 |
| `StopLossPips` | Stop-Loss-Abstand in Pips (0 deaktiviert). | 50 |
| `TakeProfitPips` | Take-Profit-Abstand in Pips (0 deaktiviert). | 50 |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips (0 deaktiviert). | 5 |
| `TrailingStepPips` | Mindestschritt für Trailing-Updates. | 5 |
| `EntryOffsetPips` | Puffer um vorherige Hochs/Tiefs für ausstehende Orders. | 16 |
| `MinDistancePips` | Mindestzulässiger Abstand zwischen Preis und Schutzniveaus. | 3 |
| `PipSize` | Pip-Größe für Pip-zu-Preis-Konvertierungen. | 0.0001 |
| `CandleType` | Von der Strategie verarbeiteter Kerzentyp. | 4-Stunden-Kerzen |

## Verwendungshinweise

1. Füge die Datei `CS/TdsGlobalPendingStrategy.cs` zu deinem StockSharp-Projekt hinzu oder lade sie dynamisch über die Backtester-Umgebung.
2. Weise das gewünschte Wertpapier und Portfolio zu, bevor du die Strategie startest. Wenn `UseRiskSizing` aktiviert ist, stelle sicher, dass das Portfolio aktuelle Eigenkapitalwerte bereitstellt.
3. Die Strategie benötigt mindestens zwei abgeschlossene Kerzen, um MACD/OsMA-Steigungen zu initialisieren. Es ist eine kurze Aufwärmphase zu erwarten.
4. Überwache die Logs auf detaillierte Order- und Positionsereignisse. Die Implementierung protokolliert Schlüsselaktionen (Order-Einreichung, Stornierung, Trailing-Updates) zur Überprüfung gegen das ursprüngliche EA-Verhalten.

## Unterschiede zur MQL-Version

- Die High-Level-API verwaltet asynchrone Order-Events, sodass Limit-Order-Füllungen über `OnOwnTradeReceived` statt über synchrone `OrderSend`-Ergebnisse verarbeitet werden.
- Broker-"Freeze"- und "Stops"-Niveaus werden mit dem konfigurierten Mindestabstand und einer Spread-basierten Heuristik approximiert, da StockSharp keine MetaTrader-spezifischen Handelslimits exponiert.
- Schutzende Exits werden über Marktorders ausgeführt, wenn die Kerze einen Bruch zeigt. Dies repliziert die manuelle Stop-Modifikationslogik des EAs ohne Abhängigkeit von MT5-Handelsserver-Einschränkungen.

Diese Anpassungen halten die Handelslogik treu, während sie sicherstellen, dass die Strategie reibungslos in das StockSharp-Framework integriert wird.
