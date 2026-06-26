# FxNode Sicherer-Tunnel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie ist ein StockSharp-Port des MetaTrader 4 Expert Advisors *FxNode - Safe Tunnel*. Das System verwendet einen ZigZag-basierten Trendkanal: Die jüngsten Swing-Hochs werden verbunden, um eine Widerstandslinie zu bilden, während Swing-Tiefs eine Unterstützungslinie erzeugen. Eine Position wird eröffnet, wenn der Marktpreis eine der Kanalgrenzen innerhalb einer konfigurierbaren Toleranz berührt und alle Sicherheitsprüfungen bestanden werden.

Die Konvertierung folgt dem originalen Workflow, passt ihn jedoch an die High-Level-API von StockSharp an:

- Die Kerzen-Subscription treibt die Logik an. Es werden nur vollständig geformte Kerzen verarbeitet.
- Ein `Highest`/`Lowest`-Paar emuliert den ZigZag-Detektor, der zum Zeichnen der Tunnel-Trendlinien verwendet wird.
- Ein `AverageTrueRange`-Indikator liefert den volatilitätsbasierten Stop-Anker, den die MQL-Version mit `ATRCheck() * 10` erzeugte.
- Level1-Kurse werden überwacht, damit die Strategie vor dem Zulassen neuer Trades einen maximalen Spread durchsetzen kann.

## Einstiegslogik

1. Swing-Hochs und -Tiefs mit einer konfigurierbaren ZigZag-Tiefe, Abweichung (in Pips) und Backstep erkennen. Die neuesten zwei Hochs und zwei Tiefs definieren die Trendlinien.
2. Den Preis jeder Trendlinie zum aktuellen Kerzenschlusskurs berechnen und den vertikalen Abstand zwischen dem letzten Swing-Hoch und -Tief messen.
3. Long-Setup: Der beste Ask-Preis muss oberhalb der unteren Trendlinie bleiben, aber nicht weiter als der `TouchDistanceBuyPips`-Puffer entfernt. Shorts spiegeln die Bedingung um die obere Trendlinie und den besten Bid.
4. Der optionale Sessionsfilter (Standardmäßig Mitternacht–06:00) muss den Handel erlauben. Die Strategie blockiert auch neue Aufträge am Freitag, Samstag und Sonntag und ahmt die ursprünglichen `AllowToOrder()`-Beschränkungen nach.
5. Der aktuelle Spread (Ask – Bid) darf `MaxSpreadPips` nicht überschreiten, wenn Kurse verfügbar sind.
6. `MaxOpenPositions` kontrolliert das maximale Netto-Engagement. Da StockSharp Netting verwendet, wirkt dieser Wert als Obergrenze für das Gesamtpositionsvolumen statt für separate Tickets.

## Ausstiegslogik

- Anfänglicher Stop-Loss: Der originale EA platzierte ihn bei `ATR * 10`. Der Port behält denselben Multiplikator bei und respektiert dabei die `MaxStopLossPips`-Obergrenze.
- Anfänglicher Take-Profit: Standardmäßig der Abstand zwischen dem letzten Swing-Hoch und -Tief, aber durch `TakeProfitPips` begrenzt, wenn konfiguriert.
- Festes Gewinnziel: Wenn `FixedTakeProfitPips` größer als null ist, wird die Position geschlossen, sobald der Preis mindestens so viele Pips vom Einstieg gewinnt.
- Trailing Stop: Sobald sich der Kerzenschluss um mehr als `TrailingStopPips` zugunsten des Trades bewegt, wird der Stop-Loss angepasst, um Gewinne zu sichern.
- Wochenendausstieg: Wenn `CloseBeforeWeekend` aktiviert ist, wird jede offene Position nach 23:50 am Freitag geschlossen.

Alle Ausstiege werden mit Market-Orders ausgeführt, um konsistent mit dem ursprünglichen Verhalten zu bleiben.

## Risiko und Positionsgröße

Die Losgröße wird in drei Stufen berechnet:

1. Versuchen, `RiskPercentage` des Portfoliowertes zu riskieren, vorausgesetzt, der Preisschritt des Instruments und der monetäre Schrittwert sind bekannt.
2. Wenn die risikobasierte Dimensionierung nicht berechnet werden kann, auf `StaticVolume` zurückgreifen.
3. Das endgültige Volumen zwischen `MinVolume` und `MaxVolume` begrenzen.

