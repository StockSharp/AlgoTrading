# Blau TS Stochastic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MetaTrader Expert Advisors "Exp_BlauTSStochastic". Das System handelt mit William Blaus dreifach geglättetem stochastischen Oszillator, der dem ursprünglichen MQL-Paket beigefügt war. Der Indikator berechnet die höchsten und niedrigsten Preise über eine konfigurierbare Rückblickperiode, glättet den stochastischen Zähler und Nenner dreimal mit der ausgewählten gleitenden Durchschnittsfamilie, skaliert das Ergebnis auf den Bereich [-100, 100] um und erzeugt schließlich eine geglättete Signallinie. Alle Berechnungen werden auf fertigen Kerzen durchgeführt, die über die hochstufige Kerzenabonnement-API geliefert werden.

Der Indikator kann aus jedem der unterstützten angewendeten Preise (Schluss, Eröffnung, Hoch, Tief, Median, Typisch, Gewichtet, Einfach, Quartil, zwei Trendfolge-Varianten oder DeMark) und vier verschiedenen Glättungsalgorithmen (SMA, EMA, SMMA/RMA, WMA) aufgebaut werden. Die `SignalBar`-Einstellung erlaubt es, die Bar-Verschiebung des ursprünglichen Expert Advisors zu reproduzieren: Die Strategie wertet Signale auf Daten aus, die `SignalBar` Bars alt sind, sodass sie mit dem Standardwert von 1 auf die Bar reagiert, die im vorherigen Schritt gerade geschlossen hat.

## Einstiegs- und Ausstiegsregeln

Drei Handelsmodi sind verfügbar. In jedem Modus steuern die booleschen Schalter `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit` und `EnableShortExit`, ob die jeweiligen Aktionen erlaubt sind.

### Breakdown-Modus

*Long-Einstieg*: Der vorherige Histogrammwert (Verschiebung `SignalBar+1`) liegt über null und der aktuellere Wert (Verschiebung `SignalBar`) liegt bei oder unter null. Dies spiegelt die ursprüngliche Bedingung "Histogramm bricht durch null" wider und eröffnet oder dreht eine Long-Position, während auch alle Shorts gedeckt werden.

*Short-Einstieg*: Der vorherige Histogrammwert liegt unter null und der aktuellere Wert liegt bei oder über null, was einen Null-Linien-Durchbruch in die entgegengesetzte Richtung signalisiert. Die Strategie öffnet oder dreht zu einer Short-Position und schließt optional Long-Exposure.

Dieselben Bedingungen lösen auch Ausstiege auf der gegenüberliegenden Seite aus: Wenn das Histogramm die vorherige Bar über null verbringt, schließt die Strategie Shorts, und wenn es die vorherige Bar unter null verbringt, schließt es Longs.

### Twist-Modus

*Long-Einstieg*: Das Histogramm bildet ein lokales Tief. Konkret liegt der Wert bei Verschiebung `SignalBar+1` unter dem Wert bei Verschiebung `SignalBar+2`, aber der Wert bei Verschiebung `SignalBar` dreht nach oben und übersteigt die Zwischenbar. Das reproduziert den "Richtungsänderungs"-Modus aus dem Expert Advisor.

*Short-Einstieg*: Das Histogramm bildet ein lokales Hoch. Der Wert bei Verschiebung `SignalBar+1` ist größer als der Wert bei Verschiebung `SignalBar+2`, und der aktuellste Wert fällt unter die Zwischenbar. Positionen in der entgegengesetzten Richtung werden geschlossen, wenn ein Twist gegen sie auftritt.

### CloudTwist-Modus

Dieser Modus folgt den Farbänderungen der Indikatormolke, die durch das Histogramm und seine Signallinie definiert ist.

*Long-Einstieg*: Das Histogramm lag auf der vorherigen Bar über der Signallinie, aber der aktuellste Wert kreuzte unter oder berührte die Signallinie. Die Strategie behandelt die Farbänderung der Wolke als bullisches Signal und deckt optional Shorts.

*Short-Einstieg*: Das Histogramm lag auf der vorherigen Bar unter der Signallinie, aber der aktuellste Wert kreuzte über oder berührte die Signallinie. Das dreht zu einer Short-Position und verlässt optional Longs.

## Risikomanagement

* `StopLossPoints` und `TakeProfitPoints` werden in Instrument-Preisschritten gemessen. Wenn ein Wert größer als null ist, aktiviert die Strategie StockSharps eingebauten Schutzblock mit Marktorders, sodass die Stops die aktive Position automatisch verfolgen.
* Die Ordergröße wird aus der `Volume`-Eigenschaft der Strategie genommen. Wenn ein Umkehrsignal erscheint, sendet die Strategie `Volume + |Position|` Kontrakte, um sicherzustellen, dass die bestehende Position geschlossen wird, bevor eine neue eröffnet wird.

## Parameter

* `CandleType` – Zeitrahmen (Datentyp), der für den Oszillator verwendet wird (Standard: 4-Stunden-Kerzen).
* `Mode` – Signalerkennungsalgorithmus: `Breakdown`, `Twist` oder `CloudTwist`.
* `AppliedPrice` – Preisquelle für die stochastische Berechnung (Schluss, Eröffnung, Hoch, Tief, Median, Typisch, Gewichtet, Einfach, Quartil, Trendfolge 0/1 oder DeMark).
* `Smoothing` – Gleitende-Durchschnitt-Familie für alle Glättungsstufen (`Simple`, `Exponential`, `Smoothed`, `Weighted`).
* `BaseLength` – Anzahl der Bars zur Berechnung des Hoch/Tief-Bereichs.
* `SmoothLength1`, `SmoothLength2`, `SmoothLength3` – Glättungslängen für Zähler und Nenner (sequenziell angewendet).
* `SignalLength` – Glättungslänge für die Histogramm-Signallinie.
* `SignalBar` – Bar-Verschiebung, die definiert, welche historischen Werte für Entscheidungen verwendet werden.
* `StopLossPoints`, `TakeProfitPoints` – Schutzstop- und Zielgröße in Preisschritten (0 deaktiviert die entsprechende Order).
* `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit` – Erlaubnisschalter für die vier grundlegenden Aktionen.

Legen Sie das gewünschte `Volume` fest, hängen Sie die Strategie an ein Instrument an und starten Sie es. Alle Berechnungen basieren auf fertigen Kerzen, sodass das System wartet, bis Indikatoren gebildet sind, bevor Trades erlaubt werden.
