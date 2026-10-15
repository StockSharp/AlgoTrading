# Color X Derivative-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein StockSharp-Port des MetaTrader-Experten "Exp_ColorXDerivative". Sie arbeitet auf einem konfigurierbaren Kerzen-Zeitrahmen (standardmäßig 12-Stunden-Kerzen) und analysiert das ColorXDerivative-Momentum-Histogramm. Der Indikator misst, wie schnell die gewählte Kursquelle über einen festen Versatz hinweg ändert, glättet das Ergebnis mit einem gleitenden Durchschnitt und klassifiziert dann jeden Balken in einen von fünf Farbzuständen. Trades folgen derselben Logik wie im ursprünglichen EA: Der Roboter kauft, wenn sich bullisches Momentum beschleunigt oder eine bärische Bewegung beginnt sich zusammenzuziehen, und verkauft, wenn bärischer Druck zunimmt oder ein bullisches Bein an Stärke verliert.

## Indikatorlogik
1. Jede Kerze in den ausgewählten `AppliedPrice` umwandeln (Schlusskurs, Eröffnungskurs, gewichteter Schlusskurs, Demark usw.).
2. Die Kursderivate berechnen: `(price[0] - price[shift]) * 100 / shift`, wobei `shift = DerivativePeriod`.
3. Die Derivate mit der ausgewählten Methode glätten (`SMA`, `EMA`, `SMMA`, `LWMA` oder `Jurik`). Der standardmäßige Jurik-gleitende Durchschnitt reproduziert die JJMA-Glättung der MQL-Implementierung.
4. Einen Farbzustand zuweisen:
   - **0** – Derivate &gt; 0 und steigend (starke bullische Beschleunigung).
   - **1** – Derivate &gt; 0, aber fallend (bullisches Momentum verliert Stärke).
   - **2** – Derivate ≈ 0 (neutral).
   - **3** – Derivate &lt; 0, aber steigend (bärische Bewegung zieht sich zusammen).
   - **4** – Derivate &lt; 0 und fallend (bärische Beschleunigung).

Ein Signalversatz steuert, welcher abgeschlossene Balken ausgewertet wird (1 = letzter geschlossener Balken, 2 = vorheriger Balken usw.).

## Handelsregeln
- **Long-Einstieg**: aktiviert wenn `EnableLongEntry` wahr ist und:
  - die aktuelle Farbe 0 ist, während die vorherige Farbe nicht 0 war (Momentum dreht scharf bullisch), oder
  - die aktuelle Farbe 3 ist, während die vorherige Farbe 4 oder 2 war (bärische Bewegung beginnt sich zusammenzuziehen).
- **Short-Einstieg**: aktiviert wenn `EnableShortEntry` wahr ist und:
  - die aktuelle Farbe 4 ist, während die vorherige Farbe nicht 4 war (bärische Beschleunigung beginnt), oder
  - die aktuelle Farbe 1 ist, während die vorherige Farbe 0 oder 2 war (bullische Bewegung lässt nach).
- **Long-Ausstieg**: ausgelöst wenn die aktuelle Farbe 1 oder 4 ist und `EnableLongExit` wahr ist.
- **Short-Ausstieg**: ausgelöst wenn die aktuelle Farbe 0 oder 3 ist und `EnableShortExit` wahr ist.

Aufträge werden als Marktaufträge mit dem `OrderVolume`-Parameter gesendet. Positionsschließungen werden vor neuen Einstiegen ausgeführt, um die sequentielle Logik des ursprünglichen EAs nachzuahmen.

## Risikomanagement
Optionale Stop-Loss- und Take-Profit-Abstände werden über `StopLossTicks` und `TakeProfitTicks` bereitgestellt. Wenn einer der Werte über null liegt, ruft die Strategie `StartProtection` auf und konvertiert Ticks in Kursschritte mithilfe der `Step`-Größe des Wertpapiers. Der Stop-/Zielschutz läuft einmal und ist mit Auto-Trading oder Backtesting kompatibel.

## Parameter
- `OrderVolume` – Marktordergröße.
- `CandleType` – Zeitrahmen für die Indikatorberechnungen (Standard: 12-Stunden-Zeitrahmen).
- `DerivativePeriod` – Abstand in Balken für den Derivatversatz.
- `AppliedPrice` – Kursquelle für die Derivate (Schlusskurs, Median, gewichtet, Demark usw.).
- `SmoothingMethod` – Glättungsfilter auf die Derivate angewendet. Unterstützte Werte: SMA, EMA, SMMA, LWMA, Jurik.
- `SmoothingLength` – Periode des Glättungsfilters.
- `SignalShift` – Wie viele abgeschlossene Balken zurück die Farbwerte gelesen werden (1 = neuester geschlossener Balken).
- `StopLossTicks` / `TakeProfitTicks` – optionale Schutzabstände in Wertpapierschritten.
- `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit` – Schalter entsprechend den ursprünglichen EA-Eingaben.

## Hinweise
- Die Strategie reproduziert die indikatorgesteuerte Logik des MetaTrader-EAs ohne zusätzliche Geldverwaltungsfunktionen.
- Jurik-Glättung ist die engste Annäherung an den im MQL-Bibliothek verwendeten JJMA-Filter; andere Optionen werden auf die Standard-StockSharp-gleitenden Durchschnitte abgebildet.
- Die Farbhistorie wird intern gespeichert, sodass die Optimierung von `SignalShift` genauso wie in der MetaTrader-Version funktioniert.
