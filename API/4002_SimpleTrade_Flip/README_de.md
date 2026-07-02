# SimpleTrade Flip-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- StockSharp-Port des MetaTrader 4-Expertenberaters **SimpleTrade.mq4** (auch bekannt als „neroTrade“).
- Konzipiert für den Handel mit Einzelsymbolen innerhalb des über den Parameter `CandleType` konfigurierten Zeitrahmens.
- Behält immer höchstens eine offene Position bei und ändert die Richtung bei der Eröffnung jedes neuen Balkens.

## Handelslogik
1. Jedes Mal, wenn eine neue Kerze aktiv wird, vergleicht die Strategie den Eröffnungspreis der Kerze mit dem Eröffnungspreis der Kerze, die `LookbackBars` Perioden älter ist.
2. Wenn die neue Eröffnung deutlich über dem historischen Referenzwert liegt, werden alle bestehenden Positionen geschlossen und eine neue Long-Market-Order mit `TradeVolume` Lots übermittelt.
3. Andernfalls (offen ist gleich oder niedriger) schließt die Strategie alle bestehenden Positionen und eröffnet eine Short-Marktposition derselben Größe.
4. Der Parameter `StopLossPoints` spiegelt die Einstellung `stop` des ursprünglichen EA wider. Wenn sowohl `PriceStep` als auch `StopLossPoints` des Wertpapiers verfügbar sind, wandelt die Strategie den Wert in einen absoluten Abstand um und leitet ihn an `StartProtection` weiter, sodass StockSharp die schützenden Stop-Loss-Orders automatisch aufrechterhalten kann.
5. Kerzenöffnungen werden mithilfe des High-Level-Kerzenabonnements API verfolgt. Fertige Kerzen füllen die Verlaufsliste, während die aktive Kerze die Entscheidung einmal pro Balken auslöst.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `TradeVolume` | Basisauftragsgröße, ausgedrückt in Losen. Muss positiv sein. | `1` |
| `StopLossPoints` | Schutzanschlagabstand in Instrumentenpunkten. Auf `0` setzen, um den automatischen Stop-Loss zu deaktivieren. | `120` |
| `LookbackBars` | Anzahl der für den offenen Preisvergleich verwendeten Balken. Ein Wert von `3` reproduziert `Open[0]` gegenüber `Open[3]` aus dem Originalcode. | `3` |
| `CandleType` | Zeitrahmen (als `DataType`), ab dem Kerzen angefordert werden. Steuert, wann neue Signale erscheinen. | `1 hour timeframe` |

## Implementierungshinweise
- Verwendet den High-Level-Workflow `SubscribeCandles(...).Bind(...)`, sodass die Strategie leichtgewichtig bleibt und sowohl auf historische als auch auf Live-Kerzen reagiert.
- `StartProtection` wird einmal während `OnStarted` aufgerufen. Stellen Sie sicher, dass die verbundene Sicherheit `PriceStep` bietet; Andernfalls kann die Stop-Loss-Distanz nicht in absolute Preise umgerechnet werden.
- Da alle Geschäfte mit Marktaufträgen zu Beginn jedes Balkens eingegeben werden, wird die Slippage-Behandlung an den Handelsplatz delegiert und es gibt keinen zusätzlichen `slippage`-Parameter.
- Der historische offene Puffer behält nur ein kleines rollierendes Fenster (`LookbackBars + 5`-Werte), um unnötigen Speicherverbrauch zu vermeiden.
- Es wird kein Python-Port bereitgestellt. Das Verzeichnis `CS/` enthält die einzige Implementierung.

## Dateistruktur
„
4002_SimpleTrade/
├── CS/
│ └── SimpleTradeFlipStrategy.cs
├── README.md
├── README_zh.md
└── README_ru.md
„
