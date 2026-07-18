# Exp Iin MA Signal MMRec Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein StockSharp-Port des MetaTrader-Experten "Exp_Iin_MA_Signal_MMRec". Die Strategie hört auf die Kreuzungssignale, die von einem Paar konfigurierbarer gleitender Durchschnitte (der ursprüngliche Iin_MA_Signal-Indikator) erzeugt werden, und wendet ein adaptives Positionsgrößenschema mit verlustbasierter Reduzierung an.

## Überblick

- **Signalgenerierung**: die schnellen und langsamen gleitenden Durchschnitte werden auf dem ausgewählten Kerzentyp und dem angewandten Preis ausgewertet. Ein Kaufsignal wird erzeugt, wenn der schnelle Durchschnitt über den langsamen kreuzt, während ein Verkaufssignal beim umgekehrten Kreuzung erzeugt wird. Der Parameter `SignalBar` verschiebt die Ausführung um die angegebene Anzahl vollständig geschlossener Balken und reproduziert so die Indikatorpuffer-Verzögerung der MQL-Version.
- **Positionsverwaltung**: `BuyPosOpen` und `SellPosOpen` aktivieren oder deaktivieren Long- und Short-Einstiege. Wenn ein entgegengesetztes Signal erscheint und das entsprechende `BuyPosClose`- oder `SellPosClose`-Flag aktiviert ist, schließt die Strategie entweder das aktuelle Exposure oder dreht direkt in die neue Richtung um.
- **Risikokontrolle**: `StopLossPoints` und `TakeProfitPoints` werden mithilfe von `Security.PriceStep` in Preisabstände umgerechnet und gegen die Kerzenextreme geprüft, bevor neue Signale verarbeitet werden.
- **Geldmanagement**: die letzten Trades werden separat für Longs und Shorts verfolgt. Wenn die Anzahl der Verlust-Trades innerhalb des `BuyTotalTrigger`/`SellTotalTrigger`-Fensters den jeweiligen Verlustschwellenwert erreicht, wechselt die Strategie von `NormalVolume` zu `ReducedVolume`. Der Parameter `MoneyMode` definiert, wie der Volumenwert interpretiert wird (feste Lots, Saldoprozentsatz oder stop-basierter Risikoprozentsatz).

## Parameter

- `FastPeriod`, `SlowPeriod` – Längen der schnellen und langsamen gleitenden Durchschnitte.
- `FastType`, `SlowType` – Typen der gleitenden Durchschnitte (`Simple`, `Exponential`, `Smoothed`, `Weighted`, `VolumeWeighted`).
- `FastPrice`, `SlowPrice` – angewandter Preis für jeden Durchschnitt (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`).
- `SignalBar` – Anzahl der geschlossenen Balken zwischen einem erkannten Signal und der Auftragsübermittlung.
- `BuyPosOpen`, `SellPosOpen` – Schalter für das Öffnen von Long/Short-Positionen.
- `BuyPosClose`, `SellPosClose` – Schalter für das Schließen oder Umkehren einer bestehenden Position beim entgegengesetzten Signal.
- `BuyTotalTrigger`, `SellTotalTrigger` – wie viele aktuelle Trades für den Verlustzähler untersucht werden.
- `BuyLossTrigger`, `SellLossTrigger` – Mindestanzahl von Verlusten innerhalb des untersuchten Fensters, die das reduzierte Volumen aktiviert.
- `NormalVolume`, `ReducedVolume` – primäres und Fallback-Volumen (oder Risikofaktor, abhängig von `MoneyMode`).
- `StopLossPoints`, `TakeProfitPoints` – Stop-Loss- und Take-Profit-Abstände in Instrument-Punkten.
- `MoneyMode` – Interpretation der Volumenwerte (`Lot`, `Balance`, `FreeMargin`, `BalanceRisk`, `FreeMarginRisk`). Saldobasierte Modi verwenden `Portfolio.CurrentValue`, während risikobasierte Modi den Risikobetrag durch den berechneten Stop-Abstand dividieren.
- `CandleType` – Kerzenserie für Indikatorberechnungen.

## Signallogik

1. Jede fertige Kerze füttert die gleitenden Durchschnitte mit dem gewählten angewandten Preis.
2. Die Differenz zwischen aktuellen und vorherigen Werten der gleitenden Durchschnitte definiert ein Kreuzungsereignis.
3. Signale werden in die Warteschlange gestellt, und der älteste Eintrag wird ausgeführt, sobald die Warteschlangengröße `SignalBar` überschreitet.
4. Wenn ein Kaufsignal ausgeführt wird:
   - Wenn eine Short-Position besteht und `SellPosClose` aktiviert ist, berechnet die Strategie den realisierten PnL für diesen Short-Trade. Dann dreht sie entweder in einen Long um (wenn `BuyPosOpen` aktiviert ist) oder schließt einfach das Exposure.
   - Wenn keine Position offen ist und `BuyPosOpen` aktiviert ist, wird ein neuer Long mit dem berechneten Volumen eröffnet.
5. Verkaufssignale spiegeln den Kauf-Workflow.

## Geldmanagement-Details

- Der Trade-Verlauf wird als rollende FIFO-Warteschlange gespeichert, begrenzt durch `BuyTotalTrigger` / `SellTotalTrigger`.
- Ein Verlust-Trade (negativer PnL) erhöht den Verlustzähler. Wenn der Zähler `BuyLossTrigger` oder `SellLossTrigger` erreicht, verwendet die nächste Position `ReducedVolume`.
- `MoneyMode = Lot` behandelt Volumenwerte als Rohmengen.
- `MoneyMode = Balance` und `FreeMargin` multiplizieren den konfigurierten Wert mit `Portfolio.CurrentValue` und dividieren durch den aktuellen Schlusskurs, um die Menge zu erhalten.
- `MoneyMode = BalanceRisk` und `FreeMarginRisk` multiplizieren den konfigurierten Wert mit `Portfolio.CurrentValue` und dividieren durch den Stop-Loss-Abstand. Wenn der Stop-Abstand null ist, ist der Fallback identisch mit der Saldoprozentsatz-Berechnung.
- Wenn Portfolio-Informationen nicht verfügbar sind, fällt das berechnete Volumen auf null zurück, um versehentliche Aufträge zu vermeiden.

## Risikobehandlung

- Stop-Loss- und Take-Profit-Level werden auf jeder Kerze mit dem Einstandspreis und dem Punktwert neu berechnet. Wenn ein Level innerhalb des Kerzenbereichs berührt wird, wird die Position geschlossen, bevor neue Signale verarbeitet werden.
- Schließungsaktionen zeichnen immer das Trade-Ergebnis auf, um sicherzustellen, dass die Geldmanagement-Warteschlangen mit tatsächlichen Exits synchronisiert bleiben.

## Hinweise

- Stellen Sie sicher, dass `StopLossPoints` und `TakeProfitPoints` mit der Instrument-Tick-Größe kompatibel sind; die Strategie multipliziert sie mit `Security.PriceStep`.
- Wenn `MoneyMode` auf Portfolio-Daten angewiesen ist, erwartet die Strategie, dass das `Portfolio`-Objekt `CurrentValue` exponiert.
- Der Algorithmus arbeitet auf Nettopositonsbasis: gleichzeitige Long- und Short-Holdings werden nicht unterstützt.
