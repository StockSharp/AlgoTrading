# AnyRange Cloud Tail System Tm Plus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert das Verhalten des Expert-Advisors **Exp_i-AnyRangeCldTail_System_Tm_Plus.mq5** unter Verwendung der High-Level-API von StockSharp. Sie erstellt eine benutzerdefinierte Intraday-Range zwischen zwei benutzerdefinierten Zeiten, wartet auf Ausbrüche über diese Range hinaus und plant Orders eine konfigurierbare Anzahl von Bars nach dem Ausbruch, sodass die Signale mit der ursprünglichen MQL-Timing-Logik übereinstimmen.

Die Strategie ist für Long- und Short-Trading ausgelegt. Sie exponiert Parameter, die Ausbruchsberechtigungen, Stop-Loss/Take-Profit-Abstände in Preisschritten, den Haltezeitraum und das Indikator-Berechnungsfenster steuern. Zusätzlich schließt ein zeitbasierter Ausstieg Positionen, die länger als die konfigurierte Minutenzahl offen bleiben, und entspricht der Schutzlogik des Quell-Expert-Advisors.

## Handelslogik

1. **Range-Konstruktion**
   - Zwei Zeitstempel (`RangeStartTime` und `RangeEndTime`) definieren das Sitzungsfenster zur Berechnung der Referenz-Range.
   - Für jeden abgeschlossenen Tag zeichnet die Strategie das höchste Hoch und das niedrigste Tief zwischen diesen Zeitstempeln auf. Wenn `RangeStartTime` größer als `RangeEndTime` ist, überspannt das Fenster automatisch Mitternacht, genau wie der ursprüngliche Indikator.
   - Die zuletzt abgeschlossene Range wird wiederverwendet, bis eine neue Tages-Range abgeschlossen ist.

2. **Ausbruchserkennung**
   - Jede abgeschlossene Kerze wird mit der gespeicherten Range verglichen.
   - Kerzen, die über dem Range-Hoch schließen, erhalten dieselben Farbcodes (2 oder 3) wie der MQL-Indikator, während Kerzen, die unter dem Range-Tief schließen, Codes 0 oder 1 erhalten. Kerzen innerhalb der Range werden mit Code 4 (kein Signal) markiert.
   - Der `SignalBar`-Parameter verschiebt den Inspektionspunkt: Die Strategie wertet die Kerze aus, die `SignalBar + 1` Bars alt ist, und bestätigt, dass die neuere Kerze (`SignalBar`) nicht dieselbe Farbe trägt. Dies reproduziert die verzögerte Bestätigung, die vom EA verwendet wird, um Orders eine Bar nach der Ausbruchskerze auszulösen.

3. **Einstiege**
   - **Long**: erlaubt wenn `AllowBuyEntry` wahr ist und eine bullische Farbe (2 oder 3) auf der Signalbar erkannt wird, während die folgende Bar die Ausbruchsfarbe nicht wiederholt.
   - **Short**: erlaubt wenn `AllowSellEntry` wahr ist und eine bärische Farbe (0 oder 1) auf der Signalbar erkannt wird, während die folgende Bar die Ausbruchsfarbe nicht wiederholt.
   - Wenn eine entgegengesetzte Position offen ist, wird deren Volumen zur neuen Marktorder hinzugefügt, damit die Position sofort dreht und das Verhalten der Hilfsfunktionen in `TradeAlgorithms.mqh` emuliert wird.

4. **Ausstiege**
   - **Entgegengesetztes Signal**: wenn `AllowBuyExit` aktiviert ist, schließt eine bärische Farbe (0 oder 1) auf der Signalbar Long-Positionen. Wenn `AllowSellExit` aktiviert ist, schließt eine bullische Farbe (2 oder 3) Short-Positionen.
   - **Zeitausstieg**: wenn `UseTimeExit` wahr ist, werden Positionen nach `ExitAfterMinutes` Minuten ab dem Einstieg liquidiert, entsprechend dem MQL-Loop, der Positionen scannt und sie nach `nTime` Minuten schließt.
   - **Stops/Ziele**: optionale Stop-Loss- und Take-Profit-Schutzmaßnahmen werden über `StopLossPoints` und `TakeProfitPoints` konfiguriert. Werte werden mit dem Preisschritt des Wertpapiers in Preisabstände umgerechnet und spiegeln die ursprüngliche punktbasierte Konfiguration wider.

