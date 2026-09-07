# Strategiediagramm mit kombiniertem MACD-Einstieg und Nachkauf
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm verbindet ein neues Kreuzen der EMA-Differenz mit einem preisabhängigen Nachkaufereignis zu einem Long-Einstiegsstrom. Jeder Kauf umfasst eine Einheit, ein Positionszyklus enthält höchstens fünf Einstiege und eine Erholung um zwei Prozent über den letzten Ausführungspreis schließt alle gezählten Einheiten.

![schema](schema.svg)

## Strategieüberblick

- Abgeschlossene Fünf-Minuten-Kerzen liefern den Schlusskurs sowie gebildete Werte von EMA(12) und EMA(26).
- Die Formel `Fast EMA - Slow EMA` erzeugt die verwendete MACD-Linie; Crossing erkennt ihr Aufwärtskreuzen der Nulllinie.
- Das Aufwärtskreuzen eröffnet den ersten Long nur, wenn Position nicht positiv und der Einstiegszähler null ist.
- Während eines Longs fügt ein Schlusskurs mindestens fünf Prozent unter der letzten Einstiegsfüllung eine Einheit hinzu, sofern weniger als fünf Einstiege gezählt sind.
- Combination führt genau diese beiden booleschen Einstiegsereignisse zum Trigger eines einzigen Market-Buy-Blocks zusammen.
- Ein Schlusskurs mindestens zwei Prozent über der letzten Einstiegsfüllung verkauft die gesamte gezählte Positionsgröße. Danach wird der Zähler für den nächsten Zyklus zurückgesetzt.

## Ein- und Ausstiegsregeln

- **Erster Long-Einstieg**: EMA(12) minus EMA(26) kreuzt die Nulllinie aufwärts, Position ist kleiner oder gleich null und Entries in Current Long ist null. Eine Einheit wird zum Markt gekauft.
- **Nachkauf**: Position ist positiv, der Zähler liegt unter Maximum Entries und die abgeschlossene Kerze schließt bei oder unter `Latest Entry Fill × (1 - Averaging Drop / 100)`. Eine weitere Einheit wird zum Markt gekauft.
- **Ausstieg**: Bei positiver Position schließt eine abgeschlossene Kerze bei oder über `Latest Entry Fill × (1 + Take Profit / 100)`. Die gesamte gezählte Menge wird zum Markt verkauft und der Zähler zurückgesetzt.
- **Geltungsbereich**: Das Diagramm handelt nur Long und verwaltet die von seinem eigenen Einstiegsstrom eröffneten Einheiten. Es gibt weder Short-Einstieg noch Stop-Loss.

## Parameter

| Parameter | Standardwert | Beschreibung |
|---|---|---|
| Kerzenserie | 00:05:00 | Abgeschlossene Fünf-Minuten-Kerzen für alle Entscheidungen. |
| Länge schnelle EMA | 12 | Länge des schnellen exponentiellen gleitenden Durchschnitts. |
| Eingabefeld schnelle EMA | Nicht gesetzt | Es ist kein alternatives Indikator-Eingabefeld gewählt. |
| Länge langsame EMA | 26 | Länge des langsamen exponentiellen gleitenden Durchschnitts. |
| Eingabefeld langsame EMA | Nicht gesetzt | Es ist kein alternatives Indikator-Eingabefeld gewählt. |
| Maximale Einstiege | 5 | Höchstzahl der Käufe zu je einer Einheit in einem Long-Zyklus. |
| Nachkaufrückgang | 5 | Prozentualer Rückgang ab der letzten Einstiegsfüllung für einen weiteren Kauf. |
| Gewinnziel | 2 | Prozentualer Anstieg ab der letzten Einstiegsfüllung für den vollständigen Ausstieg. |
| Einstiegsvolumen | 1 | Festes Market-Volumen jedes ersten Kaufs oder Nachkaufs. |

## Diagrammdetails

- Candles gibt nur abgeschlossene Werte aus und kann die Fünf-Minuten-Serie aus gespeicherten kleineren Kerzen bilden.
- Beide EMA-Blöcke geben nur gebildete Werte aus. Die erste nutzbare MACD-Differenz erscheint daher nach der Aufwärmphase der langsamen EMA.
- Die Formel `a - b` erhält schnelle und langsame EMA; Crossing vergleicht das Ergebnis mit der Null-Variable.
- Position liefert Richtungsprüfungen, während die eigene Variable Entries in Current Long die Nullanforderung und das Limit von fünf Einstiegen durchsetzt.
- Jede Buy-Ausführung übergibt ihren durchschnittlichen Orderpreis an Latest Entry Fill. Da jede Einstiegsorder einmal ausgeführt wird, ist dies der Ausführungspreis für beide Prozentniveaus.
- Ein verzögerter Zählerpfad addiert nach jedem Buy das ausgeführte Ordervolumen. Die vollständige Ausführung des Ausstiegs schreibt vor der nächsten Kerze null in denselben Zähler.
- Combination ist boolesch und besitzt zwei verbundene Eingänge: Fresh MACD Entry und Averaging Entry Below Step Five. Entry Volume wird nach beiden Zweigen ausgegeben, damit das aktuelle Ergebnis verwendet wird.
- Der Chart erhält Kerzen, beide EMA-Linien, die MACD-Linie, den letzten Einstiegspreis, Nachkaufniveau, Gewinnzielniveau und sämtliche Ausführungen.

## Verwendung

Importieren Sie `Three_Signals_Combined.json` in Designer, stellen Sie genügend Historie für die Bildung von EMA(26) bereit und testen Sie die Fünf-Minuten-Einstellung für das gewählte Instrument. Prüfen Sie vor dem Live-Handel das Risiko von fünf Einstiegen und den fehlenden Stop-Loss.
