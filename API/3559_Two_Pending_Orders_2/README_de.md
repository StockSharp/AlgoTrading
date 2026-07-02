# Zwei ausstehende Orders 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader-Expertenberaters **"Zwei ausstehende Aufträge 2"**. Es hält zwei symmetrische ausstehende Aufträge um den Marktpreis herum und ermöglicht der zuerst ausgelösten Seite, den Handel mit konfigurierbaren Stop-Loss-, Take-Profit- und Trailing-Regeln zu verwalten. Die Konvertierung verwendet die hohe Ebene StockSharp API und behält die Kernideen des ursprünglichen Algorithmus bei, während jeder Einstellknopf durch Strategieparameter freigelegt wird.

## Handelslogik
1. Die Strategie abonniert die ausgewählte Kerzenserie (standardmäßig tägliche Kerzen). Wenn eine Kerze fertig ist, wird sie zum Entscheidungspunkt für den nächsten Handelszyklus.
2. Aktive ausstehende Orders werden storniert, sobald sie ablaufen oder bevor neue Bestellungen aufgegeben werden. Dies garantiert, dass es nur die frischesten Sorten auf dem Markt gibt.
3. Wenn der aktuelle Spread innerhalb des zulässigen Schwellenwerts liegt und die Anzahl der aktiven Positionen/Orders unter dem konfigurierten Limit liegt, platziert die Strategie zwei symmetrische ausstehende Orders:
   - **Stoppmodus** (Standardeinstellung) platziert einen Kaufstopp über dem Markt und einen Verkaufsstopp darunter.
   - Im **Limit-Modus** wird ein Kauflimit unterhalb des Marktes und ein Verkaufslimit darüber gesetzt.
   - Das Flag *Reverse Levels* tauscht die Ordertypen aus, um den ursprünglichen EA-Reverse-Wechsel zu reproduzieren.
4. Die Einstiegspreise werden durch den Parameter *Pending Indent* vom aktuellen Geld-/Briefkurs abgesetzt. Aufträge werden übersprungen, wenn sie näher als der *Min Step*-Abstand zu bestehenden Positionen liegen.
5. Ausstehende Bestellungen können nach einer bestimmten Anzahl von Minuten ablaufen. Bei Ablauf werden alle verbleibenden Bestellungen storniert.

## Positionsmanagement
- Sobald eine Order ausgeführt wurde, verfolgt die Strategie den durchschnittlichen Einstiegspreis und das durchschnittliche Volumen für die entsprechende Seite. Gegenüberliegende Füllungen verkleinern oder schließen die bestehende Position, bevor sie eine neue eröffnen.
- Die Strategie beendet Long-Positionen, wenn der Preis eine dieser Bedingungen erreicht:
  - Der Preis berührt die Stop-Loss-Distanz unterhalb des durchschnittlichen Einstiegspreises.
  - Der Preis erreicht die Take-Profit-Distanz über dem durchschnittlichen Einstiegspreis.
  - Ein Trailing-Stop wird aktiviert, nachdem der Gewinn die Aktivierungsschwelle überschreitet und der Preis anschließend auf das Trailing-Niveau zurückfällt (in Schritten).
- Short-Trades nutzen die gespiegelten Regeln mit invertierten Preisvergleichen.
- Wenn *Nur eine Position* aktiviert ist, wartet die Engine darauf, dass das aktuelle Engagement geschlossen wird, bevor neue ausstehende Aufträge aufgegeben werden.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `StopLossPoints` | Abstand zum schützenden Stop-Loss in Punkten (0 deaktiviert ihn). |
| `TakeProfitPoints` | Entfernung zum Take-Profit-Ziel in Punkten (0 deaktiviert es). |
| `MaxPositions` | Maximale Anzahl gleichzeitig aktiver Positionen und ausstehender Aufträge. |
| `MinStepPoints` | Mindestabstand zwischen dem Einstiegspreis bestehender Geschäfte und neuen ausstehenden Aufträgen. |
| `TrailingActivatePoints` | Gewinnschwelle, die den Trailing Stop aktiviert (0 deaktiviert das Trailing). |
| `TrailingStopPoints` | Abstand zwischen Marktpreis und Trailing Stop nach Aktivierung. |
| `TrailingStepPoints` | Minimale Preisverbesserung, die erforderlich ist, um den Trailing Stop erneut zu verschieben. |
| `TradeMode` | Zulässige Richtung für neue ausstehende Orders: `Buy`, `Sell` oder `BuySell`. |
| `PendingType` | Art der ausstehenden Aufträge, die aufgegeben werden sollen: `Stop` oder `Limit`. |
| `PendingExpirationMinutes` | Lebensdauer ausstehender Bestellungen in Minuten (`0` behält sie bei, bis sie manuell ausgeführt oder storniert werden). |
| `PendingIndentPoints` | Offset vom aktuellen Geld-/Briefkurs, der zur Berechnung der Preise für ausstehende Aufträge verwendet wird. |
| `PendingMaxSpreadPoints` | Maximal zulässige Spanne zwischen Bid und Ask, um ausstehende Aufträge zu erteilen (`0` deaktiviert den Filter). |
| `OnlyOnePosition` | Bei `true` wird die Eröffnung neuer Trades verhindert, bis die aktuelle Position geschlossen ist. |
| `ReverseLevels` | Vertauscht die Platzierung von Kauf- und Verkaufsaufträgen, um den ursprünglichen EA-Umkehrmodus widerzuspiegeln. |
| `CandleType` | Zeitrahmen, der zum Auslösen der Signalauswertung verwendet wird (standardmäßig täglich). |

## Notizen
- Preisabstände werden in Punkten ausgedrückt und automatisch in die Tick-Größe des Instruments umgerechnet.
- Die Strategie basiert auf StockSharp-Hilfsmethoden (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) für die Auftragsregistrierung und verwendet `CancelActiveOrders`, um das Buch jedes Mal zurückzusetzen, wenn eine neue Entscheidung getroffen wird.
- Die Trailing-Stop-Logik wird bei fertigen Kerzen ausgewertet. Für das Intrabar-Trailing-Verhalten verwenden Sie ein kürzeres `CandleType`.
