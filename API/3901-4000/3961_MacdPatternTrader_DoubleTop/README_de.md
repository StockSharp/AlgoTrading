# Macd Pattern Trader DoubleTop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Port of the MetaTrader 4 expert advisor **MacdPatternTraderv04cb**. Die Strategie durchsucht eine konfigurierbare MACD-Hauptzeile nach
bärische Double-Top- und bullische Double-Bottom-Muster. Wenn der zweite Schwung den ersten nicht überschreitet, während der MACD
Bleibt der Wert über einem positiven oder negativen Auslöseniveau, eröffnet die Strategie eine Marktposition in Richtung des
erwartete Umkehr. Schutzaufträge reproduzieren die ursprünglich festgelegte Stop-Loss-Distanz von 100 Pip und die Take-Profit-Distanz von 300 Pip.

## Handelsregeln

1. Abonnieren Sie die ausgewählte Kerzenserie (Standard: 30-Minuten-Zeitrahmen) und berechnen Sie die MACD-Hauptlinie mit dem
konfigurierte Schnell-, Langsam- und Signalperioden (Standard: 5, 13 und 1).
2. Verfolgen Sie die letzten drei abgeschlossenen MACD-Werte. Ein rückläufiges Setup wird aktiviert, sobald der MACD über dem `TriggerLevel` bleibt.
bildet ein lokales Hoch und sinkt dann. Das Setup wird validiert, wenn der nächste Höchstwert von MACD niedriger ist als der zuvor gespeicherte Wert
hoch, während MACD noch über dem Auslöser liegt. In diesem Moment wird ein Marktverkauf gesendet.
3. Spiegeln Sie die gleiche Logik unter Null. Wenn der MACD unter `-TriggerLevel` bleibt, bildet sich ein Trog und der folgende Trog
is higher than the previous one, the strategy opens a market buy.
4. Setzen Sie die gespeicherten Spitzen und Tiefststände zurück, wenn die MACD-Linie wieder innerhalb der `[-TriggerLevel, TriggerLevel]`-Linie kreuzt.
Reichweite. Dies entspricht dem ursprünglichen EA-Verhalten, das die Mustersuche abbricht, wenn der Impuls an Stärke verliert.
5. Positionsgrößen beginnen ab dem konfigurierten `TradeVolume`. Beim Richtungswechsel fügt die Strategie ausreichend Volumen hinzu
Reduzieren Sie das entgegengesetzte Risiko, bevor Sie den neuen Handel einrichten.
6. Call `StartProtection` once on start so that both the 100 pip stop-loss and the 300 pip take-profit are managed by the
Plattform auch nach Neustarts.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `FastPeriod` | Schnelle EMA-Länge, die von MACD verwendet wird. |
| `SlowPeriod` | Langsame EMA-Länge, die von MACD verwendet wird. |
| `SignalPeriod` | Signalleitungsglättungslänge für MACD. |
| `TriggerLevel` | Der absolute MACD-Pegel ist erforderlich, um die Double-Top-/Double-Bottom-Erkennung zu aktivieren. |
| `StopLossPips` | Abstand des Schutzstopps in Pips (Standard 100). |
| `TakeProfitPips` | Entfernung des Take-Profits in Pips (Standard 300). |
| `TradeVolume` | Basisauftragsvolumen für neue Positionen. |
| `CandleType` | Für Indikatorberechnungen verwendete Kerzenserien. |

## Notizen

- Stop-Loss und Take-Profit werden vor der Weitergabe von Pips in Instrumentenschritte umgewandelt
`StartProtection`, wobei das Verhalten mit dem des ursprünglichen MQL4-Experten identisch bleibt.
- All indicator and trading comments inside the C# source code are written in English, as required by the repository
Richtlinien.
