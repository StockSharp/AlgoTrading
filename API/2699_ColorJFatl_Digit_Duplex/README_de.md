# Color JFATL Digit Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Color JFATL Digit Duplex-Strategie ist ein Zwei-Modul-System, das aus dem MetaTrader 5 Expert Advisor `Exp_ColorJFatl_Digit_Duplex` konvertiert wurde. Sie betreibt zwei unabhängige Signalströme basierend auf dem Color Jurik Fast Adaptive Trend Line (JFATL) Indikator. Das Long-Modul sucht nach bullischen Übergängen in der Farbkarte des Indikators, während das Short-Modul auf bärische Übergänge reagiert. Jede Seite hat ihre eigenen Glättungsparameter, Preisquelle, Rundungsgenauigkeit, Balkenverschiebung und Schutz-Offsets.

Die StockSharp-Implementierung verwendet die High-Level-API mit Kerzensubskriptionen und einer dedizierten Indikatorklasse, die die FATL-Kernelgewichte und Jurik-Glättung reproduziert. Der Indikator gibt den gerundeten JFATL-Wert zusammen mit den aktuellen und vorherigen Farbcodes aus, die für die Signalerkennung benötigt werden.

## Indikatorlogik
1. **FATL-Faltung** – die letzten 39 Preise (ausgewählt durch die angewendete Preisoption) werden mit den ursprünglichen FATL-Koeffizienten gewichtet, um eine gefilterte Reihe zu erzeugen.
2. **Jurik-Glättung** – der FATL-Ausgang wird durch einen Jurik Moving Average (JMA) geleitet. Der Phasenparameter wird durch Anwenden einer Differentialanpassung emuliert, die den geglätteten Wert vor- oder zurückschiebt.
3. **Ziffernrundung** – das Ergebnis wird auf die angegebene Anzahl von Ziffern gerundet, um den "digitalisierten" Ausgang des ursprünglichen Indikators nachzuahmen.
4. **Farbzuweisung** – der Farbpuffer wird auf 2 gesetzt, wenn der aktuelle Wert steigt, auf 0 wenn er fällt, und erbt andernfalls die vorherige Farbe. Ein konfigurierbarer `SignalBar`-Parameter wählt aus, welcher historische Balken zusammen mit seinem vorherigen Balken untersucht werden soll.

Der Indikator gibt einen komplexen Wert zurück, der die gerundete JFATL-Ablesung, die Farbe bei `SignalBar`, die vorherige Farbe und die Schließzeit des Signalbalkens enthält. Strategie-Handler verwenden diese Information, um Zustandsübergänge genau wie im MetaTrader-Code zu identifizieren.

## Handelsregeln
- **Long-Modul**
  - Eröffnet eine Long-Position, wenn die Farbe bei `SignalBar` auf 2 wechselt, während die vorherige Farbe nicht 2 war und keine Long-Exposition vorhanden ist.
  - Schließt eine bestehende Long-Position, wenn die Farbe bei `SignalBar` 0 wird.
- **Short-Modul**
  - Eröffnet eine Short-Position, wenn die Farbe bei `SignalBar` auf 0 wechselt, während die vorherige Farbe über 0 lag und keine Short-Exposition vorhanden ist.
  - Schließt eine bestehende Short-Position, wenn die Farbe bei `SignalBar` 2 wird.
- **Positionshandling** – Aufträge werden so dimensioniert, dass die entgegengesetzte Exposition eliminiert wird, bevor ein neuer Trade auf der anderen Seite eröffnet wird. `ClosePosition()` wird für Ausstiege verwendet, sodass die Strategie jederzeit eine einzelne Nettoposition aufrechterhält.

## Risikomanagement
Jedes Modul hat individuelle Stop-Loss- und Take-Profit-Abstände in Preisschritten. Wenn eine neue Position eröffnet wird, zeichnet die Strategie den Einstiegspreis auf und berechnet die Schutzlevel mit dem aktuellen `PriceStep` des Wertpapiers. Bei jeder Indikatoraktualisierung wird das entsprechende Kerzenhoch/-tief gegen die gespeicherten Level getestet:

- Bei Long-Trades schließt die Strategie die Position, wenn das Kerzentief den Stop-Preis erreicht oder das Kerzenhoch den Take-Profit-Preis erreicht.
- Bei Short-Trades wird die Logik mit Kerzenhoch für den Stop und Kerzentief für den Take-Profit gespiegelt.

Das Deaktivieren des Stops oder Takes durch Setzen des Abstands auf null lässt den Trade unverwaltet, bis der Indikator ein Ausstiegssignal ausgibt.

