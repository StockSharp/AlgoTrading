# Diagramm der Pivot-Point-Reversal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der klassische Pivot der Parketthändler wird bei jeder Kerze neu aus einem gleitenden Fenster berechnet: das höchste Hoch und das tiefste Tief der letzten sechzig Kerzen ergeben zusammen mit dem aktuellen Schlusskurs den Pivot P, die Unterstützung S1 und den Widerstand R1. Das Diagramm handelt gegen die Bewegung an den Rändern dieses Bandes und nimmt am Pivot wieder Gewinn mit.

![schema](schema.svg)

## Strategieübersicht

- Highest und Lowest über dasselbe Fenster ersetzen die Spanne der Vorsitzung, sodass die Marken mit dem Markt wandern statt einmal täglich festzustehen.
- P = (High + Low + Close) / 3, S1 = 2P - High, R1 = 2P - Low; ein Puffer von zwei Prozent der Fensterspanne verbreitert beide Zonen.
- Für einen Einstieg muss zusätzlich die Kerze passen: bullisch an der Unterstützung, bärisch am Widerstand.
- Ziel ist der Pivot selbst: Die Position wird geschlossen, sobald der Schlusskurs auf die andere Seite von P wechselt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Das Tief der Kerze reicht in die S1-Zone (Tief <= S1 + Puffer), die Kerze schließt über ihrer Eröffnung und die Position ist neutral. Die Kauforder eröffnet einen Long über ein Lot.
- **Short-Einstieg**: Das Hoch der Kerze reicht in die R1-Zone (Hoch >= R1 - Puffer), die Kerze schließt unter ihrer Eröffnung und die Position ist neutral. Die Verkaufsorder eröffnet einen Short über ein Lot.
- **Ausstieg**: Ein Long wird geschlossen, wenn der Schlusskurs über dem Pivot liegt, ein Short, wenn er darunter liegt. Beide Ausstiegsbausteine arbeiten im Schließmodus und bleiben untätig, wenn es nichts zu schließen gibt. Der Originalcode kennt weder Stop-Loss noch Take-Profit, und das Diagramm behält das bei.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Highest Length | 60 | Fensterlänge des Highest-Indikators, also die Anzahl Kerzen für das Fensterhoch. |
| Lowest Length | 60 | Fensterlänge des Lowest-Indikators; sie sollte der von Highest entsprechen. |
| Zone Buffer | 0.02 | Breite der Einstiegszonen als Anteil der Fensterspanne: 0,02 sind zwei Prozent. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist die Indikatoren Highest und Lowest sowie vier Konverter für Eröffnung, Hoch, Tief und Schluss.
- Drei Formelbausteine machen aus diesen fünf Zahlen den Pivot sowie Unterstützung und Widerstand samt Puffer; der Puffer steht als eigene Konstante und lässt sich daher optimieren.
- Jeder Einstieg ist ein logisches UND aus drei Vergleichen: Berührung der Marke, Richtung der Kerze und neutrale Position.
- Die beiden Ausstiegsbausteine werden von einem einfachen Vergleich des Schlusskurses mit dem Pivot ausgelöst und nutzen den Schließmodus statt eines festen Volumens.
- Die Originalstrategie rechnet mit Minutenkerzen und pausiert nach jedem Trade fünfhundert Bars; das Diagramm nutzt Fünf-Minuten-Kerzen, die die mitgelieferte Historie hergibt, und kennt diese Pause nicht.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
