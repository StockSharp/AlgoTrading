# SilverTrend V3-Strategie (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die SilverTrend V3-Strategie ist ein Momentum-folgendes System, das aus dem MetaTrader 5 Expert Advisor „SilverTrend v3" stammt. Der StockSharp-Port reproduziert die ursprüngliche Logik und passt sie an die High-Level-Strategie-API an. Die Kernidee besteht darin, bullisches oder bärisches Momentum mithilfe einer SilverTrend-Kanalberechnung zu erkennen, es mit dem J_TPO-Marktprofil-Oszillator zu bestätigen und die resultierenden Positionen mit Schutzstops, Trailing-Logik und einem Freitagssitzungsfilter zu verwalten.

## Signal-Engine

1. **SilverTrend-Richtung**
   - Verwendet ein rollierendes 350-Bar-Fenster mit einem 9-Bar-Glättungsparameter zur Berechnung von dynamischer Unterstützung (`smin`) und Widerstand (`smax`).
   - Wenn der aktuelle Schlusskurs unter `smin` fällt, signalisiert das System ein bärisches Regime; ein Schlusskurs über `smax` wechselt das Regime auf bullisch.
   - Die Berechnung iteriert vom ältesten zum jüngsten Balken, um die rekursive Natur des ursprünglichen MQL-Codes zu replizieren.

2. **J_TPO-Bestätigung**
   - Implementiert den originalen 14-Perioden-J_TPO-Oszillator, der misst, wie sich Preise innerhalb einer kurzfristigen Verteilung konzentrieren.
   - Erlaubt Long-Einträge nur, wenn der Oszillator positiv ist, und Short-Einträge nur, wenn er negativ ist, um schwache Momentum-Verschiebungen zu filtern.

3. **Signaländerungserkennung**
   - Ein Trade wird nur eingeleitet, wenn die neu berechnete SilverTrend-Richtung vom vorherigen Wert abweicht, sodass die Strategie auf echte Regimewechsel statt auf Rauschen reagiert.

## Handelsmanagement

- **Markteinträge** – Die Strategie handelt das konfigurierte `Volume`. Wenn eine entgegengesetzte Position offen ist, wird sie geschlossen und in einer Marktorder umgekehrt.
- **Initialer Stop-Loss** – Optional. Definiert in Preisschritten relativ zum Einstiegspreis (konvertiert mit dem `PriceStep` des Instruments).
- **Take-Profit** – Optional. Ebenfalls in Preisschritten definiert und gegen Kerzenhochs/-tiefs ausgewertet, um das ursprüngliche Ordermodifikationsverhalten zu imitieren.
- **Trailing-Stop** – Aktiviert sich, sobald sich der Kurs um die konfigurierte Trailing-Distanz in die vorteilhafte Richtung bewegt. Bei Long-Positionen wird der Stop nach oben geführt, bei Shorts nach unten – entsprechend der MetaTrader-Logik.
- **Ausstieg bei entgegengesetztem Signal** – Wenn das vorherige Regime in die entgegengesetzte Richtung zeigt, wird jede bestehende Position beim nächsten Kerzenschluss liquidiert.
- **Freitags-Handelssperre** – Neue Positionen werden nach der angegebenen Stunde am Freitag übersprungen, um Wochenend-Gaps zu vermeiden, genau wie im Quell-EA.

## Parameter

| Name | Standardwerte | Beschreibung |
| --- | --- | --- |
| `TrailingStopPoints` | 50 | Trailing-Stop-Distanz in Preisschritten. Auf null setzen, um das Trailing zu deaktivieren. |
| `TakeProfitPoints` | 50 | Take-Profit-Distanz in Preisschritten. Null deaktiviert das Ziel. |
| `InitialStopLossPoints` | 0 | Initialer Schutz-Stop in Preisschritten. Null lässt die Position ohne initialen Stop. |
| `FridayCutoffHour` | 16 | Börsenstunde, nach der am Freitag keine neuen Einträge erlaubt sind. `0` verwenden, um den ganzen Tag zu handeln. |
| `CandleType` | 1-Stunden-Kerzen | Datenserie, die die Indikatoren speist. Jeder unterstützte Zeitrahmen kann verwendet werden. |
| `Volume` | 1 Lot | Handelsgröße für jede Position (StockSharp `Volume`-Eigenschaft). |

Alle Distanzen werden zur Laufzeit mit `PriceStep` multipliziert, was die Strategie automatisch an die Tick-Größe des Wertpapiers anpasst (einschließlich 3/5-stelliger Forex-Symbole).

## Daten- und Umgebungsanforderungen

- Erfordert mindestens 360 abgeschlossene Kerzen, bevor Live-Signale erzeugt werden, damit sowohl SilverTrend- als auch J_TPO-Puffer vollständig gebildet sind.
- Für den Einzelinstrumenten-Handel über `SubscribeCandles` konzipiert. Das `GetWorkingSecurities`-Override stellt sicher, dass die Strategie nur das konfigurierte Wertpapier und den Zeitrahmen abonniert.
- Verwendet `StartProtection()`, um den Standard-StockSharp-Positionsschutz-Service einmalig beim Start zu aktivieren.

## Verwendungshinweise

- Der Algorithmus erwartet trendende Instrumente wie wichtige Forex-Paare oder liquide Futures; den Zeitrahmen an die Marktvolatilität anpassen.
- Da die SilverTrend-Berechnung rekursiv ist, verzögert sich die Signalbildung beim Neustart der Strategie mit unzureichenden historischen Kerzen, bis genügend Daten gesammelt wurden.
- Die High-Level-API-Implementierung verwendet Kerzenextrema zur Simulation des Ordermanagements (Stop-Loss, Take-Profit, Trailing). Im Live-Handel empfiehlt es sich, die Logik mit tatsächlichen Stop-/Limit-Orders zu kombinieren, wenn die Infrastruktur dies erfordert.
- Der Port speichert den internen Zustand (`_previousSignal`, `_entryPrice`, Trailing-Stops) genau einmal pro abgeschlossener Kerze und entspricht damit dem „ein Trade pro Bar"-Verhalten des ursprünglichen EA.

## Konvertierungsdetails

- Reproduziert die mathematischen Routinen aus `SilverTrend v3.mq5` getreu, einschließlich des verschachtelten J_TPO-Algorithmus.
- Wendet C#-Best-Practices an: Parameter werden über `StrategyParam<T>` bereitgestellt, alle Kommentare sind auf Englisch, und die Einrückung verwendet Tabs gemäß den Repository-Richtlinien.
- Keine Python-Version in dieser Version gemäß den Aufgabenanforderungen enthalten.