## Parameter
| Gruppe | Parameter | Beschreibung |
| --- | --- | --- |
| Allgemein | `LongCandleType` | Zeitrahmen für die Long-Indikatorsubskription. |
| Allgemein | `ShortCandleType` | Zeitrahmen für die Short-Indikatorsubskription. |
| Indikator (Long) | `LongJmaLength` | Jurik-Moving-Average-Länge für das Long-Modul. |
| Indikator (Long) | `LongJmaPhase` | Phasenanpassung am langen JMA-Ausgang (Bereich −100…100). |
| Indikator (Long) | `LongAppliedPrice` | Angewendete Preisquelle in der FATL-Faltung. |
| Indikator (Long) | `LongDigit` | Anzahl der Ziffern zur Rundung des Indikatorwerts. |
| Indikator (Long) | `LongSignalBar` | Historischer Balkenoffset für Signale (0 = aktuell geschlossener Balken). |
| Risiko (Long) | `LongStopLossPoints` | Stop-Loss-Abstand für Longs in Preisschritten. |
| Risiko (Long) | `LongTakeProfitPoints` | Take-Profit-Abstand für Longs in Preisschritten. |
| Handel (Long) | `EnableLongOpen` | Aktiviert oder deaktiviert neue Long-Einstiege. |
| Handel (Long) | `EnableLongClose` | Aktiviert oder deaktiviert Long-Ausstiege durch den Indikator. |
| Indikator (Short) | `ShortJmaLength` | Jurik-Moving-Average-Länge für das Short-Modul. |
| Indikator (Short) | `ShortJmaPhase` | Phasenanpassung am kurzen JMA-Ausgang. |
| Indikator (Short) | `ShortAppliedPrice` | Angewendete Preisquelle für den Short-Indikator. |
| Indikator (Short) | `ShortDigit` | Anzahl der Ziffern zur Rundung des Short-Indikatorwerts. |
| Indikator (Short) | `ShortSignalBar` | Historischer Balkenoffset für Short-Signale. |
| Risiko (Short) | `ShortStopLossPoints` | Stop-Loss-Abstand für Shorts in Preisschritten. |
| Risiko (Short) | `ShortTakeProfitPoints` | Take-Profit-Abstand für Shorts in Preisschritten. |
| Handel (Short) | `EnableShortOpen` | Aktiviert oder deaktiviert neue Short-Einstiege. |
| Handel (Short) | `EnableShortClose` | Aktiviert oder deaktiviert Short-Ausstiege durch den Indikator. |

## Verwendungshinweise
1. Weisen Sie den Long- und Short-Modulen geeignete Kerzentypen zu. Diese können auf verschiedene Zeitrahmen zeigen, wenn gewünscht.
2. Konfigurieren Sie den angewendeten Preis und die Rundungsziffern, um die Instrumenteigenschaften des ursprünglichen Expert Advisors anzupassen.
3. Der `SignalBar`-Parameter steuert, wie viele geschlossene Kerzen zurück das Signal validiert wird. Setzen Sie ihn auf 1, um den MT5-Standard (vorherige abgeschlossene Kerze) zu replizieren.
4. Stellen Sie sicher, dass die `Volume`-Eigenschaft der Strategie die gewünschte Handelsgröße widerspiegelt. Beim Umkehren von Positionen addiert die Strategie automatisch die Größe der bestehenden Exposition, damit die Nettoposition korrekt kippt.
5. Stops und Ziele basieren auf dem `PriceStep` des Wertpapiers. Für Instrumente ohne definierten Tick-Preis standardmäßig auf rohe numerische Schritte.

## Konvertierungshinweise
- Der Jurik-Phasenparameter in StockSharp wird durch Anwenden einer Vorlauf/Nachlauf-Differentialanpassung emuliert, da der paketierte `JurikMovingAverage` keine direkte Phaseneigenschaft freilegt. Dies bewahrt das Verhalten des ursprünglichen Experten, einschließlich aggressiver oder verzögerter Reaktionen.
- Die Strategie verwendet ein einzelnes Nettopositionsmodell. Die MetaTrader-Version konnte mehrere Aufträge pro Richtung ausführen; in StockSharp konsolidiert die Logik sie in jeweils eine Long- oder Short-Exposition.
- Schutzlevel werden bei jedem Indikatorkerzenabschluss statt auf jedem Tick ausgewertet. Dies entspricht der Signalfrequenz des MT5-Experten und hält die Implementierung innerhalb der High-Level-API-Richtlinien.

## Dateien
- `CS/ColorJfatlDigitDuplexStrategy.cs` – Strategieimplementierung mit dem benutzerdefinierten Indikator.
- `README.md` / `README_zh.md` / `README_ru.md` – Dokumentation auf Englisch, Chinesisch und Russisch.
