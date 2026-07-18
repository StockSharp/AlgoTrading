# AG Dual MACD Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Experten **AG.mq4**. Der Roboter arbeitet mit zwei Berechnungen der Moving Average Convergence Divergence (MACD), die unterschiedliche Parametersätze verwenden. Der primäre MACD erzeugt Einstiegsauslöser, während der sekundäre (skalierte) MACD als Richtungsfilter fungiert, um Gegentrend-Trades zu vermeiden und Ausstiege zu kontrollieren. Die Logik spiegelt den ursprünglichen MQL4-Experten wider, indem sie nur geschlossene Kerzen auswertet und die Signalleitungsvorzeichenprüfungen wiederverwendet, die die ursprünglichen Aufträge gesperrt haben.

## Handelslogik
- **Indikatoren**
  - Primär MACD: schneller EMA = `FastEmaLength`, langsamer EMA = `SlowEmaLength`, Signal SMA = `SignalSmaLength`.
  - Sekundär MACD: schneller EMA = `SlowEmaLength * 2`, langsamer EMA = `FastEmaLength * 2`, Signal SMA = `SignalSmaLength * 2`.
- **Langer Eintrag**
  - Die primäre MACD-Hauptleitung liegt über ihrer Signalleitung.
  - Die primäre MACD-Signalleitung ist negativ (unterhalb der Wasserlinie).
  - Die sekundäre MACD-Hauptleitung liegt über ihrer Signalleitung.
  - Die sekundäre MACD-Signalleitung ist negativ.
- **Kurzer Eintrag**
  - Die primäre MACD-Hauptleitung liegt unterhalb ihrer Signalleitung.
  - Die primäre Signalleitung MACD ist positiv.
  - Die sekundäre MACD-Hauptleitung liegt unterhalb ihrer Signalleitung.
  - Die sekundäre Signalleitung MACD ist positiv.
- **Ausgangsregeln**
  - Schließen Sie Long-Positionen, wenn der sekundäre MACD rückläufig wird, während die primäre Signallinie über Null bleibt.
  - Schließen Sie Short-Positionen, wenn der sekundäre MACD bullisch wird, während die primäre Signallinie unter Null bleibt.
- Die Strategie reagiert nur auf fertige Kerzen und ignoriert unfertige Balken, um ein Neulackieren zu vermeiden.

## Positionsmanagement
- Alle Aufträge sind Marktaufträge mit dem durch `OrderVolume` definierten festen Volumen.
- `MaxOpenOrders` spiegelt die ursprüngliche `ORDER`-Eingabe wider und begrenzt die Gesamtzahl der aktiven Orders plus offene Positionen. Stellen Sie es auf `0`, um die Kappe zu entfernen.
- `StartProtection()` wird aktiviert, sobald die Strategie startet, sodass der Risikomanager StockSharp das offene Risiko überwachen kann.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `OrderVolume` | Basislosgröße für neue Trades. |
| `FastEmaLength` | Schneller EMA Zeitraum des primären MACD. |
| `SlowEmaLength` | Langsamer EMA Zeitraum des primären MACD. |
| `SignalSmaLength` | Signalglättungszeitraum für beide MACDs. |
| `MaxOpenOrders` | Maximale Anzahl kombinierter aktiver Orders und offener Positionen. Stellen Sie `0` auf unbegrenzt ein. |
| `CandleType` | Zeitrahmen, der zum Aufbau von Kerzen für beide Indikatoren verwendet wird. |

## Notizen
- Das sekundäre MACD behält die gleiche Schnell-/Langsam-Reihenfolge wie im ursprünglichen EA bei, auch wenn die schnelle Periode größer als die langsame wird, um die Berechnungen des Autors beizubehalten.
- Die Strategie platziert keine ausstehenden Aufträge; Es öffnet oder schließt zum Marktpreis, sobald die Bedingungen vorliegen.
- Es werden keine zusätzlichen Stop-Loss- oder Take-Profit-Level hinzugefügt, da sich der ursprüngliche Experte ausschließlich auf Signalumkehrungen verlassen hat.
