# Arttrader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Arttrader ist eine Konvertierung des MetaTrader 4 Expert Advisors `Arttrader_v1_5`. Das System arbeitet mit stündlichen Kerzen und versucht, sanfte Richtungsbewegungen zu erfassen, die durch einen exponentiellen gleitenden Durchschnitt (EMA) des Eröffnungspreises gemessen werden. Einträge werden sowohl nach der EMA-Steigung als auch nach einer strengen Intrabar-Preispositionsprüfung gefiltert, während ein spezieller Volatilitätsschutz Trades nach großen Eröffnungslücken blockiert. Die Positionen werden durch ein zeitgesteuertes Stop-Loss-Verfahren, feste Notstopp- und Take-Profit-Levels sowie eine volumenbasierte Ausfallsicherung verwaltet.

Der StockSharp-Port behält die ursprünglichen Eingaben bei und führt Geschäfte über Marktaufträge auf hoher Ebene aus. Alle Berechnungen werden an fertigen Kerzen durchgeführt; Die Intrabar-Timing-Anforderungen des Fachberaters werden durch den Vergleich der konfigurierten Minutenverzögerungen mit der Kerzendauer angenähert.

## Strategielogik
### Indikator
* **Eröffnungspreis EMA** – ein einzelner EMA mit konfigurierbarem Zeitraum (`EMA Speed`) wird auf Basis des Kerzeneröffnungspreises berechnet. Die Differenz zwischen dem aktuellen und dem vorherigen EMA-Wert definiert die Steigung in Pips.

### Filter
* **Steigungsgrenzen** – die EMA-Steigung muss zwischen dem minimalen (`Slope Min`) und dem maximalen (`Slope Max`) Schwellenwert liegen. Die Strategie ignoriert Trades, wenn der Trend entweder zu schwach oder zu stark ist.
* **Intrabar-Ausrichtung** – Long-Trades erfordern, dass die Kerze unter oder auf ihrem Eröffnungskurs schließt und innerhalb des Tiefs zuzüglich des konfigurierten Einstiegsfensters bleibt. Short-Trades spiegeln die Situation rund um das Hoch wider. Die Verzögerungsparameter (`Entry Delay`, `Exit Delay`) sind erfüllt, wenn die Dauer der aktuellen Kerze mindestens so lang ist wie die konfigurierten Minuten.
* **Volatilitätsspitzenschutz** – bewertet die Open-to-Open-Unterschiede zwischen den letzten fünf Kerzen. Wenn eine einzelne Lücke mehr als `Big Jump` Pips aufweist oder eine Lücke aus zwei Balken mehr als `Double Jump` Pips aufweist, werden neue Einträge für den aktuellen Balken blockiert.

### Einträge
* **Langer Eintrag** – wird ausgelöst, wenn alle Filter erfolgreich sind, die EMA-Steigung positiv ist und keine Position vorhanden ist. Der gespeicherte synthetische Einstiegspreis wird durch den Parameter `Spread Adjust` angepasst, um die ursprüngliche Spread-Kompensation zu emulieren.
* **Kurzer Eintrag** – symmetrische Logik, die eine negative EMA-Steigung und keine aktive Position erfordert.

### Ausgänge
* **Zeitgesteuerter Smart Stop** – einmal im Gewinn oder Verlust wertet die Strategie den Smart Stop erst aus, nachdem die `Exit Delay`-Anforderung erfüllt ist. Bei Long-Positionen muss der Schlusskurs über dem Eröffnungskurs und ausreichend nahe am Hoch liegen, während der Verlust in Pips im Verhältnis zum synthetischen Einstiegspreis `Smart Stop` überschreiten muss.
* **Volumenausfallsicher** – wenn das zuvor abgeschlossene Kerzenvolumen kleiner oder gleich `Min Volume` ist, wird jede offene Position sofort beim nächsten Balken geschlossen.
* **Notstopp / Take-Profit** – Sobald ein Trade eröffnet wird, werden ein harter Notstopp und ein festes Take-Profit-Level aufgezeichnet. Wenn die Kerzenspanne eines der beiden Niveaus erreicht, wird die Position geschlossen, ohne auf die zeitgesteuerten Filter zu warten.

## Parameter
* **Auftragsvolumen** – Handelsgröße, die für Marktaufträge verwendet wird.
* **EMA Zeitraum** – Länge des EMA, der auf Kerzeneröffnungen angewendet wird.
* **Big Jump (Pips)** – größte zulässige Einzelbar-Eröffnungslücke, bevor Einstiegssignale unterdrückt werden.
* **Double Jump (Pips)** – größte zulässige Eröffnungslücke von zwei Balken, bevor Einstiegssignale unterdrückt werden.
* **Smart Stop (Pips)** – Pip-Distanz, die erforderlich ist, um die zeitgesteuerte Stop-Loss-Logik auszulösen.
* **Notstopp (Pips)** – Hard-Stop-Distanz, bewertet bei jedem Kerzenhoch/-tief.
* **Take-Profit (Pips)** – feste Take-Profit-Distanz, die bei jedem Kerzenhoch/-tief ausgewertet wird.
* **Slope Min / Slope Max (Pips)** – EMA Slope-Grenzen für die Handelsberechtigung.
* **Eintrittsverzögerung (min)** – Mindestkerzendauer (in Minuten), bevor Einträge zulässig sind.
* **Exit Delay (min)** – minimale Kerzendauer (in Minuten), bevor der zeitgesteuerte Stopp ausgeführt werden kann.
* **Entry Slip / Exit Slip (Pips)** – Toleranz zwischen dem Schluss- und dem Extremwert bei der Validierung von Ein- und Ausstiegsfiltern.
* **Min Volume** – minimales vorheriges Kerzenvolumen; Geschäfte werden geschlossen, wenn der Wert nicht überschritten wird.
* **Spread-Anpassung (Pips)** – synthetischer Spread-Ausgleich, der auf den gespeicherten Einstiegspreis angewendet wird.
* **Slippage (Pips)** – Informationseinstellung wird aus Kompatibilitätsgründen mit den MetaTrader-Eingaben beibehalten.
* **Kerzentyp** – Zeitrahmen für Kerzenabonnements (standardmäßig 1-Stunden-Kerzen).

## Notizen
* Die StockSharp-Implementierung führt Marktaufträge aus und löscht Positionen mit `BuyMarket`/`SellMarket`, was dem Einzelpositionsverhalten des ursprünglichen EA entspricht.
* Da StockSharp mit fertigen Kerzen arbeitet, werden die Intrabar-Minutenprüfungen von MetaTrader angenähert, indem die konfigurierten Verzögerungen mit der Gesamtdauer der Kerze verglichen werden.
* Die Notstopp- und Take-Profit-Niveaus werden anhand der Kerzenhöchst- und -tiefstwerte bewertet und emulieren die maklerseitigen Schutzaufträge aus der MetaTrader-Version.
