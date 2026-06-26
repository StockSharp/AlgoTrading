# CM Panel Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **CM Panel Strategie** ist ein manueller Pending-Order-Helfer, der das Verhalten des originalen MetaTrader 5-Skripts "cm panel" nachbildet. Anstatt Bildschirmsteuerungen zu zeichnen, stellt der StockSharp-Port interaktive Parameter bereit, die wie Buttons funktionieren: Das Setzen eines Flags auf `true` platziert oder storniert Pending-Stop-Orders und das Flag setzt sich sofort auf `false` zurück, was den Push-Button-Workflow des Panels imitiert. Die Strategie hält separate Konfigurationen für Kauf- und Verkaufsorders, einschließlich Abstände, Volumina und Schutzziele in Punkten.

Die Konvertierung basiert vollständig auf der High-Level-API von StockSharp. Pending-Orders werden mit den Helfern `BuyStop` und `SellStop` eingereicht, während der Post-Fill-Schutz durch die Registrierung unabhängiger Stop-Loss- und Take-Profit-Orders implementiert wird. Preis- und Volumenwerte werden automatisch an den Tick-Größe und Lot-Schritt des Wertpapiers angepasst, sodass die Strategie Börsenbeschränkungen ohne manuelle Normalisierung einhält.

## Handelslogik
1. Wenn der Benutzer `PlaceBuyStop` auf `true` setzt, liest die Strategie den besten Ask (mit Fallback auf den letzten Handelspreis, falls notwendig) und verschiebt ihn um `BuyStopOffsetPoints`, die in Preiseinheiten umgerechnet werden. Eine Buy-Stop-Order mit dem Volumen `BuyVolume` wird auf dem resultierenden Niveau eingereicht. Die gewünschten Stop-Loss- und Take-Profit-Preise werden sofort berechnet und als ausstehende Schutzziele gespeichert.
2. Wenn der Benutzer `PlaceSellStop` auf `true` setzt, wird der beste Bid (oder letzter Handel) nach unten um `SellStopOffsetPoints` verschoben. Eine Sell-Stop-Order mit dem Volumen `SellVolume` wird auf diesem Preis platziert und die entsprechenden Schutzniveaus werden aufgezeichnet.
3. Nachdem eine ausstehende Stop-Order gehandelt wird, platziert die Strategie automatisch die aufgezeichneten Schutzorders:
   - Long-Positionen erhalten einen `SellStop`-Stop-Loss unterhalb des Einstiegspreises und einen `SellLimit`-Take-Profit darüber.
   - Short-Positionen erhalten einen `BuyStop`-Stop-Loss oberhalb des Einstiegspreises und einen `BuyLimit`-Take-Profit darunter.
   Jede Schutzorder wird nur einmal eingereicht; wenn eine ausgeführt wird, wird die andere storniert, um das einzelne SL/TP-Paar von MetaTrader zu emulieren.
