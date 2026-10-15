# MACD Vier Farben 2 Martingale Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

# MACD Vier Farben 2 Martingale

Die Strategie portiert den Fachberater „MACD Four Colors 2 Martingale“ von MetaTrader auf StockSharp. Es behält die ursprüngliche Logik bei, die auf der „Farb“-Interpretation von MACD und einem Martingal-Positionsgrößenmodell basiert.

## Überblick

Der zugrunde liegende Indikator malt das MACD-Histogramm mit fünf Farben. Im standardmäßigen „neuen“ Farbschema ändert das Histogramm seine Farbe abhängig davon, ob die MACD-Linie über/unter die Nulllinie steigt oder fällt. Der Expert Advisor eröffnet eine Position immer dann, wenn die Farben von Silber zu Gelb (negative MACD-Abwärtsbewegung) oder von Rot zu Blau (positive MACD-Umstellung) wechseln. Die StockSharp-Version reproduziert diese Sequenz, indem sie die Farben aus MACD-Werten rekonstruiert.

Es ist immer nur ein direktionaler Handelskorb aktiv. Ein neuer Trade ist nur zulässig, wenn sein Preis den durchschnittlichen Einstieg des aktuellen Korbs verbessert (niedrigerer Preis für Long-Positionen, höherer Preis für Short-Positionen). Jeder neue Eintrag multipliziert das zuletzt gefüllte Volumen mit einem konfigurierbaren Loskoeffizienten und implementiert so die Martingal-Mittelung aus dem ursprünglichen EA.

## Handelsregeln

- **Indikatorlogik**: Ein `MovingAverageConvergenceDivergenceSignal`-Indikator mit der klassischen 12/26/9-Konfiguration generiert MACD-Werte.
- **Farbrekonstruktion**: Die Strategie vergleicht die letzten beiden MACD-Werte. Ansteigendes Negativ MACD wird der Farbe 1 (Silber) zugeordnet, ansteigend positiv der Farbe 2 (Rot), fallend positiv der Farbe 3 (Blau) und fallend negativ der Farbe 4 (Gelb).
- **Langer Einstieg**: Wird ausgelöst, wenn sich die rekonstruierten Farben von 1 auf 4 bewegen, während der MACD im vorherigen Balken unter Null bleibt. Der Handel wird nur ausgeführt, wenn kein Short-Engagement besteht und der neue Preis niedriger ist als ein bestehender Long-Einstieg.
- **Kurzer Eintrag**: Wird ausgelöst, wenn sich die Farben von 2 auf 3 bewegen, während der MACD im vorherigen Balken über Null bleibt. Der Handel wird nur ausgelöst, wenn kein Long-Engagement besteht und der neue Preis höher ist als jeder bestehende Short-Einstieg.
- **Volumenverwaltung**: Die erste Bestellung verwendet `InitialVolume`. Jede weitere Bestellung im selben Warenkorb multipliziert das zuletzt ausgeführte Volumen mit `LotCoefficient`. Durch Einstellen des Koeffizienten ≤ 0 wird der Multiplikator deaktiviert.
- **Gewinn- und Verlustkontrolle**: Der variable PnL wird für jede fertige Kerze ausgewertet. Durch Drücken von `TargetProfit` werden alle Positionen geschlossen und der Martingalzyklus zurückgesetzt. Das Überschreiten von `MaxDrawdown` (interpretiert als Verlustschwelle) schließt ebenfalls alles und startet den Zyklus neu. Negative Schwellenwerte werden wie im Originalcode unterstützt.
- **Positionsausstieg**: Außer den Geldzielen gibt es keine automatischen Stops. Positionen bleiben offen, bis eine Risikoschwelle erreicht wird oder der Benutzer manuell eingreift.

## Parameter

- `CandleType` *(DataType, Standard 1h)* – Zeitrahmen für die MACD-Berechnung.
- `InitialVolume` *(dezimal, Standard 1)* – Volumen der ersten Bestellung in einem Warenkorb.
- `LotCoefficient` *(dezimal, Standard 2)* – Multiplikator, der auf das vorherige Volumen angewendet wird, wenn Martingal aktiv ist.
- `MaxDrawdown` *(dezimal, Standard 50)* – schwankender Verlustschwellenwert (in Geld), der die Liquidation erzwingt. Positive Werte beobachten `-MaxDrawdown`, negative Werte verwenden den genauen Wert.
- `TargetProfit` *(dezimal, Standard 150)* – variables Gewinnziel (in Geld), das den Warenkorb schließt. Negative Werte invertieren den Vergleich wie in der MQL-Version.
- `FastEmaPeriod` *(int, default 12)* – Länge des schnellen EMA für MACD.
- `SlowEmaPeriod` *(int, Standard 26)* – Länge des langsamen EMA für MACD.
- `SignalPeriod` *(int, Standard 9)* – Länge des Signals EMA für die Glättung durch MACD.

## Nutzungshinweise

- Funktioniert auf jedem Instrument, das `PriceStep` und `StepPrice` definiert, da der nicht realisierte PnL anhand der Börsenspezifikationen berechnet wird.
- Durch die Martingalgröße kann die Position schnell wachsen. Überprüfen Sie die Risikogrenzen, bevor Sie den Handel auf einem Live-Konto aktivieren.
- Zur visuellen Analyse fügen Sie den durch die Strategie erstellten Diagrammbereich hinzu. Es zeichnet Kerzen, den MACD-Indikator und ausgeführte Trades auf.

## Katalogfilter

- **Kategorie**: Trend-/Momentum-Mittelung
- **Richtung**: Beide (lange und kurze Körbe)
- **Indikatoren**: MACD
- **Stopps**: Nur geldbasierter Ausstieg
- **Zeitrahmen**: Konfigurierbar (Standard 1 Stunde)
- **Komplexität**: Mittelschwer
- **Risiko**: Hoch aufgrund der Martingal-Skalierung
- **Automatisierung**: Vollautomatisch nach dem Start