Da StockSharp eine einzige Nettoposition pro Instrument meldet, wird das ursprüngliche `MaxOpenPosition`-Limit als maximales Gesamtengagement und nicht als Anzahl unabhängiger Tickets interpretiert.

## Parameter

| Name | Standard | Beschreibung |
|------|----------|--------------|
| `CandleType` | 30-Minuten-Kerzen | Primärer Zeitrahmen für Analyse und Trading. |
| `TrendPreference` | Beide | Nur Long, nur Short oder symmetrisches Trading wählen. |
| `TakeProfitPips` | 800 | Maximale Take-Profit-Distanz in Pips (0 deaktiviert das Limit). |
| `MaxStopLossPips` | 200 | Maximale Stop-Loss-Distanz in Pips (0 deaktiviert das Limit). |
| `FixedTakeProfitPips` | 0 | Frühzeitige Ausstiegsdistanz in Pips. |
| `TouchDistanceBuyPips` | 20 | Long-Einstiege erfordern, dass der Ask-Preis innerhalb dieses Puffers oberhalb der unteren Trendlinie bleibt. |
| `TouchDistanceSellPips` | 20 | Short-Einstiege spiegeln die Pufferanforderung nahe der oberen Trendlinie. |
| `TrailingStopPips` | 50 | Trailing-Distanz, die angewendet wird, nachdem der Trade profitabel wird. |
| `StaticVolume` | 1 | Ausweich-Ordervolumen, wenn risikobasierte Dimensionierung nicht möglich ist. |
| `MinVolume` / `MaxVolume` | 0.02 / 10 | Grenzen für das endgültige Ordervolumen. |
| `MaxSpreadPips` | 15 | Maximal erlaubter Spread in Pips für neue Einstiege. |
| `RiskPercentage` | 30 | Portfolio-Prozentsatz, der pro Trade riskiert wird. Auf 0 setzen, um immer `StaticVolume` zu verwenden. |
| `MaxOpenPositions` | 1 | Maximales Netto-Engagement (in Vielfachen des aktuellen Ordervolumens). |
| `UseTimeFilter` | true | Aktiviert das Handelsfenster. |
| `SessionStart` / `SessionEnd` | 00:00 / 06:00 | Handelsfenster. Wenn der Start später als das Ende ist, erstreckt sich das Fenster über Mitternacht. |
| `CloseBeforeWeekend` | true | Jede Position nach 23:50 am Freitag schließen. |
| `AtrPeriod` | 14 | ATR-Lookback für die Stop-Berechnung. |
| `ZigZagDepth` | 5 | ZigZag-Lookback-Tiefe. |
| `ZigZagDeviationPips` | 3 | Mindestabstand zwischen aufeinanderfolgenden Pivots (in Pips). |
| `ZigZagBackstep` | 1 | Balken zwischen zulässigen Pivots. |
| `ZigZagHistory` | 10 | Anzahl gespeicherter Pivots für die Trendlinienprojektion. |

## Hinweise und Einschränkungen

- Die ZigZag-Rekonstruktion spiegelt das MQL-Verhalten durch die Kombination der `Highest`/`Lowest`-Indikatoren mit Abweichungs- und Backstep-Filtern. Wenn das Instrument in einer benutzerdefinierten Session handelt, sollten die Parameter angepasst werden, um sie mit dem ursprünglichen Indikator auszurichten.
- Die Spread-Filterung erfordert Live-Bid/Ask-Kurse. Wenn Kurse fehlen (z. B. beim Backtesting mit nur Kerzendaten) wird der Spread-Filter übersprungen.
- Der Port arbeitet mit Nettopositionen. Umgebungen, die unabhängiges Ticket-Management erfordern, sollten die Strategie erweitern, um jede Ausführung separat zu verfolgen.
- Zeitstrings aus der MQL-Version (z. B. `"24:00"`) werden durch `TimeSpan`-Parameter ersetzt. Um eine Nacht-Session zu reproduzieren, setzen Sie den Start später als das Ende, zum Beispiel 23:30 bis 05:30.

## Verwendung

1. Die Strategie an ein Instrument anhängen, den Kerzentyp und die Parameter konfigurieren und im Simulations- oder Live-Modus ausführen.
2. Sicherstellen, dass Markttiefe oder Level1-Subscriptions aktiviert sind, um den Spread-Filter genau durchzusetzen.
3. Die Risikokontrollen vor dem Handel mit echtem Kapital überprüfen und anpassen.
