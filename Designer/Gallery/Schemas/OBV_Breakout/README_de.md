# Diagramm der Strategie OBV-Richtung mit Gleitender-Durchschnitt-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das On-Balance Volume addiert das Volumen jeder steigenden Kerze und zieht das jeder fallenden ab, seine Steigung zeigt also, welche Seite gerade handelt. Dieses Diagramm liest nur diese Steigung, Kerze für Kerze, und ein einfacher gleitender Durchschnitt des Kurses entscheidet, wann sie es wert ist. Trotz des Ordnernamens vergleicht das Signal das OBV nur mit seinem Wert auf der vorigen Kerze und nicht mit einer Ausbruchsmarke.

![schema](schema.svg)

## Strategieübersicht

- Das On-Balance Volume wird auf abgeschlossenen Kerzen berechnet und mit seinem Wert eine Kerze zuvor verglichen, was ein schlichtes Urteil steigend oder nicht steigend ergibt.
- Ein einfacher gleitender Durchschnitt des Schlusskurses über zwanzig Kerzen teilt den Chart in eine obere und eine untere Hälfte und legt die Richtung des Einstiegs fest.
- Eingestiegen wird nur aus der Neutralstellung, sodass sich beide Seiten innerhalb eines Trades nie in die Quere kommen.
- Für den Ausstieg wird der Durchschnitt nicht gebraucht: Die Position wird aufgegeben, sobald der Volumenstrom gegen sie dreht.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Das On-Balance Volume liegt über seinem Wert auf der vorigen Kerze, die Kerze schloss über dem gleitenden Durchschnitt und die Position ist neutral. Die Order kauft ein Lot zum Markt.
- **Short-Einstieg**: Das On-Balance Volume liegt auf oder unter seinem Vorwert, die Kerze schloss unter dem gleitenden Durchschnitt und die Position ist neutral. Die Order verkauft ein Lot zum Markt. Ein unverändertes OBV gilt hier als nicht steigend.
- **Ausstieg**: Ein Long wird auf der ersten Kerze geschlossen, auf der das OBV nicht mehr steigt, ein Short auf der ersten, auf der es wieder steigt, beides über Bausteine zur Positionsänderung im Schließmodus. Es gibt weder Stop Loss noch Take Profit.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Periode des einfachen gleitenden Durchschnitts, der die Richtung des Einstiegs bestimmt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den On-Balance-Volume-Baustein, den Durchschnittsbaustein und den Konverter für den Schlusskurs; ein Baustein für den vorigen Wert mit Versatz von einer Kerze liefert das frühere OBV, und zwei Vergleichsbausteine machen daraus je ein Flag steigend und nicht steigend.
- Jedes logische UND verbindet das OBV-Flag, die Lage des Kurses zum Durchschnitt und die Prüfung auf Neutralstellung und löst einen Baustein zur Positionsänderung im Modus "nur eröffnen" aus.
- Dieselben beiden OBV-Flags gehen direkt auf die Schließbausteine, die im Schließmodus stehen und daher untätig bleiben, solange keine Position offen ist.
- Das Diagramm läuft auf Fünf-Minuten-Kerzen der mitgelieferten Historie und handelt jedes gültige Signal.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
