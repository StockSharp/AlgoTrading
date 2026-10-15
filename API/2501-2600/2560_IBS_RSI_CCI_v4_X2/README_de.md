# IBS RSI CCI v4 X2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **IBS RSI CCI v4 X2-Strategie** ist ein Multi-Zeitrahmen-Momentum-System, das den Internal Bar Strength (IBS), den Relative Strength Index (RSI) und den Commodity Channel Index (CCI) kombiniert. Der ursprüngliche Algorithmus aus dem MetaTrader-5-Ökosystem wurde auf StockSharp portiert und neu gestaltet, um High-Level-Kerzen-Abonnements mit Indikator-Bindungen zu verwenden. Zwei unabhängige Indikator-Pipelines werden ausgewertet: ein langsamer „Trend"-Zeitrahmen, der die Richtungsneigung definiert, und ein schneller „Signal"-Zeitrahmen, der Ein- und Ausstiegsentscheidungen generiert.

Für jeden Zeitrahmen berechnet die Strategie einen zusammengesetzten Oszillator. Der Oszillatorwert wird aus den gewichteten Beiträgen von IBS, RSI und CCI abgeleitet. Schnelle Änderungen im zusammengesetzten Wert werden geglättet, durch einen konfigurierbaren Momentum-Schwellenwert begrenzt und mit einer Volatilitätshülle umwickelt, die die ursprüngliche Indikator-Pufferlogik nachahmt. Kreuzungen zwischen dem zusammengesetzten Wert und seiner geglätteten Hülle sind die Kerntrigger für Entscheidungen.

### Handelslogik

1. **Trenderkennung** – Der langsame Zeitrahmen überwacht den zusammengesetzten Oszillator. Wenn der Kompositwert über der Hülle bleibt, markiert die Strategie einen Aufwärtstrend, andernfalls kennzeichnet sie einen Abwärtstrend.
2. **Signalgenerierung** – Der schnelle Zeitrahmen wertet zwei aufeinanderfolgende Werte des Kompositwerts und der Hülle aus. Kreuzungen auf dem neuesten Balken bestätigen ein handelbares Signal nur, wenn der vorherige Balken den Übergang unterstützt.
3. **Einstiegsregeln** –
   * Nur Long einsteigen, wenn Long-Trades erlaubt sind, der aktuelle Trend bullisch ist und der Kompositwert im schnellen Zeitrahmen die Hülle nach unten kreuzt (bärisch-zu-bullische Umkehr in der ursprünglichen Indikatorausrichtung).
   * Nur Short einsteigen, wenn Short-Trades erlaubt sind, der aktuelle Trend bärisch ist und der Kompositwert die Hülle im schnellen Zeitrahmen nach oben kreuzt.
4. **Ausstiegsregeln** –
   * Optionale sofortige Ausstiege bei Kompositkreuzungen, wenn die Schalter `_CloseLongOnSignalCross` oder `_CloseShortOnSignalCross` aktiviert sind.
   * Erzwungene trendbasierte Ausstiege, wenn `_CloseLongOnTrendFlip` oder `_CloseShortOnTrendFlip` das Schließen anfordern, sobald die Neigung des langsamen Zeitrahmens umkehrt.
   * Das Risikomanagement wird durch StockSharp `StartProtection` gehandhabt, das die konfigurierten punktbasierten Stop-Loss- und Take-Profit-Distanzen in absolute Preisabstände umrechnet, indem der Preisschritt des Instruments verwendet wird.

### Indikatoren und Berechnungen

* **Internal Bar Strength (IBS):** `(close - low) / max(high - low, price step)` geglättet durch einen wählbaren gleitenden Durchschnitt.
* **RSI:** Standard-RSI angewendet auf einen konfigurierbaren angewendeten Preis (Schluss, Eröffnung, Hoch, Tief, Median, typisch oder gewichtet).
* **CCI:** Benutzerdefinierte CCI-Implementierung mit einem einfachen gleitenden Durchschnitt und einem Mittlere-Abweichungs-Schätzer, abgeleitet vom gewählten angewendeten Preis.
* **Zusammengesetzter Oszillator:** Gewichtete Summe der transformierten IBS-, RSI- und CCI-Werte geteilt durch drei, begrenzt durch die `Threshold`-Einstellung zur Replikation des ursprünglichen „Momentum-Limiters".
* **Hülle:** Die höchsten und niedrigsten Kompositwerte über den konfigurierten Bereich werden zweimal geglättet und gemittelt, um die Signalbasis für Kreuzungen zu erzeugen.

