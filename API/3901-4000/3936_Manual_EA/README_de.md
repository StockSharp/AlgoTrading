# Manuelle EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Manuelle EA-Strategie** ist eine Eins-zu-eins-StockSharp-High-Level-API-Konvertierung des MetaTrader 4-Expertenberaters *Manual_EA.mq4* (Ordner `MQL/8159`). Das ursprüngliche System gibt nach eigenem Ermessen Kauf- oder Verkaufsaufträge aus, wenn der Stochastic-Oszillator extreme Zonen verlässt. Der StockSharp-Port behält die gleiche 5-3-3-Oszillatorkonfiguration bei, gleicht das bestehende Risiko automatisch aus, bevor die nächste Marktorder platziert wird, und stellt die gängigen Geldverwaltungsoptionen durch Strategieparameter offen.

## Handelslogik

1. Die Strategie abonniert die `CandleType`-Serie (Standard: 15-Minuten-Kerzen) und speist die Schlusskurse in einen Stochastic-Oszillator ein, der wie folgt konfiguriert ist:
   - `%K` Lookback = `KPeriod` (Standard 5 Balken)
   - `%K` Verlangsamung = `Slowing` (Standard 3 Balken)
   - `%D` Glättung = `DPeriod` (Standard 3 Balken)
2. Signale werden anhand des Endwerts der %D-Linie (Signal) jeder fertigen Kerze ausgewertet. Zur Erkennung von Bahnübergängen werden zwei aufeinanderfolgende Messwerte verglichen.
3. **Langer Eintrag** – Wenn der vorherige %D-Wert unter oder gleich `OversoldLevel` (Standard 10) war und der letzte Wert über diesen Schwellenwert steigt. Die Strategie neutralisiert zunächst jegliches Short-Engagement und kauft dann `Volume + |short position|` nach Marktorder.
4. **Kurzer Eintrag** – Wenn der vorherige %D-Wert über oder gleich `OverboughtLevel` (Standard 90) war und der letzte Wert unter diesen Schwellenwert fällt. Jede bestehende Long-Position wird geschlossen, bevor `Volume + |long position|` zum Marktpreis verkauft wird.
5. Schutzanordnungen werden über `StartProtection` abgewickelt. Ein positiver `StopLoss` und/oder `TakeProfit` (gemessen in Preispunkten) aktiviert das automatische Risikomanagement. Wenn Sie einen Parameter auf `0` setzen, wird der entsprechende Schutz deaktiviert.

Der Port vermeidet bewusst Indikatorpufferzugriffsmuster und unvollendete Kerzenlogik und entspricht den Best Practices von StockSharp auf hoher Ebene API.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Zeitrahmen (als `DataType`), der zum Aufbau von Kerzen und zum Antreiben des Oszillators verwendet wird. | 15-minütiger Zeitrahmen |
| `KPeriod` | Lookback-Länge der Zeile Stochastic %K. | 5 |
| `DPeriod` | Glättungslänge der Signalleitung Stochastic %D. | 3 |
| `Slowing` | Zusätzliche Glättung wird auf %K angewendet, bevor %D berechnet wird. | 3 |
| `OverboughtLevel` | Obergrenze, die Short-Einträge auslöst, wenn sie um %D nach unten überschritten wird. | 90 |
| `OversoldLevel` | Untere Grenze, die lange Einträge auslöst, wenn sie um %D nach oben überschritten wird. | 10 |
| `StopLoss` | Abstand in Preispunkten für den schützenden Stop-Loss (0 = deaktiviert). | 100 |
| `TakeProfit` | Abstand in Preispunkten für das Take-Profit-Ziel (0 = deaktiviert). | 100 |
| `Volume` | Bestellgröße, die mit jedem neuen Signal (Lots) gesendet wird. Vorhandene Gegenpositionen werden zunächst verrechnet. | 0,1 |

## Zusätzliche Hinweise

- Die Strategie verwendet `SubscribeCandles` zusammen mit `BindEx`, um `StochasticOscillatorValue`-Updates zu streamen und sicherzustellen, dass die Indikatorwerte endgültig sind, bevor Handelsentscheidungen getroffen werden.
- Bei der Diagrammvisualisierung werden automatisch die ausgewählten Kerzenserien, der Stochastic-Oszillator und eigene Trades dargestellt, wenn ein Diagrammbereich verfügbar ist.
- Da %D Kreuzungen bei aufeinanderfolgenden fertigen Kerzen ausgewertet werden, entspricht das Verhalten der MQL-Implementierung, die `MODE_SIGNAL`-Werte bei Schicht 1 und 2 verglichen hat.
