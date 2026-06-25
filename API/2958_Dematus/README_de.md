# Dematus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Dematus-Strategie repliziert die Logik des ursprünglichen MetaTrader 5-Expertenberaters "Dematus". Sie verwendet den DeMarker-Oszillator zur Erkennung von Momentum-Umkehrungen und unterstützt Pyramiding mit adaptiver Positionsgrößen. Die Strategie ist für ein einzelnes Instrument ausgelegt und handelt auf der Kerzen-Serie, die durch den Parameter `CandleType` definiert ist.

Bei jeder abgeschlossenen Kerze werden zwei DeMarker-Werte ausgewertet: der aktuellste Wert und der Wert von zwei Balken zuvor. Ein Kreuzungspunkt vom überverkauften Schwellenwert (0.3) nach oben signalisiert Long-Gelegenheiten, während ein Kreuzungspunkt vom überkauften Schwellenwert (0.7) nach unten Short-Gelegenheiten signalisiert. Nach einem initialen Einstieg kann die Strategie zur Position hinzufügen, wenn der Preis eine konfigurierbare Distanz vom letzten ausgeführten Einstiegspreis zurücklegt und das DeMarker-Signal erneut auslöst.

## Handelsregeln
- **Primäreinstieg:**
  - Long-Position eröffnen wenn der DeMarker-Wert von zwei Balken zuvor unter 0.3 liegt und der aktuelle Wert über 0.3 steigt, vorausgesetzt keine offene Position existiert.
  - Short-Position eröffnen wenn der DeMarker-Wert von zwei Balken zuvor über 0.7 liegt und der aktuelle Wert unter 0.7 fällt, vorausgesetzt keine offene Position existiert.
- **Skalierungslogik:**
  - Während eine Position aktiv ist, merkt sich die Strategie den genauen Preis des letzten Fills. Wenn sich der Preis mindestens `DistancePips` (in Preiseinheiten umgerechnet) gegen die Position bewegt und die entsprechende DeMarker-Kreuzung erneut auftritt, reicht die Strategie eine zusätzliche Order in der gleichen Richtung ein.
  - Die Größe jeder zusätzlichen Order ist das vorherige ausgeführte Volumen multipliziert mit `VolumeMultiplier`, gerundet auf den Instrument-Volumenschritt und durch die Börsenlimits begrenzt. Dies spiegelt das Lot-Koeffizient-Verhalten des ursprünglichen Expertenberaters wider.
- **Stop-Management:**
  - Jedem neuen Position wird ein initialer Stop-Loss mit `StopLossPips` beigefügt. Das Stop-Level wird nach jedem Skalierungs-Trade neu berechnet, damit die konsolidierte Netto-Position immer ein gültiges Schutzlevel hat.
  - Wenn `TrailingStopPips` aktiviert ist, wird das Stop-Level enger, wenn der offene Gewinn `TrailingStopPips + TrailingStepPips` übersteigt, was die Trailing-Stop-Logik der MQL-Implementierung emuliert.
- **Eigenkapitalschutz:**
  - Im Flat-Zustand definiert die Strategie einen virtuellen Eigenkapitalboden gleich `Balance - VirtualStopEquity`.
  - Sobald das schwebende Eigenkapital um mindestens `TrailingStartEquity` steigt, wird ein Trailing-Eigenkapitalstopp aktiviert und folgt dem Peak-Eigenkapital minus `TrailingEquity`.
  - Wenn das Konto-Eigenkapital unter den virtuellen Boden fällt, während eine Position offen ist, werden alle Positionen sofort liquidiert.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `InitialVolume` | Basis-Ordergröße für den allerersten Trade. Wird erneut verwendet, wenn die Position vollständig geschlossen ist. |
| `DemarkerLength` | Periode des DeMarker-Indikators. |
| `StopLossPips` | Schutzender Stop-Abstand in Pips, angewendet auf jeden Einstieg. Auf null setzen zum Deaktivieren des statischen Stop-Loss. |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. Auf null setzen zum Deaktivieren des Trailings. |
| `TrailingStepPips` | Zusätzliche günstige Bewegung (in Pips) erforderlich bevor der Trailing-Stop bewegt wird. Muss positiv sein wenn Trailing aktiv ist. |
| `DistancePips` | Mindest-Preisabstand (in Pips) vom letzten Fill bevor in die Position skaliert wird. |
| `TrailingEquity` | Abstand zwischen dem Eigenkapital-Peak und dem schützenden Eigenkapitalboden. |
| `VirtualStopEquity` | Initialer Puffer unter Balance zur Berechnung des virtuellen Eigenkapitalbodens wenn die Strategie flach ist. |
| `TrailingStartEquity` | Gewinn-Schwellenwert über Balance, der das Eigenkapital-Trailing aktiviert. |
| `VolumeMultiplier` | Multiplikator auf die Größe der letzten ausgeführten Order beim Pyramiding. |
| `ResetEntryPrice` | Wenn aktiviert, wird der gespeicherte Einstiegspreis nach jedem Ausstieg gelöscht, was Skalierung verhindert bis ein neuer Trade auftritt. |
| `CandleType` | Kerzen-Datentyp (Zeitrahmen) für Indikator-Berechnungen und Signalgenerierung. |

## Implementierungshinweise
- Die Strategie wird mit der High-Level-StockSharp-API implementiert. Kerzen-Abonnements werden über `SubscribeCandles` verwaltet, und der DeMarker-Indikator wird über `Bind` gebunden, damit Indikatorwerte als gebrauchsfertige Dezimalzahlen ankommen.
- Der Indikatorstatus wird mit einfachen skalaren Variablen verfolgt: der aktuellste Wert, der vorherige Wert und der Wert von zwei Balken zurück, was genau das Buffer-Zugriffsmuster des MQL-Quellcodes widerspiegelt (`iDeMarkerGet(0)` und `iDeMarkerGet(2)`).
- Order-Volumen werden gemäß dem Instrument-Volumenschritt gerundet und gegen Mindest- und Höchstlimits validiert, um Ablehnungen zu verhindern.
- Eigenkapitalkontrolle verwendet `Portfolio.CurrentValue`, um die im Originalcode vorhandenen Balance/Eigenkapital-Prüfungen zu spiegeln. Wenn der eigenkapitalbasierte Stop auslöst, schließt die Strategie alle offenen Positionen durch Marktorders.
- Die Pip-Größe wird aus `Security.PriceStep` abgeleitet. Instrumente mit drei oder fünf Dezimalstellen erhalten automatisch die zehnfache Anpassung, die in der MQL-Version verwendet wird, um Punkte in Pips umzurechnen.

## Verwendungshinweise
- Stellen Sie sicher, dass das verbundene Portfolio aktuelle Eigenkapitalinformationen liefert, damit die Eigenkapital-Trailing-Logik korrekt funktioniert.
- Die Strategie arbeitet nur mit fertigen Kerzen (`CandleStates.Finished`). Sie ignoriert teilweise gebildete Balken und entspricht der "neuer Balken"-Gating-Logik des ursprünglichen Expertenberaters.
- Standard-Schwellenwerte (0.3/0.7) sind im Code eingebettet, können aber bei Bedarf durch Modifizierung der Konstanten angepasst werden.
- Die Strategie unterstützt Live-Trading und Backtesting. Für Backtests überprüfen Sie, ob der Portfolio-Simulator Eigenkapitalwerte liefert, um die Ausführung der Trailing-Eigenkapital-Logik zu ermöglichen.