Die Implementierung vermeidet direktes Abfragen von Indikatorwerten (`GetValue`), indem der gesamte Zustand innerhalb der Berechnungsklassen gehalten und Kerzen sequenziell durch die High-Level-API gefüttert werden.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `OrderVolume` | Basisauftragsgröße beim Öffnen einer neuen Position. |
| `TrendCandleType` | Kerzentyp für das Abonnement des langsamen Zeitrahmens. |
| `TrendIbsPeriod`, `TrendIbsMaType` | IBS-Glättungsperiode und Typ des gleitenden Durchschnitts für den langsamen Zeitrahmen. |
| `TrendRsiPeriod`, `TrendRsiPrice` | RSI-Periode und angewendeter Preis für den langsamen Zeitrahmen. |
| `TrendCciPeriod`, `TrendCciPrice` | CCI-Periode und angewendeter Preis für den langsamen Zeitrahmen. |
| `TrendThreshold` | Momentum-Begrenzungsschwelle im Kompositwert des langsamen Zeitrahmens. |
| `TrendRangePeriod`, `TrendSmoothPeriod` | Rückblickbereich und Glättungsfenster für die Hülle des langsamen Zeitrahmens. |
| `TrendSignalBar` | Versatz (Anzahl geschlossener Kerzen zurück) beim Lesen von Werten des langsamen Zeitrahmens. |
| `AllowLongEntries`, `AllowShortEntries` | Neue Long-/Short-Trades aktivieren oder deaktivieren. |
| `CloseLongOnTrendFlip`, `CloseShortOnTrendFlip` | Positionsausstiege erzwingen, wenn die Neigung des langsamen Zeitrahmens sich umkehrt. |
| `SignalCandleType` | Kerzentyp für das Abonnement des schnellen Zeitrahmens. |
| `SignalIbsPeriod`, `SignalIbsMaType` | IBS-Glättungskonfiguration für den schnellen Zeitrahmen. |
| `SignalRsiPeriod`, `SignalRsiPrice` | RSI-Einstellungen für den schnellen Zeitrahmen. |
| `SignalCciPeriod`, `SignalCciPrice` | CCI-Einstellungen für den schnellen Zeitrahmen. |
| `SignalThreshold` | Momentum-Begrenzungsschwelle im Kompositwert des schnellen Zeitrahmens. |
| `SignalRangePeriod`, `SignalSmoothPeriod` | Hüllenbereich und Glättung im schnellen Zeitrahmen. |
| `SignalSignalBar` | Versatz beim Auswerten von Signalen des schnellen Zeitrahmens. |
| `CloseLongOnSignalCross`, `CloseShortOnSignalCross` | Optionale Ausstiegstrigger bei Kreuzungen des schnellen Zeitrahmens. |
| `StopLossPoints`, `TakeProfitPoints` | Stop-Loss- und Take-Profit-Distanzen gemessen in Preisschrittpunkten. |

## Verwendungshinweise

1. Konfigurieren Sie das Instrument und die Kerzentypen vor dem Start der Strategie. Beide Zeitrahmen werden automatisch über `GetWorkingSecurities` abonniert.
2. Die Standardkonfiguration spiegelt die ursprüngliche MQL-Version wider: 8-Stunden-Trendkerzen mit 1-Stunden-Signalkerzen und identischen Indikatoreinstellungen auf beiden Zeitrahmen.
3. Da der zusammengesetzte Oszillator intern begrenzt wird, können extreme Volatilitätsphasen flachere Reaktionen als typische Momentum-Strategien erzeugen. Passen Sie die Parameter `Threshold`, `RangePeriod` und `SmoothPeriod` an, um die Empfindlichkeit anzupassen.
4. Der eingebaute Positionsschutz basiert auf dem `PriceStep` des Instruments. Stellen Sie sicher, dass die Instrumentenmetadaten einen gültigen Schritt liefern, andernfalls erwägen Sie, den Fallback im Code anzupassen.
5. Verwenden Sie StockSharp-Charting-Helfer, wenn Sie das Verhalten visualisieren möchten. Die Strategie zeichnet bereits die Signalzeitrahmen-Kerzen und ausgeführten Trades, wenn ein Chartbereich verfügbar ist.

## Risiken und Einschränkungen

* Die Strategie setzt sequenzielle Kerzenlieferung voraus. Außer-der-Reihe-Kerzenaktualisierungen können die internen Puffer desynchronisieren.
* Die mittlere Abweichung im benutzerdefinierten CCI wird aus den gepufferten Werten neu berechnet; die Genauigkeit hängt vom Empfang eines kontinuierlichen Datenstroms ohne Lücken ab.
* Wenn `OrderVolume` mit bestehender Exposition kombiniert wird, werden Wechsel durch das Senden einer einzigen Marktorder durchgeführt, die auf das Schließen der entgegengesetzten Position und das Öffnen der neuen dimensioniert ist. Stellen Sie sicher, dass die Maklergenehmigungen dieses Verhalten erlauben.
* Der Port bewahrt die Ausrichtung des ursprünglichen Indikators (negative Koeffizienten). Signale können daher kontraintuitiv erscheinen, bis Sie das Legacy-Indikator-Design überprüfen.

## Erweiterung der Strategie

* Passen Sie die Typen des gleitenden Durchschnitts unabhängig für die Hülle und die IBS-Glättung an, um schnellere oder langsamere Reaktionen zu erkunden.
* Ersetzen Sie den benutzerdefinierten CCI-Rechner durch den eingebauten Indikator von StockSharp, wenn eine zukünftige Version die notwendigen Preisselektoren bereitstellt.
* Fügen Sie Chart-Overlays hinzu, indem Sie die Kompositwerte an zusätzliche Chart-Panes binden, wenn mehr visuelles Feedback benötigt wird.
* Kombinieren Sie mit zusätzlichen Risikokontrollen wie maximalem Tagesverlust oder Handelszeitfiltern für Produktionseinsätze.
