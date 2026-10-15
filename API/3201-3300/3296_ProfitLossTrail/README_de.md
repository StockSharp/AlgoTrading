# ProfitLossTrailStrategy-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

ProfitLossTrailStrategy ist ein Risikomanagement-Helfer, der aus dem MetaTrader Expert Advisor **ProfitLossTrailEA v2.30** konvertiert wurde. Die Strategie erzeugt keine eigenen Einstiege. Stattdessen überwacht sie die aktuell offene Position im konfigurierten Wertpapier und wendet automatisch Schutzausstiege an:

- anfängliche Stop-Loss- und Take-Profit-Niveaus;
- Trailing-Stop-Management mit optionaler Aktivierungsdistanz und Trailing-Schrittsteuerung;
- Break-even-Schutz mit konfigurierbarem Gewinntrigger und Offset;
- Möglichkeit, bestehende Schutzlevel zu entfernen, wenn der Trader sie manuell verwalten möchte.

Das Verhalten entspricht eng dem "Basket"-Managementmodus des ursprünglichen EA: Alle Orders derselben Richtung werden als eine Position behandelt, und die Schutzlevel werden bei jeder Exposure-Änderung neu berechnet.

## Parameterreferenz

| Parameter | Beschreibung |
|-----------|--------------|
| **Manage As Basket** | Wenn aktiviert (Standard), berechnet jeder Fill in derselben Richtung den durchschnittlichen Einstiegspreis neu und aktualisiert Stop-Loss-/Take-Profit-Niveaus. Deaktivieren Sie das Flag, um die anfänglichen Niveaus nach dem ersten Fill beizubehalten. |
| **Enable Take Profit** | Schaltet automatische Take-Profit-Verarbeitung ein oder aus. |
| **Take Profit (pips)** | Distanz in Pips zwischen Einstiegspreis und Take-Profit-Ziel. |
| **Enable Stop Loss** | Schaltet automatische Stop-Loss-Verarbeitung ein oder aus. |
| **Stop Loss (pips)** | Distanz in Pips zwischen Einstiegspreis und anfänglichem Schutz-Stop. |
| **Enable Trailing Stop** | Aktiviert dynamisches Stop-Management, sobald die Position im Gewinn ist. |
| **Trailing Activation (pips)** | Mindestgewinn in Pips, bevor sich der Trailing Stop bewegen darf. `0` für sofortige Aktivierung. |
| **Trailing Stop (pips)** | Basis-Trailing-Distanz in Pips. |
| **Trailing Step (pips)** | Zusätzlicher Gewinn, der vor weiterem Nachziehen des Trailing Stops erzielt werden muss. |
| **Enable Break-Even** | Aktiviert die Break-even-Routine, die den Stop nach einer Trigger-Distanz in den Gewinn verschiebt. |
| **Break-Even Trigger (pips)** | Gewinndistanz, die die Break-even-Bewegung aktiviert. |
| **Break-Even Offset (pips)** | Zusätzlicher Offset oberhalb (Long) oder unterhalb (Short) des Einstiegspreises bei Break-even-Aktivierung. |
| **Remove Take Profit** | Wenn auf `true` gesetzt, wird jeder aktuelle Take-Profit-Wert gelöscht und keine Take-Profit-Ausstiege werden ausgegeben. |
| **Remove Stop Loss** | Wenn auf `true` gesetzt, wird jeder aktuelle Stop-Loss-Wert gelöscht und keine Stop-Loss- oder Trailing-Ausstiege werden ausgegeben. |
| **Candle Type** | Kerzenserie zur Überwachung der Preisbewegung. Trailing-, Break-even- und Ausstiegsprüfungen werden auf abgeschlossenen Kerzen bewertet. |

## Nutzungshinweise

1. Binden Sie die Strategie an ein Wertpapier und stellen Sie sicher, dass Orders extern oder durch eine andere Strategie platziert werden. ProfitLossTrailStrategy konzentriert sich nur auf die Verwaltung der offenen Exposure.
2. Konfigurieren Sie die pipbasierten Parameter passend zur Preisstellung des Instruments. Die Pip-Größe wird automatisch aus `Security.PriceStep` abgeleitet.
3. Wenn Break-even und Trailing Stop beide aktiviert sind, erfolgt die Break-even-Anpassung zuerst. Nachfolgende Trailing-Schritte ziehen den Stop nur enger, wenn das neue Niveau den aktuellen Schutzpreis mindestens um die angegebene Trailing-Schritt-Distanz verbessert.
4. **Remove Stop Loss** deaktiviert Stop-Loss-, Trailing- und Break-even-Logik gleichzeitig und spiegelt damit das Verhalten des ursprünglichen EA.
5. Die Strategie verwendet Marktorders (`BuyMarket`/`SellMarket`), um Positionen zu schließen, wenn Schutzlevel erreicht werden.

## Hinweise zur Umstellung

- Die MetaTrader-Modi "Order_By_Order" und "Same_Type_As_One" werden durch das Flag **Manage As Basket** dargestellt. Stop-Level pro Ticket werden in StockSharp nicht unterstützt, daher wird Basket-Modus standardmäßig angewendet.
- Magic-Number- und Kommentarfilter aus dem ursprünglichen EA sind nicht erforderlich; die Strategie wirkt nur auf das konfigurierte `Strategy.Security`.
- Bildschirmzeichnung, Tonalarme und timerbasierte UI-Aktualisierungen wurden weggelassen, da StockSharp Diagnosen bereits über Logs und Chart-Bindings bereitstellt.
