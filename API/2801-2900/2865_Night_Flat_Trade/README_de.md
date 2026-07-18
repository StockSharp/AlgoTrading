# Nacht-Flat-Trade-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Nacht-Flat-Trade-Strategie reproduziert den klassischen MQL5-Expertenberater, der nach engen nächtlichen Ranges auf EURUSD H1-Kerzen sucht. Sie konzentriert sich auf die Stunde rund um den Handelstageswechsel und wartet darauf, dass der Preis zu den Rändern eines engen Konsolidierungskanals zurückkehrt und auf eine Ausbruchsfortsetzung setzt. Die StockSharp-Version bewahrt die ursprünglichen Ideen und stützt sich auf High-Level-Kerzenabonnements, Indikatorbindungen und Parameterobjekte für bessere Konfigurierbarkeit.

## Überblick

- **Markt und Zeitrahmen**: Für EURUSD auf dem H1-Zeitrahmen konzipiert, aber jedes Instrument mit einem klar definierten Preisschritt kann verwendet werden.
- **Sitzungsfenster**: Einstiege sind nur während eines Zwei-Stunden-Fensters erlaubt, das um die konfigurierte `OpenHour` beginnt und bei `OpenHour + 1` endet (Börsenzeit).
- **Range-Filter**: Der Hoch-Tief-Bereich der letzten drei abgeschlossenen Kerzen muss zwischen `DiffMinPips` und `DiffMaxPips` bleiben (in Preiseinheiten umgerechnet).
- **Richtungsneigung**: Nur Long oder nur Short je nachdem, wo der letzte Schluss innerhalb der qualifizierenden Range liegt.

## Handelslogik

1. **Range-Grenzen berechnen**
   - Die Strategie bindet sich an die eingebauten `Highest`- und `Lowest`-Indikatoren (Länge = 3), um das höchste Hoch und tiefste Tief über die letzten drei Kerzen zu erhalten.
   - Der Abstand zwischen diesen Grenzen ist die Arbeitsrange, die für alle nachfolgenden Prüfungen verwendet wird.

2. **Einstiegsbedingungen**
   - **Long-Setup**: Während der aktiven Sitzung, wenn der Schlusskurs über dem Range-Tief liegt, aber noch innerhalb des unteren Viertels (`lowest + range/4`), öffnet die Strategie eine Long-Position mit einem anfänglichen Schutz-Stop bei `lowest - range/3`.
   - **Short-Setup**: Symmetrisch dazu, wenn der Schluss unter dem Range-Hoch liegt, aber noch im oberen Viertel (`highest - range/4`), wird eine Short-Position mit einem Stop bei `highest + range/3` eröffnet.

3. **Ausstiegsverwaltung**
   - **Stop-Loss**: Stops werden intern simuliert und lösen einen Marktausstieg aus, wenn die nächste Kerze den gespeicherten Schwellenwert verletzt.
   - **Take-Profit**: Wenn `TakeProfitPips > 0`, wird ein zusätzliches festes Take-Profit-Niveau (in Pips) relativ zum Einstiegspreis erstellt.
   - **Trailing-Stop**: Wenn sowohl `TrailingStopPips` als auch `TrailingStepPips` positiv sind, wird der Stop erst angepasst, nachdem der Preis um `TrailingStop + TrailingStep` Pips zugunsten des Trades vorgerückt ist. Nachfolgende Anpassungen erfordern einen zusätzlichen Fortschritt von `TrailingStepPips`, um das ursprüngliche stufenweise Trailing-Verhalten widerzuspiegeln.

4. **Wiedereinstiegskontrolle**
   - Der Algorithmus wartet immer, bis die aktuelle Position vollständig geschlossen ist, bevor er nach einem neuen Signal sucht, um das System zwischen Trades flat zu halten, wie beim Referenz-Expertenberater.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Zu abonnierende Kerzenserie (Standard H1). | 1-Stunden-Kerzen |
| `TakeProfitPips` | Optionaler Take-Profit-Abstand in Pips. | 50 |
| `TrailingStopPips` | Abstand zwischen Preis und Trailing-Stop in Pips (0 deaktiviert Trailing). | 15 |
| `TrailingStepPips` | Zusätzliche Pips vor jeder Trailing-Stop-Aktualisierung. | 5 |
| `DiffMinPips` | Minimal zulässige Drei-Kerzen-Range (Pips). | 18 |
| `DiffMaxPips` | Maximal zulässige Drei-Kerzen-Range (Pips). | 28 |
| `OpenHour` | Sitzungsstartsstunde in Börsenzeit (Einstiege bis `OpenHour + 1` erlaubt). | 0 |

## Indikatoren

- `Highest(Length = 3)` zum Überwachen der letzten Range-Obergrenze.
- `Lowest(Length = 3)` zum Überwachen der letzten Range-Untergrenze.

## Implementierungshinweise

- Die Pip-Konvertierung passt sich automatisch an Instrumente mit 3 oder 5 Dezimalstellen an, indem der gemeldete Preisschritt mit 10 multipliziert wird, genau wie die ursprüngliche MQ5-Implementierung.
- Da StockSharp in diesem Beispiel auf abgeschlossenen Kerzen operiert, werden Intra-Kerzen-Einstiegsbedingungen mit dem Schlusskurs angenähert. Dies hält die Logik deterministisch und bleibt dem Zweck des Quellcodes treu.
- Alle Risikoparameter sind über `StrategyParam<T>`-Objekte exponiert, wodurch sie in der UI sichtbar und bereit für Optimierung oder Batch-Experimente sind.
