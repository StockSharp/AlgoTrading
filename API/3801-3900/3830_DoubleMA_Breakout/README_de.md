# Double MA Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Double MA Breakout Strategy** ist eine StockSharp-Portierung des MetaTrader-Expertenberaters `DoubleMA_Breakout`. Die Strategie überwacht einen schnellen und einen langsamen gleitenden Durchschnitt der fertigen Kerzen. Wenn sich der schnelle Durchschnitt über den langsamen bewegt, wird eine Kauf-Stopp-Order in einer konfigurierbaren Ausbruchsdistanz über dem letzten Schlusskurs platziert. Wenn der schnelle Durchschnitt unter den langsamen fällt, wird symmetrisch unter dem Markt ein Verkaufsstopp platziert. Ausstehende Aufträge werden storniert und offene Positionen werden abgeflacht, wenn der Crossover umkehrt oder das Handelsfenster geschlossen wird.

Bei der Konvertierung bleibt die zentrale Breakout-Logik erhalten, es wird eine Auftragsverwaltung auf hoher Ebene hinzugefügt und eine umfassende Konfiguration über `StrategyParam<T>`-Parameter verfügbar gemacht. Alle Kommentare im Code wurden wie gewünscht in Englisch umgeschrieben.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `FastMaPeriod` | 2 | Periode des schnellen gleitenden Durchschnitts. |
| `SlowMaPeriod` | 5 | Periode des langsamen gleitenden Durchschnitts. |
| `FastMaMode` | `Simple` | Typ des gleitenden Durchschnitts für die Fast Line (SMA, EMA, SMMA, LWMA, LSMA). |
| `SlowMaMode` | `Simple` | Typ „gleitender Durchschnitt“ für die langsame Linie. |
| `FastAppliedPrice` | `Close` | Angewendeter Preis für den schnellen Durchschnitt (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet). |
| `SlowAppliedPrice` | `Close` | Angewandter Preis für den langsamen Durchschnitt. |
| `SignalShift` | 1 | Anzahl der abgeschlossenen Kerzen, auf die bei der Bewertung des Crossovers zurückgegriffen werden soll. `0` bedeutet die aktuelle Kerze. |
| `BreakoutDistancePoints` | 45 | Ausbruchsdistanz in Preisschritten, die verwendet wird, um Stop-Orders vom letzten Schlusskurs entfernt zu platzieren. |
| `UseTimeWindow` | `true` | Aktiviert den Start-/Stoppstundenfilter. |
| `StartHour` | 11 | Erste Stunde (einschließlich), in der neue Trades zulässig sind. |
| `StopHour` | 16 | Letzte Stunde (einschließlich), wenn der Handel erlaubt ist. |
| `UseFridayCloseAll` | `true` | Schließen Sie Positionen und stornieren Sie alle ausstehenden Aufträge, sobald die Schließzeit am Freitag erreicht ist. |
| `FridayCloseTime` | 21:30 | Tageszeit am Freitag, zu der die Strategie einen Hard Flat durchführt. |
| `UseFridayStopTrading` | `false` | Deaktivieren Sie neue Einträge nach der konfigurierten Freitag-Stoppzeit und behalten Sie bestehende Positionen bei. |
| `FridayStopTradingTime` | 19:00 | Tageszeit am Freitag, zu der neue Einträge blockiert werden (falls aktiviert). |
| `CandleType` | 1 Stunde | Kerzendatentyp, der sowohl für Indikatoren als auch für Signale verwendet wird. |

## Handelslogik
1. Abonnieren Sie fertige Kerzen, die durch `CandleType` definiert sind, und berechnen Sie zwei gleitende Durchschnitte entsprechend den ausgewählten Modi und angewendeten Preisen.
2. Behalten Sie kurze rollierende Historien der Indikatorwerte bei, damit die Strategie auf die von `SignalShift` ausgewählte Kerze verweisen kann, ohne gegen die Richtlinie „Kein GetValue“ zu verstoßen.
3. **Bulnisches Setup:** Wenn der schnelle MA über dem langsamen MA der Signalkerze liegt, heben Sie alle Verkaufsstopps auf, schließen Sie Short-Positionen und platzieren Sie eine Kaufstopp-Order `BreakoutDistancePoints × PriceStep` über dem letzten Schlusskurs, wenn keine Aufträge oder Positionen übrig sind.
4. **Bearisches Setup:** Wenn der schnelle MA unter dem langsamen MA der Signalkerze liegt, heben Sie alle Kaufstopps auf, schließen Sie Long-Positionen und platzieren Sie eine Verkaufsstopp-Order im gleichen Abstand unter dem Markt.
5. **Zeitmanagement:** Wenn das Handelsfenster deaktiviert oder geschlossen ist, werden alle ausstehenden Aufträge storniert. Freitags werden vor dem Wochenende die optionalen Stop-Trading- und Hard-Flat-Zeiten eingehalten.
6. Wenn eine Stop-Order ausgeführt wird, wird die gegnerische ausstehende Order storniert, um mehrere gleichzeitige Geschäfte zu vermeiden.

## Unterschiede zum MetaTrader EA
- Geldverwaltungsschalter und benutzerdefinierte Trailing-Stop-Schemata aus dem Originalskript werden nicht portiert. Die `Volume`-Eigenschaft von StockSharp definiert die Handelsgröße und eine Risikokontrolle kann durch Standardschutzmodule hinzugefügt werden.
- Fehlerwiederholungen und Bestellschleifen auf niedriger Ebene werden durch StockSharp-Helfer auf hoher Ebene (`BuyStop`, `SellStop`, `ClosePosition`, `CancelOrder`) ersetzt.
- Brokerspezifische Konzepte wie Margin-Cut-Offs oder Slippage-Korrekturen entfallen; diese können bei Bedarf separat implementiert werden.
- Der LSMA-Modus verwendet den `LinearRegression`-Indikator von StockSharp, um den in MetaTrader verwendeten gleitenden Durchschnitt der kleinsten Quadrate anzunähern.

## Nutzungshinweise
- Konfigurieren Sie `Volume`, bevor Sie mit der Strategie beginnen. Standardmäßig verwendet StockSharp ein einzelnes Los/einen einzelnen Vertrag.
- Kombinieren Sie die Strategie mit `StartProtection` (bereits im Code aufgerufen), um bei Bedarf Stop-Loss- oder Take-Profit-Module auf Plattformebene hinzuzufügen.
- Aktivieren Sie für Optimierungsworkflows die gewünschten Parameter über die im Konstruktor bereitgestellten `.SetCanOptimize`-Einstellungen.
- Stellen Sie sicher, dass das Instrument über einen gültigen `PriceStep` verfügt. andernfalls fällt die Ausbruchsdistanz auf `1` zurück, um Null-Offsets zu vermeiden.
