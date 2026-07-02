# Trailing-Stop-Trigger-Manager-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Trailing Stop Trigger Manager-Strategie** ist eine StockSharp-Portierung des MetaTrader-Expertenberaters `Trailing Sl.mq5`. Das Original EA
hat keine Geschäfte selbstständig eröffnet. Stattdessen überwachte es bereits offene Positionen mit einer passenden *magischen Zahl* und verschärfte diese
Stop-Loss-Level, wenn sich der Markt in die gewünschte Richtung bewegt. Diese C#-Implementierung reproduziert dieses Verhalten mithilfe von
Die High-Level-Strategie API von StockSharp bietet ein transparentes Trailing-Stop-Management, das mit jedem von unterstützten Instrument funktioniert
StockSharp.

## Nachgestellte Logik
1. Abonniert das Orderbuch, um die neuesten besten Geld- und Briefkurse zu lesen.
2. Erkennt, ob die Strategie derzeit eine Long- oder Short-Nettoposition hält.
3. Berechnet den variablen Gewinn anhand der entsprechenden Marktseite (bester Geldkurs für Long-Positionen, bester Briefkurs für Shorts-Positionen).
4. Aktiviert den Trailing-Modus, sobald der Gewinn `TriggerPoints` übersteigt (umgerechnet in Preiseinheiten bis `PriceStep`).
5. Setzt den Trailing Stop auf den konfigurierten Abstand `TrailingPoints` vom aktuellen Marktkurs.
6. Verschiebt den Trailing Stop nur in Richtung Markt, um weiterhin zusätzlichen Gewinn zu erzielen.
7. Sendet eine Marktorder, um die Position zu glätten, sobald der beste Kurs das berechnete Trailing-Stop-Level berührt.

## Auftrags- und Risikomanagement
- Die Strategie übermittelt **keine** Ersteintrittsaufträge. Es verwaltet lediglich eine bestehende Position, die ggf. manuell eröffnet wurde
oder durch eine andere Strategie.
- Marktaustritte werden mit `BuyMarket`/`SellMarket` platziert und spiegeln die `PositionModify`-Aufrufe aus dem ursprünglichen MetaTrader-Code wider.
- Der Stoppabstand skaliert automatisch mit dem `PriceStep` des Instruments, wodurch die punktbasierte Konfiguration erhalten bleibt
der EA.
- Sobald die Position geschlossen ist, wird der Trailing-Status zurückgesetzt, sodass neue Positionen von vorne beginnen.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `TrailingPoints` | `int` | `1000` | Abstand zwischen dem aktuellen Preis und dem Trailing Stop, gemessen in Preisschritten. |
| `TriggerPoints` | `int` | `1500` | Mindestgewinn in Preisschritten, der erforderlich ist, um mit dem Nachlaufen der Position zu beginnen. |

## Nutzungshinweise
- Hängen Sie die Strategie dem Wertpapier an, dessen Position Sie überwachen möchten. Es beginnt sofort mit der Verfolgung des Vorhandenen
Belichtung.
- Konfigurieren Sie den anfänglichen `Volume` der Strategie so, dass er der Größe Ihrer offenen Position entspricht. StockSharp verwendet Nettopositionen, also
Die Strategie verlässt das gesamte Lot, wenn der Trailing Stop ausgelöst wird.
- Wenn der Broker grobe Preissprünge liefert, passen Sie `TrailingPoints` und `TriggerPoints` entsprechend an, um vorzeitige Ausstiege zu vermeiden.
- Die Strategie behält ihren Status vollständig in StockSharp, sodass sie mit jedem beliebigen diskretionären oder automatisierten System kombiniert werden kann
überlässt die eigentliche Auftragsausführung StockSharp.

## Unterschiede zum ursprünglichen MetaTrader-Experten
- MetaTrader verwaltete separate Positionen pro Ticket und filterte sie nach *magischer Zahl*. StockSharp arbeitet mit einer Nettoposition pro
Sicherheit, wodurch die Notwendigkeit einer Ticketfilterung entfällt.
- Die Eingänge `Setloss`, `TakeProfit` und `Lots` wurden im ursprünglichen EA nicht verwendet. Sie werden daher im StockSharp weggelassen.
Version, um die Konfiguration auf das Nachlaufverhalten zu konzentrieren.
- Auftragsänderungen werden durch direkte Marktaustritte ersetzt, was der idiomatische Ansatz für Netting-Konten in StockSharp ist.
