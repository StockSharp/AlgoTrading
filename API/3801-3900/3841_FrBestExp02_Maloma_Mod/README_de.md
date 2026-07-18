# FrBestExp02 Maloma Mod-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Portierung des MetaTrader 4-Experten `Frbestexp02_1_maloma_mod.mq4`. Es kombiniert OsMA-Momentum, fraktale Umkehrungen, Bestätigung des Tick-Volumens und einen rollierenden täglichen Pivot-Filter, um erschöpfte Bewegungen im M15-Zeitrahmen auszublenden.

## Handelslogik

- **Sitzungs-Pivot** – ein rollierender Pivot-Punkt wird aus dem höchsten Hoch, dem niedrigsten Tief und dem ältesten Schlusskurs innerhalb eines konfigurierbaren Fensters berechnet (standardmäßig 96 Kerzen, entspricht einem Handelstag bei M15). Es sind nur Trades erlaubt, die mit dem Pivot-Bias übereinstimmen: Shorts über dem Pivot und Long-Positionen darunter.
- **Fraktales Muster** – die Strategie wartet auf ein bestätigtes Bill Williams-Fraktal drei Kerzen zurück. Abwärtsfraktale (Swing-Tiefs) ermöglichen Short-Positionen, während Aufwärts-Fraktale (Swing-Hochs) Long-Positionen ermöglichen.
- **OsMA-Histogramm** – ein MACD-Histogramm (standardmäßig schnell 12, langsam 26, Signal 9) muss für Short-Positionen weiter in den negativen Bereich und für Long-Positionen höher in den positiven Bereich abfallen. Der vorherige Histogrammwert muss ebenfalls auf der gleichen Seite von Null liegen.
- **Volumenfilter** – das Volumen der zuvor fertigen Kerze muss einen konfigurierbaren Schwellenwert überschreiten und größer sein als das Volumen vor zwei Kerzen. Dies reproduziert die Tick-Volumen-Spitzenanforderung des ursprünglichen Experten.
- **Order-Timing** – Trades werden durch ein Mindestintervall (standardmäßig 20 Sekunden) zwischen den Eingaben gedrosselt.
- **Risikomanagement** – konfigurierbarer Stop-Loss, Take-Profit und optionaler Trailing-Stop werden in Punkten ausgedrückt und in Instrumentenpreise umgerechnet. Schutzanordnungen werden mit den integrierten `SetStopLoss`/`SetTakeProfit`-Helfern aktualisiert.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Volume` | Bestellvolumen, das für jeden Eintrag verwendet wird. | 1 |
| `StopLossPoints` | Stop-Loss-Distanz in Instrumentenpunkten. | 1000 |
| `TakeProfitPoints` | Take-Profit-Distanz in Instrumentenpunkten. | 1000 |
| `TrailingStopPoints` | Optionaler Trailing-Stop-Abstand in Punkten (0 deaktiviert das Trailing). | 0 |
| `VolumeThreshold` | Mindestvolumen der vorherigen Kerze, das erforderlich ist, um ein Signal zu aktivieren. | 50 |
| `OsmaFastPeriod` / `OsmaSlowPeriod` / `OsmaSignalPeriod` | MACD Parameter, die zur Berechnung des OsMA-Histogramms verwendet werden. | 26.12.9 |
| `PivotWindow` | Anzahl der fertigen Kerzen, die in die Pivot-Berechnung einbezogen werden. | 96 |
| `MinTradeIntervalSeconds` | Mindestanzahl von Sekunden zwischen neuen Einträgen. | 20 |
| `CandleType` | Primärer Zeitrahmen (standardmäßig 15-Minuten-Kerzen). | M15 |

## Unterschiede zum MQL4-Experten

- Der ursprüngliche Code unterstützte mit `kh` multiplizierte Absicherungsaufträge und eine komplexe Gewinnrecyclinglogik. Die StockSharp-Version führt eine einzelne Richtungsposition aus und schließt oder kehrt sie um, bevor ein neuer Handel eröffnet wird.
- Die Handhabung von Trailing-Stops wird durch die Verwendung des standardmäßigen `SetStopLoss`-Helfers vereinfacht, anstatt Aufträge pro Tick manuell zu ändern.
- Auf Gewinnaggregation und Wiederherstellungsblöcke im Martingal-Stil wird verzichtet. Das Exit-Management setzt auf Stop-Loss, Take-Profit oder Trailing-Stop.
- Alle Indikatorberechnungen erfolgen ereignisgesteuert auf fertige Kerzen. Es gibt keine Änderung der Intrabar-Order.

## Nutzungshinweise

1. Hängen Sie die Strategie an ein Instrument an, das Tick-Volumendaten liefert, wenn der Volumenfilter dem ursprünglichen Verhalten entsprechen soll.
2. Halten Sie den Zeitrahmen bei 15 Minuten, um die ursprüngliche Kalibrierung des Pivot-Fensters und des fraktalen Lookbacks zu reproduzieren.
3. Passen Sie die `VolumeThreshold`- und OsMA-Perioden an, um sie an Symbole mit unterschiedlichen Volatilitäts- oder Volumenprofilen anzupassen.
4. Aktivieren Sie den Trailing Stop nur, wenn ein engerer Ausstieg gewünscht ist. andernfalls belassen Sie es bei Null, um sich auf den statischen Stopp/Ziel zu verlassen.

Der Code folgt den übergeordneten StockSharp API-Richtlinien: Kerzenabonnements über `SubscribeCandles`, Indikatorbindung für das MACD-Histogramm und sichere Ausführung über `BuyMarket`/`SellMarket` mit automatischen Schutzanordnungen.