4. Wenn das `CancelPendingOrders`-Flag gesetzt wird, werden alle aktiven Buy- oder Sell-Stop-Orders storniert, die von der Strategie erstellt wurden. Schutzorders, die bereits offene Positionen sichern, werden absichtlich unberührt gelassen, damit laufende Trades geschützt bleiben.
5. Volumina werden an `VolumeStep`, `MinVolume` und `MaxVolume` des Wertpapiers angepasst. Wenn die resultierende Größe ungültig wird (z.B. unter dem Mindestlot), wird die Operation abgebrochen und stattdessen eine Warnung protokolliert.
6. Alle Preisabstände werden in Punkten ausgedrückt und mit dem `PriceStep` des Wertpapiers umgerechnet. Wenn der Schritt unbekannt ist, wird ein konservativer Fallback von `0.0001` angewendet, sodass das Panel auf Symbolen ohne Tick-Metadaten nutzbar bleibt.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `BuyVolume` | `decimal` | `0.10` | Volumen, das mit jeder Buy-Stop-Order gesendet wird, nach Berücksichtigung des Lot-Schritts des Instruments. |
| `SellVolume` | `decimal` | `0.10` | Volumen, das mit jeder Sell-Stop-Order gesendet wird, nach Berücksichtigung des Lot-Schritts des Instruments. |
| `BuyStopOffsetPoints` | `int` | `100` | Abstand in Punkten, der über dem aktuellen Ask hinzugefügt wird, um den ausstehenden Buy-Stop zu positionieren. |
| `SellStopOffsetPoints` | `int` | `100` | Abstand in Punkten, der vom aktuellen Bid subtrahiert wird, um den ausstehenden Sell-Stop zu positionieren. |
| `BuyStopLossPoints` | `int` | `100` | Stop-Loss-Abstand (in Punkten) für Long-Positionen, die durch den Buy-Stop ausgelöst werden. Auf null setzen, um die Schutzorder zu überspringen. |
| `SellStopLossPoints` | `int` | `100` | Stop-Loss-Abstand (in Punkten) für Short-Positionen, die durch den Sell-Stop ausgelöst werden. Auf null setzen, um die Schutzorder zu überspringen. |
| `BuyTakeProfitPoints` | `int` | `150` | Take-Profit-Abstand (in Punkten) für Long-Positionen, die durch den Buy-Stop ausgelöst werden. Auf null setzen, um die Schutzorder zu überspringen. |
| `SellTakeProfitPoints` | `int` | `150` | Take-Profit-Abstand (in Punkten) für Short-Positionen, die durch den Sell-Stop ausgelöst werden. Auf null setzen, um die Schutzorder zu überspringen. |
| `PlaceBuyStop` | `bool` | `false` | Flag, das einmalig eine Buy-Stop-Order platziert. Der Wert setzt sich nach der Verarbeitung automatisch auf `false` zurück. |
| `PlaceSellStop` | `bool` | `false` | Flag, das einmalig eine Sell-Stop-Order platziert. Der Wert setzt sich nach der Verarbeitung automatisch auf `false` zurück. |
| `CancelPendingOrders` | `bool` | `false` | Flag, das alle aktiven ausstehenden Stop-Orders storniert, die vom Panel erstellt wurden. |

## Unterschiede zur MetaTrader-Version
- MetaTrader hängt Stop-Loss- und Take-Profit-Niveaus direkt an ausstehende Orders. StockSharp behält dasselbe Verhalten bei, indem dedizierte Schutzorders unmittelbar nach einer Einstiegsfüllung generiert werden.
- Die StockSharp-Implementierung passt Volumina und Preise transparent an die Wertpapier-Metadaten an und beseitigt den Bedarf für manuelle Normalisierung mit `_Point`, `_Digits` oder Volumenrundung.
- Stop-Level-Einschränkungen des Handelsplatzes werden nicht automatisch abgefragt. Benutzer sollten Offsets konfigurieren, die den Mindestabstand des Brokers respektieren, genau wie in MetaTrader.
- Das Lösch-Toggle (`CancelPendingOrders`) storniert nur ausstehende Stops. Bestehende Schutzorders für offene Positionen bleiben aktiv, damit Live-Trades geschützt bleiben.

## Verwendungstipps
- Portfolio und Wertpapier zuweisen, bevor Action-Flags gesetzt werden; andernfalls protokolliert die Strategie eine Warnung und ignoriert die Anfrage.
- Um den ursprünglichen Panel-Workflow zu emulieren, die Strategie zur Designer- oder Runner-UI hinzufügen, die Parameter im Eigenschaften-Raster exponieren und die Booleans kippen, wenn Orders eingereicht oder storniert werden sollen.
- Da die Logik auf den besten Bid/Ask-Kursen beruht, sicherstellen, dass Level-1-Daten gestreamt werden. Wenn die besten Preise fehlen, greift der Code auf den letzten Handelspreis zurück, aber ausstehende Orders können näher am Markt landen als beabsichtigt.
- Die Punktabstände anpassen, um den Mindest-Stop-Level des Instruments zu respektieren. Der Helfer erzwingt keine broker-spezifischen Puffer automatisch.
- Schutzabstände auf null setzen, wenn nackte Stop-Orders ohne begleitende SL/TP-Niveaus platziert werden sollen.
