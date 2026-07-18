# MA MACD Positionsmittelungs-Strategie v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **MA MACD Positionsmittelungs-Strategie v2** ist eine direkte Übersetzung des MetaTrader Expert Advisors von Vladimir
Karputov. Sie kombiniert einen gewichteten gleitenden Durchschnittsfilter, einen MACD-Bestätigungsblock und ein Mittelungsmodul,
das die Exposition erhöht, wenn bestehende Trades gegen die Position laufen. Die StockSharp-Version behält die ursprüngliche
Signalhierarchie bei, verarbeitet Indikatoren auf fertigen Kerzen und verwaltet Schutzlogik (Stop-Loss, Take-Profit, Trailing)
im Code, um das brokerseitige Verhalten aus MQL zu reproduzieren.

## Handelslogik
1. **Indikatorvorbereitung**
   - Ein konfigurierbarer gleitender Durchschnitt berechnet auf dem ausgewählten Kerzentyp und Preiskomponenten. Der Parameter
     `MaShift` emuliert MetaTraders Vorwärtsverschiebung durch Lesen von Werten aus älteren Kerzen, während `BarOffset` die
     Bewertung der aktuellen oder einer vorherigen Bar ermöglicht.
   - Ein MACD-Signalindikator produziert die Haupt- und Signallinie unter Verwendung anpassbarer schneller, langsamer und
     Signal-Perioden und eines angewendeten Preises, der dem ursprünglichen Expert Advisor entspricht.
2. **Signalvalidierung**
   - Long-Setups erfordern, dass beide MACD-Linien negativ sind, der Preis über dem verschobenen gleitenden Durchschnitt liegt
     und der Preisabstand zum Durchschnitt `MaIndentPips` überschreitet (umgerechnet in absoluten Preis unter Verwendung der
     Pip-Größe des Instruments).
   - Short-Setups spiegeln die Bedingungen: Beide MACD-Linien müssen positiv sein, der Preis muss unter dem verschobenen
     gleitenden Durchschnitt bleiben und die Lücke zum Durchschnitt muss mindestens `MaIndentPips` betragen.
   - Der Verhältnisfilter `MacdRatio` erzwingt `MACD_main / MACD_signal >= MacdRatio` (unter Verwendung absoluter
     Dezimaldivision), bevor ein Trade erlaubt ist.
   - Wenn `ReverseSignals = true`, wird die Richtung der Marktorder invertiert, nachdem alle Bedingungen erfüllt sind.
3. **Positions-Lebenszyklus**
   - Wenn **keine Position** besteht, öffnet die Strategie eine Marktorder mit dem konfigurierten `OrderVolume` (gerundet
     nach dem Instrument-Volumenschritt) in der berechneten Richtung. Stop-Loss- und Take-Profit-Niveaus werden sofort
     gemäß `StopLossPips` und `TakeProfitPips` angewendet.
   - Wenn **bereits eine Exposition besteht**, öffnet die Strategie nie die Gegenseite. Stattdessen:
     - Schließt alles, wenn Longs und Shorts gleichzeitig erkannt werden (Sicherheitsnetz, das die MQL-Prüfung spiegelt), oder
     - Ruft den Mittelungsblock für die aktuelle Seite auf.
4. **Mittelungsmodul**
   - Für Longs findet der Algorithmus das günstigste offene Leg, dessen nicht realisierter Verlust `StepLossPips` überschreitet.
     Für Shorts wählt er das verlustreichste Leg mit dem höchsten Preis.
   - Sobald ein Kandidat gefunden ist, wird eine neue Marktorder mit Volumen `CandidateVolume × LotCoefficient` gesendet
     (nach Anpassung an den erlaubten Volumenschritt/Min/Max). Dies reproduziert die geometrische Progression des ursprünglichen
     Experten.
   - Neue Legs erben dieselben Stop-Loss- und Take-Profit-Abstände und werden für Trailing-Updates geeignet.