5. **Risikokontrollen**
   - Orders verwenden das konfigurierte `OrderVolume` (Basisgröße in Instrument-Volumeneinheiten). Die Ordergröße wird bei jedem `BuyMarket`/`SellMarket`-Aufruf angewendet und beim Wechsel der Position angepasst.
   - Stop-Loss und Take-Profit werden vom integrierten `StartProtection`-Helfer verwaltet, der OCO-Schutzmaßnahmen direkt nach dem Start der Strategie registriert.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Basisordergröße für neue Positionen. | `0.1` |
| `AllowBuyEntry` | Long-Einstiege bei bullischen Ausbrüchen erlauben. | `true` |
| `AllowSellEntry` | Short-Einstiege bei bärischen Ausbrüchen erlauben. | `true` |
| `AllowBuyExit` | Long-Positionen bei bärischen Ausbrüchen schließen. | `true` |
| `AllowSellExit` | Short-Positionen bei bullischen Ausbrüchen schließen. | `true` |
| `UseTimeExit` | Zeitbasierten Ausstieg aktivieren. | `true` |
| `ExitAfterMinutes` | Haltezeit in Minuten bevor der Zeitausstieg auslöst. | `1500` |
| `StopLossPoints` | Stop-Loss-Abstand in Preisschritten. `0` zum Deaktivieren. | `1000` |
| `TakeProfitPoints` | Take-Profit-Abstand in Preisschritten. `0` zum Deaktivieren. | `2000` |
| `SignalBar` | Anzahl der zurück inspizierten Bars für die Ausbruchserkennung (entspricht dem MQL `SignalBar`). | `1` |
| `RangeLookbackDays` | Maximale Anzahl vergangener Sitzungen zur Suche einer abgeschlossenen Range. `0` um immer nur die neueste Range zu verwenden. | `1` |
| `RangeStartTime` | Beginn des Range-Aufbaufensters (TimeSpan). | `02:00` |
| `RangeEndTime` | Ende des Range-Aufbaufensters (TimeSpan). | `07:00` |
| `CandleType` | Kerzen-Datentyp/-Zeitrahmen für Berechnungen. | `30 Minuten` |

## Implementierungshinweise

- Die Klasse verwendet `SubscribeCandles` und die ereignisgesteuerte `WhenNew`-Pipeline, um nur abgeschlossene Kerzen zu verarbeiten, und stellt sicher, dass Entscheidungen dem MQL-Expert-Advisor entsprechen, der sich auf `IsNewBar`-Prüfungen stützte.
- Range-Werte werden in leichtgewichtigen Structs gespeichert und der Algorithmus vermeidet LINQ über vollständige Sammlungen, um den Projektrichtlinien zu entsprechen.
- Der Zeitausstieg speichert den Einstiegszeitstempel für die aktuell offene Richtung und spiegelt wider, wie der Quellcode offene Positionen iterierte.
- Das Ordervolumen ist mit der Basis-`Strategy.Volume`-Eigenschaft synchronisiert, sodass die StockSharp-UI die konfigurierte Größe widerspiegelt.
- Der Code enthält englische Kommentare, die jeden Hauptabschnitt erläutern, um Wartung und weitere Anpassungen zu erleichtern.

## Verwendungshinweise

- Stellen Sie sicher, dass der Datenfeed Kerzen liefert, die mit dem gewählten `CandleType` übereinstimmen. Die Ausbruchserkennung basiert auf abgeschlossenen Kerzen; Tick-basierte oder teilweise geformte Bars sollten nicht verarbeitet werden.
- Passen Sie `RangeStartTime` und `RangeEndTime` beim Handel an Märkten mit unterschiedlichen Handelssitzungen an, um den Akkumulationszeitraum abzudecken, der am besten zum Basisinstrument passt.
- Wenn das Instrument einen unregelmäßigen Preisschritt hat, überprüfen Sie die `StopLossPoints`/`TakeProfitPoints`-Konvertierung durch Inspektion der generierten Schutzorders im Chart oder Orderprotokoll.
- Reduzieren Sie `ExitAfterMinutes` bei schnelleren Zeitrahmen, um zu vermeiden, dass Positionen länger als beabsichtigt gehalten werden.
