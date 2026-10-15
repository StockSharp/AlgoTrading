# Three Typical Candles-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Three Typical Candles-Strategie** recreiert den MetaTrader Expert Advisor „Three Typical Candles" innerhalb der StockSharp High-Level-API. Das System beobachtet den typischen Preis der letzten drei abgeschlossenen Kerzen und handelt, wenn es eine streng monotone Sequenz erkennt. Der typische Preis ist definiert als das arithmetische Mittel aus Hoch, Tief und Schlusskurs einer Kerze. Wenn die drei zuletzt abgeschlossenen Kerzen eine steigende Sequenz typischer Preise bilden, geht die Strategie long. Umgekehrt löst eine fallende Sequenz einen Short-Einstieg aus.

Der Port folgt eng der ursprünglichen MQL5-Logik:
- Signale werden nur einmal pro abgeschlossener Kerze ausgewertet, um Intrabar-Rauschen zu vermeiden.
- Ein konfigurierbares Handelsfenster kann den Handel außerhalb ausgewählter Stunden deaktivieren und zwingt die Strategie bei aktivem Filter in eine flache Position.
- Gegenpositionen werden geschlossen, bevor eine neue eröffnet wird, sodass die Strategie nie beide Richtungen gleichzeitig hält.
- Das Order-Volumen spiegelt den Quell-EA wider, indem eine feste Lotgröße verwendet wird, während der Börsenvolumsschritt sowie die vom Wertpapier gemeldeten Mindest- und Höchstvolumenbeschränkungen eingehalten werden.

## Handelsregeln
1. **Signalerkennung**
   - Typischen Preis `Tp = (High + Low + Close) / 3` für jede abgeschlossene Kerze berechnen.
   - Die zwei vorherigen typischen Werte verfolgen. Sobald drei Werte verfügbar sind, auf eine streng steigende oder streng fallende Sequenz prüfen.
2. **Long-Einstieg**
   - Wenn `Tp[-2] < Tp[-1] < Tp[0]` (drei steigende typische Preise) und die aktuelle Position nicht long ist, schließt die Strategie jedes Short-Exposure und sendet eine Market-Kauforder.
3. **Short-Einstieg**
   - Wenn `Tp[-2] > Tp[-1] > Tp[0]` (drei fallende typische Preise) und die aktuelle Position nicht short ist, schließt die Strategie jedes Long-Exposure und sendet eine Market-Verkaufsorder.
4. **Zeitkontrolle**
   - Wenn der optionale Zeitfilter aktiviert ist, wertet die Strategie das Signal nur aus, wenn die Kerzen-Eröffnungszeit innerhalb der konfigurierten Handelssitzung liegt. Außerhalb dieses Fensters wird jede offene Position sofort liquidiert und keine neuen Trades platziert.
5. **Positionsmanagement**
   - Die Strategie hat keine expliziten Stop-Loss- oder Take-Profit-Niveaus. Das Risikomanagement sollte extern gehandhabt werden (z.B. über Schutzstrategien oder manuelle Überwachung).

## Parameter
| Name | Typ | Standard | Beschreibung |
|------|-----|----------|-------------|
| `Volume` | decimal | `1` | Festes Order-Volumen (Lots oder Kontrakte). Die Strategie rundet den Wert automatisch auf den nächsten gültigen Volumensschritt und erzwingt die Mindest-/Höchstgrenzen des Instruments. |
| `UseTimeControl` | bool | `true` | Aktiviert den Intraday-Handelsfensterfilter. Wenn deaktiviert, werden Signale rund um die Uhr ausgewertet. |
| `StartHour` | int | `11` | Inklusiver Start-Stunde (0-23) des Handelsfensters, wenn `UseTimeControl` wahr ist. |
| `EndHour` | int | `17` | Exklusive End-Stunde (0-23) des Handelsfensters, wenn `UseTimeControl` wahr ist. Wenn die Endstunde kleiner als die Startstunde ist, überspannt das Fenster Mitternacht. |
| `CandleType` | `DataType` | `TimeFrame(1h)` | Kerzentyp für die Analyse. Wählen Sie einen Zeitrahmen, der mit Ihrem Datenfeed kompatibel ist. |

## Implementierungshinweise
- Die StockSharp-Basisklasse `Strategy` übernimmt Abonnements und Order-Routing. Signale werden in `ProcessCandle` ausgewertet, das abgeschlossene Kerzen über die High-Level-Binding-API empfängt.
- Market-Orders werden über `BuyMarket` und `SellMarket` ausgegeben. Bei einer Umkehrung schließt die Strategie zuerst das bestehende Exposure mit einer entgegengesetzten Market-Order, bevor der neue Einstieg gesendet wird.
- `StartProtection()` wird während der Initialisierung aufgerufen, um das optionale Anhängen von Schutzmechanismen zu ermöglichen.
- Der Helper `GetTradeVolume` repliziert MetaTraders Lot-Normalisierung, indem das konfigurierte Volumen an Börsenbeschränkungen angepasst wird (Volumensschritt, Minimum und Maximum).
- Die Strategie speichert nur zwei historische typische Preise, was ausreicht, um das Drei-Kerzen-Muster auszuwerten, ohne große Sammlungen zu pflegen.

## Nutzungshinweise
- Hängen Sie die Strategie an ein Instrument mit ausreichender Liquidität. Der ursprüngliche EA verwendete Intraday-Forex-Daten, aber jeder Markt mit OHLC-Kerzen kann genutzt werden.
- Wählen Sie einen Kerzen-Zeitrahmen, der zu Ihrem Handelshorizont passt. Die Standard-Einstunden-Kerzen replizieren das Verhalten des Quell-EA, kürzere oder längere Intervalle können durch Parameter-Optimierung erkundet werden.
- Erwägen Sie, die Strategie mit Risikokontrollen wie maximalen Drawdown-Grenzen oder portfolioweitem Stop-Loss über das StockSharp-Schutzstrategien-Framework zu kombinieren.
- Backtesten Sie über mehrere Instrumente und Handelssitzungen, um zu bestätigen, dass das streng monotone Muster unter Ihren Marktbedingungen umsetzbare Signale liefert.