5. **Risikokontrollen**
   - Ein Trailing Stop aktiviert sich nur, wenn sowohl `TrailingStopPips` als auch `TrailingStepPips` größer als null sind.
     Für Longs bewegt sich der Stop auf `Close - TrailingStopPips`, sobald der Gewinn `TrailingStopPips + TrailingStepPips`
     überschreitet; Shorts verhalten sich symmetrisch.
   - Manuelle Stop-Loss- und Take-Profit-Prüfungen werden bei jeder fertigen Kerze durchgeführt. Wenn ausgelöst, schließt
     eine Marktorder das genaue Leg und entfernt es aus der Mittelungsliste.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| **OrderVolume** | Basisvolumen für den allerersten Trade in einem Zyklus. |
| **StopLossPips** | Stop-Loss-Abstand in Pips. Auf null setzen, um den Stop zu deaktivieren. |
| **TakeProfitPips** | Take-Profit-Abstand in Pips. Auf null setzen, um das Ziel zu deaktivieren. |
| **TrailingStopPips** | Abstand zwischen Preis und Trailing Stop. Funktioniert zusammen mit `TrailingStepPips`. |
| **TrailingStepPips** | Zusätzliche günstige Bewegung, die erforderlich ist, bevor der Trailing Stop aktualisiert wird. |
| **StepLossPips** | Mindestverlust (in Pips), bevor ein Mittelungs-Leg hinzugefügt wird. |
| **LotCoefficient** | Multiplikator, der auf das ausgewählte verlustsreiche Leg-Volumen bei der Mittelung angewendet wird. |
| **BarOffset** | Anzahl der Bars zurück zum Lesen von Indikatorwerten (0 = aktuelle fertige Bar). |
| **ReverseSignals** | Invertiert die Long/Short-Ausführung unter Beibehaltung derselben Filter. |
| **MaPeriod** | Periode des gleitenden Durchschnitts. |
| **MaShift** | Vorwärtsverschiebung des gleitenden Durchschnitts (MetaTrader-Stil). |
| **MaMethod** | Glättungsmethode des gleitenden Durchschnitts (Einfach, Exponentiell, Geglättet, Gewichtet). |
| **MaPrice** | Kerzenpreiskomponente für den gleitenden Durchschnitt. |
| **MaIndentPips** | Mindestpreisabstand vom gleitenden Durchschnitt vor dem Einstieg. |
| **MacdFastPeriod** | Schnelle EMA-Periode für MACD. |
| **MacdSlowPeriod** | Langsame EMA-Periode für MACD. |
| **MacdSignalPeriod** | Signal-EMA-Periode für MACD. |
| **MacdPrice** | Angewendeter Preis in der MACD-Berechnung. |
| **MacdRatio** | Mindestverhältnis zwischen MACD-Haupt- und Signallinie. |
| **CandleType** | Kerzenreihe für alle Berechnungen. |

## Implementierungshinweise
- Die Pip-Größe wird aus dem Preisschritt des Instruments berechnet, was die 3/5-stellige Anpassung aus der MQL-Version
  reproduziert. Dies hält pip-basierte Abstände über Forex-Symbole hinweg identisch.
- Alle Indikator-Buffer verwenden Warteschlangen, um MetaTraders `ma_shift`- und `bar`-Indizierung zu emulieren, ohne
  historische Suchmethoden aufzurufen, die durch die Projektregeln verboten sind.
- Volumenanpassungen respektieren `Security.VolumeStep`, `Security.MinVolume` und `Security.MaxVolume`, um ungültige
  Ordergrößen zu verhindern, wenn `LotCoefficient` die Exposition multipliziert.
- Schutzlogik (Stops, Takes, Trailing) läuft vollständig in der Strategieschicht, sodass keine Abhängigkeit von brokerseitigen
  Positionsmodifikations-APIs besteht.
- Die Klasse befindet sich im Namespace `StockSharp.Samples.Strategies` und folgt der Repository-Anforderung, Tab-Einrückung
  und ausschließlich englische Kommentare zu verwenden.
