# Einfache Alligator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die einfache Alligator-Strategie recreiert den MetaTrader Expert Advisor "Alligator Simple v1.0" mithilfe der StockSharp High-Level-API. Sie liest den Bill Williams Alligator-Indikator auf abgeschlossenen Kerzen und eröffnet eine Position, wenn die Lips-, Teeth- und Jaw-Linien auf dem vorherigen abgeschlossenen Balken in dieselbe Richtung expandieren. Jeder Trade kann optional pip-basiertes Stop-Loss-, Take-Profit- und Trailing-Stop-Management umfassen, das die ursprüngliche MQL-Implementierung widerspiegelt.

## Indikatoren und Daten
- **Alligator-Linien**: drei Smoothed Moving Averages (SMMA), berechnet auf dem Kerzenmittelpunkt `(high + low) / 2` mit konfigurierbaren Längen und Vorwärtsverschiebungen für Jaw, Teeth und Lips.
- **Kerzen**: die Strategie abonniert einen einzigen konfigurierbaren `CandleType` (standardmäßig Einstunden-Kerzen) und verarbeitet nur abgeschlossene Kerzen, um Look-Ahead-Bias zu vermeiden.

## Handelslogik
1. **Signalauswertung**
   - Die verschobenen Alligator-Werte für die vorherige abgeschlossene Kerze abrufen.
   - Long-Signal: `Lips[t-1] > Teeth[t-1] > Jaw[t-1]`.
   - Short-Signal: `Lips[t-1] < Teeth[t-1] < Jaw[t-1]`.
2. **Ausführung**
   - Mit `OrderVolume` in den Markt einsteigen, wenn keine Position offen ist.
   - Es wird nur eine Position gleichzeitig gehalten; entgegengesetzte Signale werden ignoriert, bis die aktuelle Position geschlossen ist.

## Ausstieg und Risikomanagement
- **Anfänglicher Stop-Loss**: wenn `StopLossPips > 0`, versetzt die Strategie den Ausführungspreis um die pip-basierte Distanz, konvertiert mit dem Preisschritt des Instruments (einschließlich des 3/5-stelligen Pip-Multiplikators, der von MetaTrader-Symbolen verwendet wird).
- **Take-Profit**: wenn `TakeProfitPips > 0`, wird ein Gewinnziel symmetrisch um den Eintrittspreis platziert. Ein Nullwert deaktiviert das Ziel.
- **Trailing Stop**: wenn sowohl `TrailingStopPips` als auch `TrailingStepPips` positiv sind, wird der Stop zu `close − TrailingStop` (Longs) oder `close + TrailingStop` (Shorts) vorgerückt, sobald sich der Preis mindestens `TrailingStop + TrailingStep` zugunsten des Trades bewegt hat. Trailing-Updates stützen sich auf das Kerzenhoch/-tief, um Intra-Bar-Berührungen zu simulieren.
- **Ausstiegshandhabung**: Stop-Loss-, Take-Profit- und Trailing-Bedingungen geben Market-Orders aus, um die Position zu schließen und werden bei jeder abgeschlossenen Kerze ausgewertet.

## Parameter
- `OrderVolume` (Standard **1**): Handelsgröße in Lots oder Kontrakten.
- `StopLossPips` (Standard **100**): anfängliche Stop-Loss-Distanz in Pips. Auf null setzen zum Deaktivieren.
- `TakeProfitPips` (Standard **100**): Take-Profit-Distanz in Pips. Auf null setzen zum Deaktivieren.
- `TrailingStopPips` (Standard **5**): Trailing-Stop-Distanz in Pips. Null deaktiviert das Trailing.
- `TrailingStepPips` (Standard **5**): zusätzliche Pip-Distanz, die der Preis zurücklegen muss, bevor der Trailing Stop vorrückt. Muss positiv sein, wenn Trailing aktiviert ist.
- `JawPeriod`, `TeethPeriod`, `LipsPeriod`: SMMA-Längen für Jaw, Teeth und Lips (Standardwerte 13/8/5).
- `JawShift`, `TeethShift`, `LipsShift`: Vorwärtsverschiebungen beim Lesen von Alligator-Werten (Standardwerte 8/5/3).
- `CandleType`: Kerzendatentyp/Zeitrahmen für Berechnungen (Standard Einstunden-Kerzen).

## Implementierungshinweise
- Pip-Abstände passen sich automatisch an die Tick-Größe des Wertpapiers an. Instrumente mit drei oder fünf Dezimalstellen multiplizieren den Preisschritt mit zehn, um die MetaTrader-Pip-Definition zu replizieren.
- Indikator-History-Buffer speichern genügend Werte, um die konfigurierten Vorwärtsverschiebungen zu respektieren, und eliminieren die manuelle Array-Manipulation.
- Die Strategie verwendet `BuyMarket`- und `SellMarket`-Helfer zum Einreichen von Orders und hält den Code auf die Signalgenerierung und Risikohandhabung fokussiert.
