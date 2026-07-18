# RSI Benachrichtigungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **RSI Alert Strategy** reproduziert das Verhalten des Expertenberaters MetaTrader 5 „RSI Alert“ innerhalb des StockSharp-Frameworks. Der ursprüngliche Bot achtete auf Messwerte des Relative Strength Index (RSI), die stark überverkaufte (≤20) oder überkaufte (≥80) Werte kreuzten, und sendete beim Öffnen von Marktpositionen sofort Warnmeldungen. Die konvertierte Version behält diese ereignisgesteuerte Philosophie bei: Sie wartet auf abgeschlossene Kerzen, wertet den RSI aus und dreht die Position automatisch um, indem sie Marktaufträge sendet, wenn die konfigurierten Schwellenwerte erreicht werden.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie (Standard: 1-Minuten-Zeitrahmen) und geben Sie die Schlusskurse in einen `RelativeStrengthIndex`-Indikator ein.
2. Ignorieren Sie unvollständige Kerzen und warten Sie, bis der Indikator RSI vollständig gebildet ist. Dies spiegelt den MQL-Experten wider, der die Bedingungen nur einmal pro neuem Balken bewertete.
3. Handelssignale generieren:
   - **Kaufsignal** – RSI ≤ `OversoldLevel`. Die Strategie schließt jegliche Short-Position und eröffnet eine Long-Position mit dem konfigurierten Volumen.
   - **Verkaufssignal** – RSI ≥ `OverboughtLevel`. Die Strategie schließt jegliche Long-Position und eröffnet eine Short-Position mit dem konfigurierten Volumen.
4. Aufträge werden immer mit `BuyMarket`/`SellMarket` aufgegeben, es gibt also keine ausstehenden Aufträge, Stop-Loss- oder Take-Profit-Werte. Die MetaTrader-Implementierung erlaubte optionale SL/TP-Eingaben, war jedoch standardmäßig auf manuelle Verwaltung angewiesen. Der StockSharp-Port konzentriert sich auf die Konvertierung von Alert-to-Trade und überlässt das Risikomanagement externen Modulen (z. B. `StartProtection()` oder Kontrollen auf Portfolioebene).

Die Strategie bleibt zwischen den Signalen flach. Wenn ein entgegengesetzter Auslöser erscheint, kehrt er die Position um, indem er genug Volumen hinzufügt, um die bestehende Exposition abzuflachen, bevor er in die neue Richtung übergeht, genau wie es der ursprüngliche EA tat, als er aufeinanderfolgende Alarme auslöste.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `OrderVolume` | 0,01 | Handelsgröße für Marktaufträge. Beim Umkehren fügt die Strategie den erforderlichen Betrag hinzu, um die bestehende Position abzudecken, bevor sie erneut einsteigt. |
| `RsiPeriod` | 30 | RSI Mittelungszeitraum. Muss eine positive ganze Zahl sein. |
| `OverboughtLevel` | 80 | RSI Schwellenwert, der ein Verkaufssignal ausgibt. Kann optimiert werden, um die Aggressivität abzustimmen. |
| `OversoldLevel` | 20 | RSI Schwellenwert, der ein Kaufsignal ausgibt. |
| `CandleType` | 1 Minute `TimeFrameCandle` | Kerzendatenquelle, die für die RSI-Berechnung verwendet wird. Ändern Sie es, um höhere Zeitrahmen zu analysieren. |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht, sodass sie im StockSharp-Designer angezeigt werden, in XML-Voreinstellungen gespeichert werden können und Optimierungsszenarien unterstützen.

## Implementierungshinweise
- Das übergeordnete StockSharp API wird durchgehend verwendet: Kerzen werden über `SubscribeCandles()` abgerufen, und RSI wird über `subscription.Bind(indicator, callback)` aktualisiert. Es ist keine manuelle Pufferverwaltung oder historisches Kopieren erforderlich.
- Die Basiseigenschaft `Strategy.Volume` wird mit dem Parameter `OrderVolume` synchronisiert, sodass die Positionsumkehr auch dann korrekt funktioniert, wenn der Benutzer die Losgröße zur Laufzeit ändert.
- Inline-Kommentare und XML-Dokumentation werden entsprechend den Projektanforderungen in englischer Sprache verfasst.
- Die Diagrammausgabe ist optional, wird aber unterstützt: Wenn die Strategie im Designer ausgeführt wird, werden die Preiskerzen, ausgeführten Trades und die Indikatorwerte RSI dargestellt.

## Nutzungstipps
- Kombinieren Sie die Strategie mit externen Stop-Loss-/Take-Profit-Modulen, wenn eine automatisierte Risikokontrolle erforderlich ist.
- Optimieren Sie die RSI-Schwellenwerte bei der Anpassung an Märkte mit unterschiedlichen Volatilitätsregimen.
- Erhöhen Sie den Kerzenzeitrahmen für Swing-Setups oder behalten Sie die standardmäßige 1-Minuten-Serie für Warnungen im Scalping-Stil bei, wie im Originalskript.
