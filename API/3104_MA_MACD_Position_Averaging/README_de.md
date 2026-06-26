# MA MACD Positionsmittelungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine getreue Konvertierung des MetaTrader Expert Advisors **"MA MACD Position averaging"**. Sie kombiniert
einen gewichteten gleitenden Durchschnittsfilter mit einer MACD-Verhältnisprüfung und fügt ein Martingal-artiges Mittelungsmodul
hinzu, das die Positionsgröße erhöht, wenn sich der Preis um eine konfigurierbare Anzahl von Pips ungünstig bewegt. Alle
Risikoparameter werden in Pip-Einheiten konfiguriert und intern in Preisabstände umgerechnet, unter Verwendung der von
StockSharp bereitgestellten Instrument-Metadaten.

## Handelslogik

1. **Indikatorvorbereitung**
   - Ein konfigurierbarer gleitender Durchschnitt (`MaPeriod`, `MaMethod`, `MaAppliedPrice`) wird auf abgeschlossenen Kerzen
     abgetastet. Die Parameter `SignalBar` und `MaShift` emulieren die Fähigkeit von MetaTrader, um eine bestimmte Anzahl von
     Bars zurückzublicken und den gleitenden Durchschnitt mit einem horizontalen Offset darzustellen.
   - Ein MACD-Indikator (`MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod`, `MacdAppliedPrice`) wird auf denselben Kerzen
     verarbeitet. Die Strategie speichert die MACD-Haupt- und Signallinie in einem kleinen rollierenden Puffer, damit historische
     Werte ohne direkte Aufrufe von Indikator-APIs zugänglich sind.
2. **Eintrittsbedingungen**
   - **Long**: Beide MACD-Linien sind unter null, das Verhältnis `MACDmain / MACDsignal` ist größer oder gleich `MacdRatio`,
     der Kerzenschlusskurs liegt über dem abgetasteten gleitenden Durchschnitt und der Abstand zwischen Preis und Durchschnitt
     beträgt mindestens `IndentPips` Pips.
   - **Short**: Beide MACD-Linien sind über null, das Verhältnis ist über `MacdRatio`, der Kerzenschlusskurs liegt unter dem
     gleitenden Durchschnitt und der Abstand zwischen ihnen beträgt mindestens `IndentPips` Pips.
   - Neue Einträge sind nur erlaubt, wenn die Strategie kein Exposure hat. Wenn ein Mittelungszyklus bereits im Gange ist,
     wird die Signallogik übersprungen und nur die Mittelungsregeln gelten.
3. **Mittelungsmodul**
   - Wenn eine Long-Position besteht und der Preis mindestens `StepLossingPips` vom besten (niedrigsten) Long-Eintrag fällt,
     öffnet die Strategie einen zusätzlichen Long-Trade, dessen Volumen gleich dem letzten Leg-Volumen multipliziert mit
     `LotCoefficient` ist (gerundet nach dem Instrument-Volumenschritt).
   - Wenn eine Short-Position besteht und der Preis mindestens `StepLossingPips` vom besten (höchsten) Short-Eintrag steigt,
     wird ein neues Short-Leg mit demselben `LotCoefficient`-Multiplikator hinzugefügt.
   - Wenn Exposure in beiden Richtungen erkannt wird (sollte unter normalen Bedingungen nie passieren), schließt die Strategie
     sofort jedes Leg, um die Konsistenz wiederherzustellen.
4. **Schutzausstiege**
   - Jedes Leg speichert individuelle Stop-Loss- und Take-Profit-Niveaus, ausgedrückt in Preiseinheiten (`StopLossPips`,
     `TakeProfitPips`). Bei jeder fertigen Kerze prüft die Strategie, ob der Kerzenbereich eines der gespeicherten Niveaus
     gekreuzt hat, und wenn ja, wird das Leg mit einer Marktorder geschlossen.
   - Ein Trailing Stop (`TrailingStopPips`, `TrailingStepPips`) ist optional. Sobald sich der Preis zugunsten eines Legs um
     `TrailingStopPips + TrailingStepPips` bewegt, wird der Stop auf `TrailingStopPips` Pips hinter dem aktuellen Schlusskurs
     verschoben. Der Stop zieht sich nur dann enger, wenn der Preis einen zusätzlichen Fortschritt von mindestens
     `TrailingStepPips` Pips macht.
5. **Housekeeping**
   - Volumen-Befehle werden am Instrument-Volumenschritt ausgerichtet und auf das erlaubte Minimum/Maximum begrenzt. Die
     Strategie führt nur auf vollständig geformten Kerzen aus (`CandleStates.Finished`), um Doppelverarbeitung zu vermeiden.

