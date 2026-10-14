# Powertrend - Volumen-Bereichsfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen volumenangepassten Bereichsfilter, um dynamische Preisbänder zu definieren. Long-Einstiege erfolgen, wenn der Preis das obere Band während eines Aufwärtstrends durchbricht, und Positionen werden geschlossen, wenn der Preis unter das untere Band fällt. Optionale Filter umfassen ADX, Trendbestätigung durch Hochs/Tiefs und eine VWMA des Filterwerts.

## Berechnung und Ausführung

C# und Python verwenden diese ausdrückliche kausale Definition. Auf abgeschlossenen Kerzen gelten `d = abs(Close - vorheriger Close)` und `V = TotalVolume`. Nach `RangePeriod` Änderungen wird im Fenster einschließlich der aktuellen Kerze `r = RangeMultiplier * sum(d * V) / sum(V)` berechnet; das Gesamtvolumen muss positiv sein. Der erste gebildete Filter `f` ist der aktuelle Schlusskurs. Danach wird das vorherige `f` auf `[Close - r, Close + r]` begrenzt. Entscheidungsbänder sind `vorheriges f + r` und `vorheriges f - r`, bevor sich das Zentrum ändert. Dadurch wird das Band nach der Filterbewegung nicht auf den Ausbruchspreis selbst gesetzt.

Nur aus einer Nullposition mit `Volume` einsteigen, wenn `Close > oberes Band` und der Filter steigt. Bei `Close < unteres Band` wird der gesamte Long per Marktorder geschlossen, unabhängig von Einstiegsfiltern. Gleichheit hält die Position. Ausstehende Orders sperren zusätzliche Orders. Keine Shorts, EMA-Kreuzungen oder Wartefrist.

Optionaler ADX muss gebildet sein und strikt über `AdxThreshold` liegen; deaktiviert wird er nicht registriert und verlangt keine Aufwärmung. Hoch-/Tiefbestätigung verlangt gleichzeitig `aktuelles High > vorheriges High` und `aktuelles Low > vorheriges Low`. Filterwert-VWMA ist `sum(f * V) / sum(V)` der letzten `VwmaPeriod` gebildeten Werte einschließlich des aktuellen. Erforderlich sind ein volles Fenster, positives Gesamtvolumen und `Close > VWMA`. Deaktivierte Filter stellen keine Aufwärmungsanforderung. Entscheidungen erfolgen zum Kerzenschluss per Marktorder, ohne Börsen-Stoporders oder angenommene Intrabar-Ausführung an Hochs/Tiefs.

Standardwerte: `RangePeriod=40`, `RangeMultiplier=1.5`, `AdxPeriod=14`, `AdxThreshold=20`, `VwmaPeriod=20`, `CandleType=TimeSpan.FromMinutes(5).TimeFrame()`. `IsAdxFilterEnabled`, `IsHighLowFilterEnabled`, `IsVwmaFilterEnabled` sind `false`. Perioden und Multiplikator positiv; ADX-Schwelle in `[0, 100]`. Diese Formel und Werte konkretisieren das Beispiel; die ursprüngliche Kurzbeschreibung enthielt keine Originalindikatorformel.
