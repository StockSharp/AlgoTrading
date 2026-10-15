# GBP 9 AM-Strategie für ausstehende Orders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Konvertierung des ursprünglichen MetaTrader 4-Experten mit Sitz in `MQL/7687/Gbp9am.mq4`. Es stellt die Breakout-Routine um 9 Uhr morgens in London wieder her, die zwei ausstehende Orders um den aktuellen Preis herum aktiviert und während der Sitzung höchstens einen aktiven Trade aufrechterhält.

## Handelsidee

1. Zur konfigurierten *Blickstunde* und *Minute* verwendet die Strategie den letzten Kerzenschluss als Preisschnappschuss.
2. Über dem Snapshot-Preis wird ein Kauf-Stopp und darunter ein Verkaufs-Stopp platziert. Beide Orders teilen sich das gleiche Volumen und verfügen über individuelle Stop-Loss-Level sowie eine gemeinsame Take-Profit-Distanz.
3. Wenn eine der Orders ausgeführt wird, wird die andere sofort storniert, sodass immer nur eine Position aktiv ist.
4. Die offene Position wird mit synthetischen Stop-Loss- und Take-Profit-Levels verwaltet, die bei jeder abgeschlossenen Kerze überprüft werden.
5. Es kann eine tägliche Schließungsstunde aktiviert werden, um das verbleibende Risiko einzudämmen und ausstehende Aufträge nach der Londoner Sitzung zu entfernen.
6. Wenn beide ausstehenden Aufträge ohne Handel entfernt werden oder die Marktzeit von der Beobachtungsstunde abweicht, wird die Strategie am nächsten Tag genau wie die MetaTrader-Version wieder aktiviert.

Die Pip-Offsets werden anhand der Instrumentenpreisstufe angenähert. Wenn der Broker gebrochene Pips (3 oder 5 Dezimalstellen) bereitstellt, skaliert die Logik automatisch auf typische 0,1-Pip-Schritte.

## Parameterreferenz

| Parameter | Beschreibung |
|-----------|-------------|
| `Volume` | Auftragsvolumen (Lots), aufgeteilt auf beide ausstehenden Aufträge. |
| `LookHour` | Börsenstunde, die 9:00 Uhr Londoner Zeit darstellt. |
| `LookMinute` | Minute innerhalb der Bildstunde, in der der Schnappschuss aufgenommen wird. |
| `CloseHour` | Stunde, in der alle Positionen und ausstehenden Aufträge zwangsweise geschlossen werden. |
| `UseCloseHour` | Aktiviert oder deaktiviert das tägliche Abschlussverfahren. |
| `TakeProfitPips` | Zielentfernung in Pips, symmetrisch auf beide Richtungen angewendet. |
| `BuyDistancePips` | Offset in Pips zwischen dem Snapshot-Preis und dem Buy-Stop-Eintrag. |
| `SellDistancePips` | Offset in Pips zwischen dem Snapshot-Preis und dem Sell-Stop-Eintrag. |
| `BuyStopLossPips` | Stop-Loss-Distanz in Pips für den Long-Trade. |
| `SellStopLossPips` | Stop-Loss-Distanz in Pips für den Short-Trade. |
| `CandleType` | Kerzenserie, die zur Zeitmessung und Stoppverwaltung verwendet wird (Standard 1 Minute). |

## Verhaltensnotizen

- Die Strategie ignoriert unvollendete Kerzen, um mehrere Auslöser innerhalb desselben Balkens zu vermeiden.
- Orderpreise werden mithilfe der Wertpapierpreisstufe auf den nächsten gültigen Tick gerundet.
- Das Re-Arming-Gate spiegelt die `clear_to_send`-Flagge des MQL-Experten wider: Sobald der tägliche Straddle platziert ist, werden keine neuen Orders gesendet, bis entweder beide ausstehenden Orders verschwinden, während der Markt außerhalb der Look-Hour ist, oder die Uhr die Stunde vor dem nächsten Signal erreicht.
- Wenn `UseCloseHour` aktiviert ist, beendet die Strategie jeden offenen Handel mit einer Marktorder und löscht ausstehende Orders, sobald die Schlussstunde erreicht ist.
- Die Pip-Berechnungen basieren auf historischen Kerzen, daher können die genauen Stopp-/Zielabstände geringfügig von der Tick-basierten MetaTrader-Umgebung abweichen, insbesondere bei Symbolen mit großen Spreads.

## Risikomanagement

Bei der Konvertierung bleiben die ursprünglichen statischen Stopps und Ziele erhalten. Es gibt keinen Trailing Stop oder eine Skalierungslogik. Der Positionsschutz ist in `OnStarted` aktiviert, damit unerwartete Verbindungsabbrüche das Konto nicht ungeschützt lassen.

## Nutzung

1. Konfigurieren Sie die Werte `Volume`, `LookHour` und `LookMinute` so, dass sie mit der Austauschzeitzone Ihres Daten-Feeds übereinstimmen.
2. Passen Sie die Distanzparameter an, um die Spread-Struktur Ihres Brokers widerzuspiegeln.
3. Hängen Sie die Strategie an ein GBPUSD-Symbol (oder ein anderes FX-Paar Ihrer Wahl) an und starten Sie sie vor der Londoner Sitzung.
4. Überwachen Sie die resultierenden Trades im StockSharp-Diagramm, das nach dem Start automatisch gezeichnet wird.

Die Implementierung folgt den Richtlinien von `AGENTS.md`: Sie verwendet das Kerzenabonnement auf hoher Ebene API, verwendet Strategieparameter mit UI-Metadaten und vermeidet Verlaufsabfragen auf niedriger Ebene.
