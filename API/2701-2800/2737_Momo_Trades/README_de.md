# Momo Trades-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertierung des originalen MetaTrader Expert Advisors "Momo_trades", der Momentum-Ausbrüche handelt, gefiltert durch einen gleitenden Durchschnitt und MACD-Struktur.

## Strategielogik
- Arbeitet mit abgeschlossenen Kerzen des konfigurierten Zeitrahmens und verarbeitet immer nur eine Netto-Position gleichzeitig.
- Verwendet einen einfachen gleitenden Durchschnitt mit einem konfigurierbaren Balken-Shift, um zu messen, wie weit der Preis vom Durchschnitt entfernt geschlossen hat. Long-Trades erfordern, dass der verschobene Schlusskurs den SMA um mehr als den Preis-Shift-Schwellenwert übersteigt; Shorts erfordern das Gegenteil.
- Wertet ein kaskadiertes MACD-Momentum-Muster aus, das die MQL-Regeln widerspiegelt: Mehrere vergangene MACD-Hauptlinien-Werte müssen für Longs durch null steigen oder für Shorts durch null fallen. Dies verhindert Trades, während das Momentum nachlässt.
- Öffnet eine Market-Order mit dem Strategie-Volumen, sobald sowohl der SMA-Distanzfilter als auch das MACD-Muster für dieselbe Richtung übereinstimmen.

## Risikomanagement
- Stop-Loss, Take-Profit, Trailing Stop, Trailing Step, Break-even und Preis-Shift-Inputs werden in Pips definiert und automatisch in Preiseinheiten über den Instrumentenschritt umgerechnet.
- Wenn Take-Profit und Trailing-Werte angegeben werden, wird der Stop erst nachgezogen, nachdem der Preis um die Trailing-Distanz plus den Trailing-Step vorgerückt ist, was das MQL-Verhalten reproduziert.
- Wenn kein Take-Profit konfiguriert ist, aber eine Break-even-Distanz gesetzt ist, wird der Stop auf den Einstiegspreis verschoben, sobald der Break-even-Trigger erreicht wird.
- Alle Stop- und Take-Level werden bei jeder abgeschlossenen Kerze neu berechnet und durch Market-Orders geschlossen, wenn sie von Kerzenextremen gekreuzt werden.

## Session-Management
- Das `CloseEndDay`-Flag entspricht dem originalen Expert Advisor und schließt jede aktive Position um 23:00 Plattformzeit (21:00 an Freitagen). Nach dem Cut-off überspringt die Strategie neue Einstiege bis zum nächsten Tag.

## Parameter
- **SMA Period / MA Bar Shift** – Länge des gleitenden Durchschnitts und der Balken-Index zum Abrufen von SMA- und Preiswerten.
- **MACD Fast / Slow / Signal / Bar Shift** – MACD-Konfiguration und der Offset, der auf gespeicherte Werte für Musterprüfungen angewendet wird.
- **Stop Loss / Take Profit / Trailing Stop / Trailing Step / Breakeven / Price Shift** – Pip-Distanzen zur Steuerung von Ausstieg, Trailing und SMA-Filtern.
- **Close End Of Day** – Schließt Positionen nach dem konfigurierten Sitzungsende.
- **Candle Type** – Zeitrahmen für Kerzen und Indikatorberechnungen.
