# KWAN CCC-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die KWAN CCC-Strategie reproduziert den MetaTrader-Experten `Exp_KWAN_CCC.mq5` mithilfe der High-Level-API von StockSharp. Das System leitet Handelssignale aus einem benutzerdefinierten Oszillator ab, der wie folgt aufgebaut ist:

1. Berechnung des Chaikin-Oszillators (Differenz zwischen schnellem und langsamem gleitendem Durchschnitt der Akkumulations-/Distributions-Linie).
2. Multiplikation des Chaikin-Werts mit dem Commodity Channel Index (CCI).
3. Division des Ergebnisses durch den Momentum-Indikatorwert. Wenn Momentum gleich null ist, ersetzt das Skript einen konstanten Wert von 100, um eine Division durch null zu vermeiden – genau wie der Originalcode.
4. Glättung der resultierenden Reihe mit der vom Benutzer gewählten XMA-Methode.
5. Erkennung der Steigung der geglätteten Reihe. Steigende Balken werden mit `0` gefärbt, fallende Balken mit `2`, andernfalls `1`.

Wenn sich die Farbe von `0` zu etwas anderem ändert, schließt die Strategie Shorts und eröffnet eine Long-Position. Wenn sich die Farbe von `2` zu etwas anderem ändert, schließt sie Longs und eröffnet einen Short. Dies spiegelt die im MQL-Experten implementierte Logik wider, einschließlich der optionalen Signalverschiebung (`SignalBar`).

## Handelsregeln
- **Long-Einstieg**: Farbe auf dem Balken bei `SignalBar + 1` ist gleich `0` und der Balken bei `SignalBar` unterscheidet sich von `0`.
- **Short-Einstieg**: Farbe auf dem Balken bei `SignalBar + 1` ist gleich `2` und der Balken bei `SignalBar` unterscheidet sich von `2`.
- **Long-Ausstieg**: aktiviert, wenn `EnableLongExits = true` und die Short-Einstiegsbedingung ausgelöst wird.
- **Short-Ausstieg**: aktiviert, wenn `EnableShortExits = true` und die Long-Einstiegsbedingung ausgelöst wird.
- Schutz-Stop- und Zielorders werden über `StartProtection` mit absoluten Preisoffsets erstellt, die aus `StopLossPoints` und `TakeProfitPoints` multipliziert mit dem `PriceStep` des Instruments abgeleitet werden.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `OrderVolume` | Basis-Ordergröße beim Eröffnen einer neuen Position. |
| `CandleType` | Zeitrahmen für alle Indikatorberechnungen. Standard ist 1 Stunde. |
| `FastPeriod` / `SlowPeriod` | Längen der gleitenden Durchschnitte im Chaikin-Oszillator. |
| `ChaikinMethod` | Typ des gleitenden Durchschnitts (einfach, exponentiell, geglättet, gewichtet) für die Akkumulations-/Distributions-Linie. |
| `CciPeriod` | Periode des Commodity Channel Index. |
| `MomentumPeriod` | Periode des Momentum-Indikators. |
| `SmoothingMethod` | XMA-Glättungsmethode, gemappt aus den Originaloptionen. `JurX`, `Parabolic` und `T3` fallen auf Jurik MA zurück; `Vidya` verwendet eine adaptiv geglättete Methode basierend auf dem Chande-Momentum-Oszillator; `Adaptive` verwendet Kaufman AMA. |
| `SmoothingLength` | Anzahl der Balken, die vom gewählten Glättungsfilter verwendet werden. |
| `SmoothingPhase` | Zusätzlicher Parameter für bestimmte Methoden (z. B. VIDYA CMO-Länge, AMA-Langsamperiode). |
| `SignalBar` | Offset (in abgeschlossenen Balken) zur Auswertung der Farbübergänge. `1` reproduziert den MetaTrader-Standard. |
| `EnableLongEntries` / `EnableShortEntries` | Eröffnung neuer Positionen in der entsprechenden Richtung erlauben oder blockieren. |
| `EnableLongExits` / `EnableShortExits` | Indikatorbetriebenes Positionsschließen erlauben oder blockieren. |
| `StopLossPoints` / `TakeProfitPoints` | Schutz-Stop/Ziel gemessen in Preisschritten (auf null setzen zum Deaktivieren). |

## Implementierungshinweise
- Die Strategie agiert nur auf abgeschlossenen Kerzen und verwendet StockSharp's `Bind`-Helfer, um Kerzendaten in die Indikatoren zu streamen.
- Die Liste der Glättungsmethoden spiegelt die XMA-Implementierung aus der Originalbibliothek wider. Methoden, die in StockSharp nicht verfügbar sind, werden der nächsten Alternative zugeordnet, wie in der Parametertabelle angegeben.
- MetaTraders `VolumeType`-Eingabe wird weggelassen, da StockSharp-Kerzen bereits die gesamten Volumeninformationen enthalten, die von der Akkumulations-/Distributions-Linie benötigt werden.
- Das Geldmanagement im Original-Experten basierte auf benutzerdefinierten Lot-Sizing-Helfern. Die Konvertierung geht von einem festen Volumen aus, das durch `OrderVolume` angegeben wird.

## Verwendungshinweise
- Stellen Sie sicher, dass das Instrument aussagekräftige Volumendaten liefert, wenn das Verhalten des Chaikin-Oszillators wichtig ist. Bei illiquiden Instrumenten sollten Sie `MomentumPeriod` erhöhen, um Rauschen zu reduzieren.
- Kombinieren Sie beim Optimieren der Glättungsparameter `SmoothingLength` und `SmoothingPhase` sorgfältig: Extreme Kombinationen können Signale erheblich verzögern.
- Die Standard-Schutzwerte (`StopLossPoints = 1000`, `TakeProfitPoints = 2000`) entsprechen großen Offsets. Passen Sie sie an die Tick-Größe des Instruments an.
