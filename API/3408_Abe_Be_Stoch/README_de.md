# ABE BE Stochastic Engulfing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader Expert Advisor **Expert_ABE_BE_Stoch** auf den StockSharp High-Level API. Es kombiniert die japanische Candlestick-Analyse mit der Bestätigung des Momentums, um Zeitumkehrungen in der Nähe von überverkauften und überkauften Zonen zu ermöglichen. Das primäre Signal sucht nach einer zinsbullischen Engulfing-Kerze, die durch einen stark überverkauften stochastischen Oszillator unterstützt wird, oder nach einer bärischen Engulfing-Kerze, die durch einen überkauften Oszillatorwert bestätigt wird. Sobald eine Position offen ist, basiert die Strategie auf stochastischen Schwellenwertüberschreitungen, um Ausstiege zu verwalten, und reproduziert dabei die „Abstimmungs“-Mechanik des ursprünglichen Experten.

Die Taktik ist sowohl für lange als auch für kurze Teilnahmen ausgelegt. Es wertet nur abgeschlossene Kerzen aus und bleibt daher immun gegen Intrabar-Rauschen. Die Handelsgröße bleibt unter der Kontrolle der `Volume`-Eigenschaft des Frameworks, während optionale Stop-Loss- und Take-Profit-Schutzmaßnahmen die ursprünglichen punktbasierten Risikoeinstellungen in StockSharp `Unit`-Objekte umwandeln.

## Wie es funktioniert

1. **Datenabonnement** – Die Strategie abonniert den konfigurierten Kerzentyp und erstellt ein `StochasticOscillator` mit drei einstellbaren Parametern (`%K`, `%D` und dem Verlangsamungsfaktor).
2. **Mustererkennung** – Bei jeder fertigen Kerze prüft der Algorithmus, ob der neueste Balken den Körper des vorherigen überdeckt. Zwei Hilfsmethoden reproduzieren die in MetaTrader verwendeten bullischen und bärischen Engulfing-Definitionen.
3. **Momentum-Bestätigung** – Die `%D`-Linie der Stochastik dient als Bestätigungsfilter. Für bullische Engulfing-Trades sind Werte unter dem Überverkaufsschwellenwert (Standard 30) erforderlich, während Werte über dem Überkaufsschwellenwert (Standard 70) für bärische Signale erforderlich sind.
4. **Positionsverwaltung** – Der vorherige `%D`-Wert wird zwischengespeichert. Wenn der neue Wert entweder 20 oder 80 nach oben überschreitet, wird jede Short-Position geschlossen. Umgekehrt liquidieren Abwärtskreuzungen von 80 oder 20 das Long-Engagement. Diese Schwellenwerte spiegeln die zusätzlichen „nahen“ Stimmen wider, die durch die MQL-Logik erzeugt werden.
5. **Risikomanagement** – Wenn positive Stop-Loss- oder Take-Profit-Abstände (ausgedrückt in Preisschritten) angegeben werden, wandelt die Strategie diese in `UnitTypes.Price` um und aktiviert `StartProtection`. Andernfalls wird der standardmäßige StockSharp-Schutz mit `StartProtection()` aktiviert.

## Handelsregeln

- **Long-Einstieg**: Die vorherige Kerze ist bärisch, die aktuelle Kerze ist bullisch und der Körper der aktuellen Kerze umhüllt den vorherigen Körper. Der stochastische `%D`-Wert muss unter dem `EntryOversoldLevel` liegen (Standard 30). Über `BuyMarket` wird ein eventuell bestehender Short geschlossen und ein neuer Long eröffnet.
- **Kurzer Eintrag**: Die vorherige Kerze ist bullisch, die aktuelle Kerze ist bärisch und der Körper der aktuellen Kerze umhüllt den vorherigen Körper. Der stochastische `%D`-Wert muss den `EntryOverboughtLevel` (Standard 70) überschreiten. Über `SellMarket` wird ein eventuell bestehender Long geschlossen und ein neuer Short eröffnet.
- **Long-Ausstieg**: Bei einem offenen Long-Kurs wird die Position mit `SellMarket` geschlossen, wenn `%D` nach unten durch entweder `ExitUpperLevel` (Standard 80) oder `ExitLowerLevel` (Standard 20) kreuzt.
- **Short-Ausstieg**: Wenn bei einem offenen Short `%D` entweder `ExitLowerLevel` oder `ExitUpperLevel` nach oben kreuzt, wird die Position durch `BuyMarket` gedeckt.
- **Stopps/Ziele**: Optional `StopLossPoints` und `TakeProfitPoints` wandeln punktbasierte Entfernungen in absolute Preisversätze um, wenn das Instrument einen `PriceStep` ungleich Null anzeigt.

## Parameter

| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Kerzenquelle zur Mustererkennung. |
| `StochasticPeriodK` | `int` | `47` | Lookback-Zeitraum für die schnelle `%K`-Berechnung. |
| `StochasticPeriodD` | `int` | `9` | Glättungszeitraum für die Signalleitung `%D`. |
| `StochasticPeriodSlow` | `int` | `13` | Auf `%K` wird eine zusätzliche Glättung angewendet, bevor es zu `%D` wird. |
| `EntryOversoldLevel` | `decimal` | `30` | Obergrenze für `%D`, die bullische Engulfing-Trades ermöglicht. |
| `EntryOverboughtLevel` | `decimal` | `70` | Untergrenze für `%D`, die bärische Engulfing-Trades ermöglicht. |
| `ExitLowerLevel` | `decimal` | `20` | Ebene, die bei Überschreitung nach oben kurze Ausstiege erzwingt; Wenn es nach unten gekreuzt wird, schließt es Long-Positionen. |
| `ExitUpperLevel` | `decimal` | `80` | Die obere Grenze wird auf die gleiche Weise wie die untere Ebene verwendet, jedoch für überkauftes Gebiet. |
| `TakeProfitPoints` | `decimal` | `0` | Abstand in Preisschritten für die Take-Profit-Order (0 deaktiviert sie). |
| `StopLossPoints` | `decimal` | `0` | Abstand in Preisschritten für die Stop-Loss-Order (0 deaktiviert sie). |

## Notizen

- Funktioniert auf jedem Instrument, das OHLC Kerzen liefert; Die Standardwerte gehen von stündlichen Balken aus.
- Alle Berechnungen basieren auf geschlossenen Kerzen, um mit der Zeitrahmenlogik des MQL-Experten in Einklang zu bleiben.
- Die Positionsgröße sollte über die Basisstrategie `Volume` Property oder ein Portfoliomanagement auf höherer Ebene konfiguriert werden.
