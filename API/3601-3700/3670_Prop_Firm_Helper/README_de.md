# Prop Firm Helper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Prop Firm Helper Strategy ist ein Donchian-Channel-Breakout-System, das vom MetaTrader-Expertenberater „Prop Firm Helper“ abgeleitet wurde. Die Strategie übermittelt Stop-Orders oberhalb der aktuellen Spanne für Long-Einstiege und unterhalb der Spanne für Short-Einstiege. Es erzwingt automatisch die Prop-Firma-Challenge-Regeln, indem es den Handel stoppt, nachdem das Zielkapital erreicht ist oder wenn das tägliche Verlustlimit überschritten wird.

## Handelslogik
- Abonnieren Sie Kerzen, die durch den Parameter `Candle Type` definiert sind.
- Berechnen Sie zwei Donchian-Kanäle:
  - `Entry Period`/`Entry Shift` zur Erkennung von Ausbrüchen.
  - `Exit Period`/`Exit Shift`, um offene Trades zu verfolgen.
- Platzieren Sie Kauf-Stopp-Orders einen Tick über dem verschobenen oberen Donchian-Hoch, wenn es flach oder kurz ist.
- Platzieren Sie Verkaufsstopp-Orders einen Tick unter dem verschobenen unteren Donchian-Tief, wenn Sie flach oder lang sind.
- Verwenden Sie die Average True Range-Glättung (`ATR Period`), um zu entscheiden, wann Stop-Orders nach vorne verschoben werden sollen.
- Schließen Sie Long-Positionen, wenn sich die Kerze unter dem nachlaufenden Donchian-Tief einpendelt. Schließen Sie Short-Positionen, wenn die Kerze über dem nachlaufenden Hoch von Donchian schließt.

## Risikomanagement
- `Risk Per Trade %` berechnet das Ordervolumen aus dem aktuellen Portfolio-Eigenkapital, der Schrittgröße des Instruments und dem Schrittpreis. Das Volumen wird auf den Börsenvolumenschritt gerundet und durch das minimale/maximale Volumen eingeschränkt.
- Schützende Stop-Orders verfolgen die Position mithilfe des Ausstiegskanals Donchian plus eines Puffers ATR, um eine übermäßige Auftragsabwanderung zu vermeiden.

## Regeln für Prop Firm Challenge
- `Use Challenge Rules` ermöglicht Challenge-Prüfungen.
- Der Handel stoppt, sobald `Pass Criteria` Eigenkapital erreicht ist. Alle Aufträge werden storniert und die Position geschlossen.
- Tägliche Drawdowns von mehr als `Daily Loss Limit` lösen eine vollständige Liquidation aus und deaktivieren neue Aufträge für den Rest der Sitzung. Das Referenzkapital wird zu Beginn jedes Handelstages zurückgesetzt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Entry Period` | Rückblick auf Breakout-Kanal Donchian. |
| `Entry Shift` | Anzahl der fertigen Kerzen, die bei Verwendung des Breakout-Kanals ignoriert werden. |
| `Exit Period` | Lookback für den letzten Kanal Donchian. |
| `Exit Shift` | Anzahl der fertigen Kerzen, die für Trailing Stops ignoriert werden. |
| `Risk Per Trade %` | Prozentsatz des Portfolio-Eigenkapitals zum Risiko bei jedem Einstieg. |
| `ATR Period` | Lookback für den ATR-Filter, der beim Verschieben von Stopps verwendet wird. |
| `Use Challenge Rules` | Ermöglicht Bedingungen für die Herausforderung von Stützenfirmen. |
| `Pass Criteria` | Eigenkapitalniveau, das den weiteren Handel stoppt. |
| `Daily Loss Limit` | Erlaubter täglicher Drawdown vor Handelsstopps. |
| `Candle Type` | Für Berechnungen verwendetes Kerzenabonnement. |

## Notizen
- Die Strategie erfordert eine Portfolioverbindung, um risikobasierte Positionsgrößen und Herausforderungskennzahlen zu berechnen.
- Aufträge werden bei jeder fertigen Kerze storniert und erneut übermittelt, um die Auslösepreise auf dem neuesten Stand von Donchian zu halten.
- Standardparameter reproduzieren das Verhalten des ursprünglichen MetaTrader-Expertenberaters.
