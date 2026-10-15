# Strategie VmMatrix Double Zero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
VmMatrix Double Zero ist ein StockSharp-Port des MetaTrader 4 Expert Advisors `vMATRIXDoubleZero`. Der ursprüngliche Roboter sucht nach „Doppelnull“-Ausbrüchen, indem er die vorherige Kerze auf nahezu zwei Dezimalstellen rundet und Trades eingeht, wenn der Preis dieses gerundete Niveau überschreitet. Der Port behält die geschichtete Filterstruktur des EA bei: konfigurierbare Mehrbalken-Bias-Vergleiche, optionale Volumen- und Bereichsprüfungen, ein ATR-Beschleunigungstor und einen sekundären Schwungstärkefilter. Die Strategie kann auch den täglichen Commodity Channel Index (CCI) zur Richtungsbestätigung erfordern und bietet eine adaptive Take-Profit-Komponente, die aus stündlichen ATR-Statistiken abgeleitet wird.

Der Handel ist auf ein benutzerdefiniertes Terminalzeitfenster beschränkt und separate Schalter steuern, ob Long- oder Short-Setups vorgenommen werden können. Stopps und Ziele werden intern verwaltet, einschließlich einer Annäherung an das ursprüngliche Trailing-Stop-Verhalten, das das Take-Profit-Niveau erweitert, wenn Trailing aktiviert ist.

## Strategielogik
### Bias-Erkennung
* **Abgerundeter Ausbruch** – Der Kernauslöser vergleicht den Schlusskurs der letzten beiden abgeschlossenen Kerzen mit dem vorherigen Schlusskurs, gerundet auf zwei Dezimalstellen. Ein langes Signal erfordert `Close[2] < round(Close[1], 2)` und `Close[1] > round(Close[1], 2)`; Kurze Signale kehren die Ungleichungen um.
* **Matrixfilter (optional)** – wenn aktiviert, werden sechs historische Kerzen, die durch die Parameter `LongK1…LongK6` (für Longs) oder `ShortK1…ShortK6` (für Shorts) definiert sind, anhand von Mittelpunktabweichungen verglichen. Jede Abweichung wird als `Close - (High + Low) / 2` berechnet. Die Vergleiche spiegeln das Original EA wider und erfordern, dass die erste Abweichung die zweite dominiert, die dritte eine mit einem Multiplikator skalierte vierte (`LongQc`/`ShortQc`) überschreitet und die fünfte eine zweite mit einem Multiplikator skalierte sechste (`LongQg`/`ShortQg`) überschreitet.

### Zusätzliche Filter
* **Sitzungsfilter** – Trades werden nur ausgewertet, wenn die Schlussstunde der verarbeiteten Kerze zwischen `StartHour` und `EndHour` (einschließlich) liegt.
* **Volumenfilter** – wenn aktiviert, muss das Gesamtvolumen der vorherigen Kerze `MinimumVolume` überschreiten.
* **Bereichskomprimierung** – das höchste Hoch und das niedrigste Tief der letzten `RangeBars` Kerzen müssen innerhalb von `RangeThresholdPips` Pips liegen.
* **ATR-Beschleunigung** – vergleicht den letzten ATR-Wert (`AtrPeriod` Länge im Arbeitszeitraum) mit dem ATR-Wert vor `AtrShift` Balken. Das Signal wird nur akzeptiert, wenn der aktuelle ATR höher ist, was den VSA-Umschalter von EA nachahmt.
* **Sekundärer Swing-Filter** – wenn aktiv, muss eine gewichtete Summe der Hoch-/Tief-Differenzen, die aus dem `SecondaryPivot`-Lookback erstellt werden, für Long-Positionen positiv und für Short-Positionen negativ sein. Die Gewichtungen (`Xb2`, `Xs2`, `Yb2`, `Ys2`) folgen dem ursprünglichen Parameterschema, wobei 50 Neutralität darstellt.
* **Tägliche CCI-Bestätigung** – optionales Gate, das erfordert, dass der letzte tägliche CCI-Wert (Zeitraum `DailyCciPeriod`) für Long-Positionen über Null und für Short-Positionen unter Null liegt.