## Parameter

| Parameter | Typ | Standard | Beschreibung |
|-----------|-----|----------|--------------|
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Zeitrahmen für Indikatorberechnungen. |
| `OrderVolume` | `decimal` | `0.1` | Basis-Lotgröße für den ersten Eintrag. |
| `StopLossPips` | `int` | `50` | Stop-Loss-Abstand in Pips (0 deaktiviert den Stop). |
| `TakeProfitPips` | `int` | `50` | Take-Profit-Abstand in Pips (0 deaktiviert das Ziel). |
| `TrailingStopPips` | `int` | `5` | Trailing-Stop-Offset in Pips. Muss positiv sein, um Trailing zu aktivieren. |
| `TrailingStepPips` | `int` | `5` | Zusätzlicher Pip-Abstand, der erforderlich ist, bevor der Trailing Stop wieder bewegt wird. |
| `StepLossingPips` | `int` | `30` | Preisrückgang in Pips, der ein neues Mittelungs-Leg auslöst. |
| `LotCoefficient` | `decimal` | `2.0` | Multiplikator, der auf das vorherige Leg-Volumen bei der Mittelung angewendet wird. |
| `SignalBar` | `int` | `0` | Anzahl abgeschlossener Bars zum Zurückblicken beim Abtasten von Indikatoren. |
| `MaPeriod` | `int` | `15` | Länge des gleitenden Durchschnitts in Bars. |
| `MaShift` | `int` | `0` | Horizontale Verschiebung (in Bars) der gleitenden Durchschnittswerte. |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | Glättungsalgorithmus für gleitenden Durchschnitt (einfach, exponentiell, geglättet, gewichtet). |
| `MaAppliedPrice` | `AppliedPriceType` | `Weighted` | Kerzenpreis als Eingabe für den gleitenden Durchschnitt. |
| `IndentPips` | `int` | `4` | Minimale Pip-Lücke zwischen Preis und gleitendem Durchschnitt vor dem Eintritt. |
| `MacdFastPeriod` | `int` | `12` | Schnelle EMA-Länge des MACD-Filters. |
| `MacdSlowPeriod` | `int` | `26` | Langsame EMA-Länge des MACD-Filters. |
| `MacdSignalPeriod` | `int` | `9` | Signallinienlänge des MACD-Filters. |
| `MacdAppliedPrice` | `AppliedPriceType` | `Weighted` | Angewendeter Preis für die MACD-Berechnung. |
| `MacdRatio` | `decimal` | `0.9` | Mindest-MACD-Haupt-/Signal-Verhältnis für den Handel. |

### Pip-Umrechnung

Alle pip-basierten Einstellungen (`StopLossPips`, `TakeProfitPips`, `TrailingStopPips`, `TrailingStepPips`, `StepLossingPips`,
`IndentPips`) werden mit dem `PriceStep` des Instruments multipliziert. Wenn das Instrument 3 oder 5 Dezimalstellen hat, wird
der Wert zusätzlich mit 10 multipliziert, um MetaTraders "Pip"-Definition für Bruchwährungsnotierungen zu reproduzieren. Wenn
kein Preisschritt verfügbar ist, wird ein Fallback-Wert von `0.0001` verwendet.

## Implementierungshinweise

- Die Strategie hält eine interne Liste von Positions-Legs, da StockSharp im Netting-Modus arbeitet. Jedes Leg verfolgt seinen
  eigenen Eintrittspreis, Stop- und Take-Niveaus, damit das Mitteln wie der originale MetaTrader EA verhält.
- Schutzaufträge werden softwareseitig simuliert: wenn eine Kerze ein Stop-Loss- oder Take-Profit-Niveau berührt, wird die
  Position mit einer Marktorder auf diesem Bar geschlossen.
- Das Mitteln wird automatisch deaktiviert, wenn `StepLossingPips` null ist. Andernfalls verwendet jedes zusätzliche Leg das
  vorherige Leg-Volumen multipliziert mit `LotCoefficient` und abgerundet auf den nächsten Volumenschritt.
- Trailing-Stop-Updates verwenden den Kerzenschlusskurs als aktuellen Preis-Proxy. Der Stop bewegt sich nie in der nachteiligen
  Richtung und bleibt inaktiv, bis der Preisfortschritt `TrailingStopPips + TrailingStepPips` überschreitet.
- Indikator-Buffer berücksichtigen die `SignalBar`- und `MaShift`-Offsets, sodass die Entscheidungslogik genau dieselben Werte
  sieht, die der MetaTrader Expert aus seinen Indikator-Buffern erhalten würde.
