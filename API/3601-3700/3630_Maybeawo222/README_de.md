# Maybeawo222-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Maybeawo222-Strategie repliziert den MetaTrader-Expertenberater „maybeawo222“ unter Verwendung des High-Level-API von StockSharp. Es handelt ein einzelnes Instrument mit einem einfachen Crossover des gleitenden Durchschnitts (SMA) der vorherigen Kerze und begrenzt die Aktivität auf ein konfigurierbares Zeitfenster. Durch die Umstellung bleibt das abgestufte Breakeven-Management erhalten, das versucht, Gewinne zu sichern, sobald der Preis um vordefinierte Distanzen steigt.

## Handelslogik
1. Die Strategie abonniert die durch `CandleType` ausgewählte Hauptkerzenserie und berechnet einen einfachen gleitenden Durchschnitt mit dem durch `MovingPeriod` angegebenen Zeitraum.
2. Am Ende jeder Kerze wird der SMA-Wert um `MovingShift` Balken verschoben, bevor er in der Entscheidung verwendet wird. Dies reproduziert den ursprünglichen `iMA`-Aufruf mit einem Verschiebungsparameter.
3. Handelssignale werden nur ausgewertet, wenn die Schlusszeit der fertigen Kerze in den Bereich `[StartHour, EndHour)` fällt. Außerhalb dieses Fensters werden keine neuen Aufträge erstellt, offene Positionen werden jedoch weiterhin verwaltet.
4. Ein **Kaufsignal** erscheint, wenn die vorherige Kerze (diejenige, die gerade geschlossen wurde) unterhalb des verschobenen SMA öffnet und darüber schließt. Ein **Verkaufssignal** erfordert den umgekehrten Crossover. Die Strategie kehrt bei Bedarf bestehende Positionen um, so dass nur eine Richtung offen bleibt.
5. Bei jeder fertigen Kerze überprüft die Engine die Höchst-/Tiefstwerte, um Stop-Loss- oder Take-Profit-Treffer zu erkennen. Immer wenn eines der Level berührt wird, wird sofort der entsprechende Marktausstieg ausgelöst.
6. Die Position aktiviert außerdem bis zu zwei abgestufte Break-Even-Anpassungen. Sobald der variable Gewinn `BreakevenPips1` überschreitet, bewegt sich der Stop gemäß `DesiredBreakevenDistancePips1` näher an den Einstieg. In einer zweiten Stufe wird der Vorgang mit `BreakevenPips2` und `DesiredBreakevenDistancePips2` wiederholt.

## Risikomanagement
- Die anfänglichen Stop-Loss- und Take-Profit-Abstände werden in Pips konfiguriert. Die Umrechnung verwendet das Instrument `PriceStep` und wendet den herkömmlichen MetaTrader-Faktor von 10 für drei- und fünfstellige Notierungen an.
- Breakeven-Level werden nur einmal pro Positionsseite angewendet. Jeder neue Eintrag setzt die Flags zurück, sodass der Stop während der Laufzeit des Handels zweimal nachlaufen kann.
- Positionsausstiege nutzen Marktaufträge, sodass die Engine Geschäfte auch dann schließen kann, wenn die Stop- oder Zielniveaus auf der Brokerseite nicht verfügbar sind.

## Parameter
| Name | Standard | Bereich / Hinweise | Beschreibung |
|------|---------|---------------|-------------|
| `MovingPeriod` | `14` | Positive ganze Zahl | SMA Länge, die für die Crossover-Prüfung verwendet wird. |
| `MovingShift` | `0` | `0` – `10` (empfohlen) | Anzahl der abgeschlossenen Kerzen, um den SMA-Wert nach hinten zu verschieben. |
| `StopLossPips` | `100` | `0` deaktiviert | Abstand vom Einstiegspreis zum schützenden Stop-Loss, gemessen in Pips. |
| `TakeProfitPips` | `800` | `0` deaktiviert | Abstand vom Einstiegspunkt zum Take-Profit-Level, gemessen in Pips. |
| `BreakevenPips1` | `180` | `0` deaktiviert | Gewinnschwelle (in Pips), die die erste Breakeven-Anpassung auslöst. |
| `DesiredBreakevenDistancePips1` | `60` | Alles, was nicht negativ ist | Neuer Stoppabstand vom Einstieg nach Breakeven-Stufe 1. |
| `BreakevenPips2` | `500` | `0` deaktiviert | Gewinnschwelle (in Pips), die die zweite Breakeven-Anpassung auslöst. |
| `DesiredBreakevenDistancePips2` | `350` | Alles, was nicht negativ ist | Neuer Stoppabstand vom Einstieg nach Break-Even-Stufe 2-Feuern. |
| `StartHour` | `3` | `0` – `23` | Inklusive Startstunde der Handelssitzung, basierend auf der Börsenzeit. |
| `EndHour` | `22` | `0` – `23` | Exklusive Endstunde der Handelssitzung. |
| `OrderVolume` | `0.5` | Größer als `0` | Mit jeder Marktorder gesendetes Volumen vor Positionsverrechnung. |
| `CandleType` | `H1` | Beliebiger Kerzendatentyp | Kerzenserie, die zum Erzeugen von Signalen und zur Berechnung des SMA verwendet wird. |

## Hinweise zur Verwendung
- Stellen Sie sicher, dass die verbundene Sicherheit ein gültiges `PriceStep` bereitstellt. andernfalls fällt die Pip-Konvertierung auf `1` zurück. Passen Sie die Pip-bezogenen Parameter entsprechend an, wenn Ihr Instrument in großen Ticks notiert.
- Die Strategie erwartet ein Einzelsymbol-Setup. Fügen Sie es einem Schema mit dem gewünschten Instrument hinzu, bevor Sie mit der Strategie beginnen.
- Erwägen Sie beim Live-Handel die Aktivierung von Slippage-Zulagen oder schützenden Stop-Orders durch Broker-spezifische Erweiterungen, wenn Marktaustritte nicht ausreichen.
