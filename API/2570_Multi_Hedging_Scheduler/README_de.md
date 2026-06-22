# Multi-Hedging-Planer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Multi-Hedging-Planer-Strategie** ist eine direkte StockSharp-Konvertierung des ursprünglichen MetaTrader-5-Expertenberaters `MultiHedg_1.mq5`. Die Strategie ist für Konten ausgelegt, die Hedging ermöglichen, und kann bis zu zehn verschiedene Instrumente gleichzeitig verwalten. Sie öffnet Positionen in derselben Richtung während eines konfigurierbaren Handelsfensters und bietet Portfolio-Ausstiegslogik basierend auf Zeit- oder Eigenkapitalprozent-Schwellenwerten.

Anstatt sich auf Indikatoren zu stützen, verwendet die Strategie einen Ein-Minuten-Kerzenstrom (konfigurierbar) rein als Zeitquelle. Jede fertiggestellte Kerze löst Prüfungen aus, um Trades zu öffnen, alles zu schließen wenn das Handelsfenster abläuft, und eigenkapitalbasierte Risikoregeln durchzusetzen. Die Strategie eignet sich daher für Portfolios, bei denen die Ausführung eher durch Zeitplan als durch Preismuster gesteuert wird.

## Handelslogik
1. **Instrumentenauswahl** – Bis zu zehn Symbole können aktiviert werden. Für jeden aktivierten Eintrag löst die Strategie den Ticker über den `SecurityProvider` auf, abonniert Kerzen des konfigurierten Typs und verwendet dieselbe Logik für alle Instrumente.
2. **Handelsfenster** – wenn der Kerzenzeitstempel in das `TradeStartTime`-Fenster eintritt (das `TradeDuration` dauert), öffnet die Strategie eine Marktposition in der konfigurierten Richtung (`TradeDirection`) für jedes aktivierte Symbol, das noch keine offene Position in dieser Richtung hat. Wenn eine entgegengesetzte Position existiert, wird das Volumen erhöht, um zur gewünschten Seite zu wechseln.
3. **Eigenkapitalschutz** – wenn `CloseByEquityPercent` aktiviert ist und das Portfolio-Eigenkapital vom Startguthaben um `PercentProfit` oder `PercentLoss` abweicht, wird jede von der Strategie verwaltete offene Position geschlossen.
4. **Zeitbasierter Ausstieg** – wenn `UseTimeClose` aktiviert ist, schließt die Strategie alle verfolgten Positionen, wenn die Uhr das `CloseTime`-Fenster erreicht (das `TradeDuration` dauert).
5. **Protokollierung** – Aktionen wie Einstiege, eigenkapitalbasierte Ausstiege und zeitbasierte Ausstiege werden durch `LogInfo`-Aufrufe für Rückverfolgbarkeit protokolliert.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `TradeDirection` | Richtung aller Orders (`Buy` oder `Sell`). | Buy |
| `TradeStartTime` | Lokalzeit, wenn das Eintrittsfenster öffnet. | 19:51 |
| `TradeDuration` | Länge beider Eintritts- und Schließfenster. | 00:05:00 |
| `UseTimeClose` | Aktiviert das zeitbasierte Schließfenster. | true |
| `CloseTime` | Lokalzeit, wenn das Schließfenster öffnet. | 20:50 |
| `CloseByEquityPercent` | Aktiviert das Schließen aller Positionen bei Eigenkapitalschwellenwerten. | true |
| `PercentProfit` | Prozentuale Eigenkapitalgewinn, der einen globalen Schluss auslöst. | 1.0 |
| `PercentLoss` | Prozentualer Eigenkapitalrückgang, der einen globalen Schluss auslöst. | 55.0 |
| `CandleType` | Kerzentyp als Planungsantrieb. | 1-Minuten-Zeitrahmen |
| `UseSymbol0..9` | Schaltet den Handel für das entsprechende Symbol. | true für Symbole 0–5, false für 6–9 |
| `Symbol0..9` | Ticker für jeden Slot, über `SecurityProvider.LookupById` aufgelöst. | Siehe Standardwerte unten |
| `Volume0..9` | Ordervolumen für jeden Slot (Lots im Original-EA). | 0.1–1.0 |

**Standard-Symbol-Konfiguration**

| Slot | Aktiviert | Symbol | Volumen |
|------|---------|--------|--------|
| 0 | ✔ | EURUSD | 0.1 |
| 1 | ✔ | GBPUSD | 0.2 |
| 2 | ✔ | GBPJPY | 0.3 |
| 3 | ✔ | EURCAD | 0.4 |
| 4 | ✔ | USDCHF | 0.5 |
| 5 | ✔ | USDJPY | 0.6 |
| 6 | ✖ | USDCHF | 0.7 |
| 7 | ✖ | GBPUSD | 0.8 |
| 8 | ✖ | EURUSD | 0.9 |
| 9 | ✖ | USDJPY | 1.0 |

## Verwendungshinweise
- Sicherstellen, dass das Konto Hedging unterstützt, wenn das ursprüngliche MetaTrader-Verhalten repliziert werden soll. Auf Netting-Konten kompensiert die Strategie automatisch entgegengesetzte Positionen beim Wechseln der Richtungen.
- Instrument-Identifier in den `SymbolX`-Parametern genau so angeben, wie sie dem StockSharp `SecurityProvider` bekannt sind (zum Beispiel `EURUSD@FXCM`).
- Der Kerzenstrom wird nur verwendet, um die Planungslogik anzutreiben. `CandleType` anpassen, wenn die Datenquelle ein anderes Aggregationsintervall bereitstellt.
- Eigenkapitalschutz vergleicht das Live-Eigenkapital mit dem bei `OnStarted` erfassten Guthaben. Neustart der Strategie setzt das Referenzguthaben zurück.
- Die Strategie enthält keine Schutz-Stop- oder Take-Profit-Orders. Globale Ausstiege werden ausschließlich durch die Eigenkapitalprozentsätze und das Schließfenster gesteuert.

## Konvertierungshinweise
- Der ursprüngliche MT5-Experte verwendete `OnTick`. In der StockSharp-Version ersetzen fertiggestellte Kerzen Tick-Ereignisse, um Zeitfenster auf High-Level, ereignisgesteuerte Weise auszuwerten.
- Magic-Number-Filterung ist unnötig, da die Strategie innerhalb des Strategie-Containers von StockSharp operiert; daher iteriert `CloseAllManagedPositions` nur durch die konfigurierten Symbole.
- Sound-Benachrichtigungen und Chart-Kommentare wurden weggelassen, aber die Strategie protokolliert alle kritischen Aktionen über `LogInfo` für einfachere Prüfung.
