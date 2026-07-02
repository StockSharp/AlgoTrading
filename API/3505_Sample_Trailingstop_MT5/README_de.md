# SampleTrailingstop MT5-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **SampleTrailingstopMt5Strategy** reproduziert das Verhalten des MetaTrader 5 Expertenberaters `SampleTrailingstop-MT5.mq5` unter Verwendung der hohen Ebene API von StockSharp. Die Strategie verwaltet ständig gepaarte Breakout-Stop-Orders, schützt gefüllte Positionen mit speziellen Exit-Orders und wendet einen Trailing-Stop an, sobald der Handel profitabel wird. Alle Berechnungen basieren auf der Preisstufe des Instruments, sodass die Logik mit der ursprünglichen, auf „Punkten“ basierenden Implementierung übereinstimmt.

## Handelslogik
1. **Datenfeed**. Die Strategie abonniert Kurse der Stufe 1, um die besten Geld-/Briefkurse zu erhalten, die die Order- und Trailing-Stop-Aktualisierungen vorantreiben.
2. **Eintrittsbefehle**.
   - Eine Kauf-Stopp-Order wird mit `BuyStop` über dem aktuellen Markt platziert. Die Bestellung wird erst aktualisiert, wenn die vorherige Instanz abgeschlossen ist.
   - Eine Verkaufsstopp-Order spiegelt den Long-Einstieg mit `SellStop` unter dem Marktwert wider.
   - Beide Einstiegsaufträge haben das gleiche konfigurierbare Volumen sowie die gleichen Stop-Loss- und Take-Profit-Abstände. Bestellungen erhalten außerdem eine Ablaufzeit einen Tag im Voraus, passend zur MQL-Implementierung.
3. **Positionsschutz**.
   - Nach der Ausführung verfolgt die Strategie die Netto-Signed-Position und den durchschnittlichen Einstiegspreis.
   - Es werden separate Exit-Stop- und Take-Profit-Orders erstellt (`SellStop`/`BuyStop` und `SellLimit`/`BuyLimit`), sodass die Schutzniveaus an der Börse auch dann bestehen bleiben, wenn die Entry-Orders storniert werden oder ablaufen.
   - Die Ausstiegsaufträge werden fortlaufend mit der aktuellen Positionsgröße und dem aktuellsten durchschnittlichen Einstiegspreis synchronisiert.
4. **Nachgestellte Logik**.
   - Wenn der variable Gewinn den konfigurierten Trailing-Abstand erreicht, wird der Schutzstopp verschärft, um diesen Abstand vom aktuellen Geld- (für Long-Positionen) bzw. Brief-Kurs (für Short-Positionen) beizubehalten.
   - Der Trailing Stop überschreitet niemals den Einstiegspreis und respektiert einen minimalen Aktualisierungsschritt, der einem Preisschritt entspricht.
5. **Positionsverfolgung**. Jeder eigene Trade aktualisiert den kumulierten Positionswert und berechnet den gewichteten durchschnittlichen Einstiegspreis neu, sodass Teilfüllungen und Umkehrungen korrekt verarbeitet werden.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Festes Ordervolumen (Lots oder Kontrakte), das für beide Breakout-Stop-Orders verwendet wird. |
| `TakeProfitPoints` | Abstand in Instrumentenpunkten zum Gewinnziel. Auf Null setzen, um den Take-Profit zu deaktivieren. |
| `StopLossPoints` | Abstand in Punkten für den schützenden Stop-Loss. |
| `TrailingStopPoints` | Nachlaufdistanz in Punkten, die angewendet wird, sobald die Position im Gewinn ist. Null deaktiviert das Nachziehen. |

## Verhaltensnotizen
- Eintrittsaufträge werden erst erneut eingereicht, nachdem die vorherige Instanz abgeschlossen (erfüllt, storniert oder abgelaufen) ist. Dies spiegelt die `CheckPendingOrder`-Logik des ursprünglichen Experten wider.
- Die Stop-Loss- und Take-Profit-Abstände werden immer mit `Security.PriceStep` in Preiswerte umgewandelt, um ein konsistentes Verhalten über verschiedene Instrumente hinweg sicherzustellen.
- Wenn die Position vollständig geschlossen ist, storniert die Strategie automatisch alle verbleibenden Ausstiegsaufträge und setzt die internen Durchschnittswerte zurück.
- Die Strategie basiert ausschließlich auf Daten der Ebene 1 und erfordert keine Kerzen oder Indikatoren, sodass die Konvertierung nahe an der MQL-Vorlage bleibt.

## Nutzung
1. Weisen Sie vor Beginn der Strategie das gewünschte Wertpapier und Portfolio zu.
2. Passen Sie die vier öffentlichen Parameter an, um sie an das gehandelte Instrument anzupassen (Volumen, Stop-Loss, Take-Profit und Trailing-Distanz).
3. Starten Sie die Strategie. Es wird Breakout-Aufträge und Positionsschutz autonom in Echtzeit verwalten.
