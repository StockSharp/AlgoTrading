# Einfache Pivot-Flip-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine High-Level-C#-Portierung des MetaTrader 4 Expert Advisors, gespeichert in `MQL/7610/Simplepivot_www_forex-instruments_info.mq4`. Das ursprüngliche Programm vergleicht den Eröffnungspreis jeder neuen Kerze mit dem vorherigen Kerzenbereich und wechselt zwischen Long- und Short-Marktpositionen. Die StockSharp-Version behält das gleiche Verhalten bei, indem sie sich ausschließlich auf High-Level-Helfer wie `SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket` und `ClosePosition` verlässt.

Die umgewandelte Logik:

1. Wartet darauf, dass eine fertige Kerze die offenen, hohen und niedrigen Werte erhält.
2. Verwendet den vorherigen Kerzenbereich, um einen einfachen Pivot in der Mitte zu erstellen.
3. Eröffnet eine neue Long-Position, wenn die aktuelle Kerze in der unteren Hälfte der Spanne öffnet oder Lücken über dem vorherigen Hoch aufweist.
4. Eröffnet eine neue Short-Position, wenn die aktuelle Kerze in der oberen Hälfte der Spanne öffnet.
5. Schließt immer die bestehende Position, bevor sie in die entgegengesetzte Richtung einsteigt, und reproduziert so das Single-Ticket-Verhalten der MQL-Version.

Im ursprünglichen Expert Advisor sind keine Stop-Loss- oder Take-Profit-Levels implementiert, daher wird die Position nur umgekehrt, wenn eine neue Kerze eine andere Richtung vorgibt.

## Parameter
| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `OrderVolume` | 1 | Market-Order-Volumen, das bei der Eingabe einer Position verwendet wird. |
| `CandleType` | Zeitrahmen von 1 Minute | Vom Datenfeed angeforderter Kerzentyp. |

## Details zur Handelslogik
1. Die allererste fertige Kerze wird gespeichert und dient als Referenz für die nächste Entscheidung. Es wird kein Auftrag gesendet, bis eine vollständige Kerze zur Analyse vorliegt.
2. Für jede weitere fertige Kerze:
   - Berechnen Sie `pivot = (previousHigh + previousLow) / 2`.
   - Wenn `Open < previousHigh` **und** `Open > pivot`, bereitet die Strategie einen kurzen Eintrag vor.
   - Andernfalls bereitet es einen Long-Einstieg vor (dies umfasst Eröffnungen in der unteren Hälfte, Öffnungen in Höhe des Pivots und alle Lücken über dem vorherigen Hoch oder unter dem vorherigen Tief).
3. Wenn die Strategie bereits eine Position in der gewählten Richtung hält, wird das Signal ignoriert, um eine doppelte Auszahlung des Spreads zu vermeiden – was die frühe Rendite widerspiegelt, die im MQL-Code gefunden wird.
4. Ändert sich die Richtung, wird die aktuelle Position über `ClosePosition()` geschlossen und eine neue Market-Order über `OrderVolume` gesendet.
5. Der vorherige Hoch-/Tiefpuffer wird mit der zuletzt abgeschlossenen Kerze aktualisiert, um die nächste Entscheidung voranzutreiben.

## Risikomanagement
Der umgewandelte Algorithmus berücksichtigt keine Stopps oder Gewinnziele. Die Positionsgröße wird nur durch den Parameter `OrderVolume` gesteuert, daher sollte das Risiko extern verwaltet werden (z. B. durch Anpassung des Volumens oder durch Kombination der Strategie mit Schutzmaßnahmen auf Kontoebene).

## Visualisierung
Wenn ein Diagrammbereich verfügbar ist, zeichnet die Strategie die angeforderten Kerzen und die ausgeführten Trades auf, was dabei hilft, die Pivot-Flips während Backtests zu validieren.