### Auftragsverwaltung
* **Eingabegröße** – Aufträge verwenden `OrderVolume`, angepasst an die Volumenstufe des Wertpapiers. Wenn bereits eine Gegenposition offen ist, schließt die Strategie diese optional zuerst (`CloseOnBiasFlip` muss wahr sein); andernfalls wird der neue Eintrag übersprungen, da der Port in einer Netting-Umgebung läuft.
* **Anfangsstopps** – Stop-Loss-Abstände werden in Pips bis `LongStopLossPips`/`ShortStopLossPips` ausgedrückt und anhand der erkannten Pip-Größe umgerechnet. Take-Profit-Abstände verwenden `LongTakeProfitPips`/`ShortTakeProfitPips` und können durch die unten stehende dynamische Komponente erweitert werden.
* **Dynamischer Take-Profit** – wenn `UseDynamicTakeProfit` aktiviert ist, fügt die Strategie eine gewichtete Kombination aus stündlichen ATR-Statistiken und Swing-Differenzen zur Basis-Take-Profit-Distanz hinzu. Der Beitrag spiegelt die `TPb()`-Funktion von EA wider: Er mischt die Änderung der stündlichen ATR(1), der neuesten stündlichen ATR(1), der stündlichen ATR(25) und der Differenz zwischen den durch `SwingPivot`-Balken getrennten Höchstständen. Alle Gewichte sind um 50 herum zentriert und entsprechen der Originalschnittstelle.
* **Trailing Stop** – Durch die Aktivierung von `UseTrailingStop` wird ein stufenförmiger Trailing Stop aktiviert, der das Stop-Level immer dann erhöht (oder senkt), wenn der Preis etwa das Doppelte der konfigurierten Stop-Distanz über den aktuellen Stop hinaus überschreitet. Wie in der MQL-Version wird die Take-Profit-Distanz mit 10 multipliziert, um den Handel effektiv offen zu halten, während das Trailing aktiv ist.
* **Schutzausstiege** – bei jeder abgeschlossenen Kerze prüft die Strategie, ob der Stop-Loss oder Take-Profit verletzt wurde. Als Reaktion darauf werden die Positionen zum Marktwert geschlossen. Ein Bias-Flip (`CloseOnBiasFlip`) schließt auch die aktuelle Position, wenn das entgegengesetzte Signal erkannt wird.

## Parameter
Die folgende Tabelle fasst die bereitgestellten Parameter zusammen (alle stehen zur Optimierung zur Verfügung, sofern nicht anders angegeben):

| Gruppe | Parameter | Beschreibung |
| --- | --- | --- |
| Allgemein | `StartHour` / `EndHour` | Inklusives Handelsfenster in Endzeit. |
| Allgemein | `OrderVolume` | Basisordergröße, normalisiert auf den Volumenschritt des Instruments. |
| Allgemein | `UseTrailingStop` | Aktiviert die Trailing-Stop-Näherung und erweitert den Take-Profit-Faktor, um den EA zu emulieren. |
| Allgemein | `CloseOnBiasFlip` | Wenn „true“, wird das gegnerische Exposure geschlossen, bevor ein neuer Trade eingegangen wird. |
| Lang / Kurz | `EnableLongs` / `EnableShorts` | Schaltet zwischen langer und kurzer Signalverarbeitung um. |
| Lang / Kurz | `LongStopLossPips`, `LongTakeProfitPips`, `ShortStopLossPips`, `ShortTakeProfitPips` | Stop-Loss- und Take-Profit-Abstände, gemessen in Pips. |
| Filter | `UseBiasFilter` with `LongK1…LongK6`, `ShortK1…ShortK6`, `LongQc`, `LongQg`, `ShortQc`, `ShortQg` | Konfiguriert die Abweichungsvergleiche im Matrixstil für lange und kurze Signale. |
| Filter | `UseRangeFilter`, `RangeBars`, `RangeThresholdPips` | Lehnt Trades ab, wenn die aktuelle Preisspanne den Pip-Schwellenwert überschreitet. |
| Filter | `UseVolumeFilter`, `MinimumVolume` | Erfordert, dass das vorherige Kerzenvolumen den Schwellenwert überschreitet. |
| Filter | `UseVsaFilter`, `AtrPeriod`, `AtrShift` | Fordert, dass ATR im Vergleich zu vor `AtrShift` Balken gestiegen ist. |
| Filter | `UseSecondaryFilter`, `Xb2`, `Xs2`, `Yb2`, `Ys2`, `SecondaryPivot` | Gewichteter Schwungstärkefilter basierend auf Höhen und Tiefen. |
| Filter | `UseDailyCciFilter`, `DailyCciPeriod` | Täglich CCI Gate; Long-Positionen benötigen positive CCI, Short-Positionen benötigen negative CCI. |
| Nehmen Sie Gewinn mit | `UseDynamicTakeProfit`, `WeightSn1…WeightSn4`, `SwingPivot` | Steuert die adaptive Take-Profit-Komponente, die stündliche ATR-Metriken und Swing-Distanzen kombiniert. |
| Allgemein | `CandleType` | Primärer Zeitrahmen, der alle Signalberechnungen steuert. |

## Zusätzliche Hinweise
* Die Pip-Größe wird aus `Security.PriceStep` abgeleitet. Fünf- und dreistellige FX-Symbole werden automatisch einem 10-fachen Multiplikator zugeordnet, der die MQL-Behandlung von `Digits` und `Point` widerspiegelt.
* Der Port abonniert drei Datenströme: den Arbeitszeitrahmen, stündliche Kerzen (für ATR-Berechnungen) und tägliche Kerzen (für CCI). Stellen Sie sicher, dass der Datenanbieter alle angeforderten Zeitrahmen bereitstellen kann.
* Da StockSharp-Strategien auf Nettopositionen basieren, wird die gleichzeitige Absicherung desselben Instruments in beide Richtungen nicht unterstützt. Aktivieren Sie `CloseOnBiasFlip`, um die Fähigkeit von EA zum schnellen Schließen und Umkehren nachzuahmen.
* Das Trailing-Stop-Verhalten ist ungefähr; Der EA verwendete rohe Spread-Werte, um den nachgestellten Schritt zu bestimmen. Der Hafen erfordert, dass der Preis etwa die doppelte Stop-Distanz zurücklegt, bevor er den Stop vorrückt, was zu einem ähnlichen Ergebnis ohne explizite Spread-Informationen führt.
