# Diagramm der Monatswechsel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm handelt einen Kalendereffekt statt eines Kursmusters: Es trägt eine Long-Position über die Grenze zwischen zwei Monaten und hält in der Monatsmitte gar nichts. Einen Indikator gibt es nicht; die einzige Eingabe ist das Datum jeder abgeschlossenen Kerze.

![schema](schema.svg)

## Strategieübersicht

- Ein Konverter liest die Tageszahl aus der Eröffnungszeit der Kerze, und eine kurze Formel macht daraus den Abstand zum nächsten Monatsrand: min(day - 1, 31 - day).
- Eine einzige Schwelle definiert das ganze Fenster: Liegt der Abstand darunter oder darauf, gilt das Datum als Monatswechsel, darüber als Monatsmitte.
- Das Original zählt Handelstage und überspringt Wochenenden; ein Diagramm kennt keine Schleifen, also werden Kalendertage genommen und das Fenster liegt symmetrisch um die Monatsgrenze. In einem 31-Tage-Monat deckt es die ersten und die letzten sechs Kalendertage ab, in einem kurzen Monat ein bis zwei Tage weniger.
- Die Strategie ist reine Long-Strategie, deshalb entscheidet die Positionsprüfung zwischen Eröffnen und Schließen, und einen Short-Zweig gibt es überhaupt nicht.
- Die Pause von 10 Balken zwischen den Trades aus dem Original entfällt: Bei einem Fenster von mehreren Tagen und einem Einstieg, den die Positionsbedingung ohnehin sperrt, ändert sie nichts.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Abstand zum Monatsrand liegt auf oder unter dem Fenster und die Position ist nicht long. Die Order kauft das feste Volumen und eröffnet den Long, der über die Monatsgrenze getragen werden soll.
- **Short-Einstieg**: Einen Short-Einstieg gibt es nicht. Die Strategie hält entweder einen Long oder gar nichts - genau wie das Original.
- **Ausstieg**: Der Abstand zum Monatsrand ist größer als das Fenster und die Position ist long. Der Schließen-Baustein sendet eine Marktorder über die Größe der offenen Position, sodass das Diagramm die Monatsmitte neutral verbringt. Weder Stop-Loss noch Take-Profit sind vorhanden.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Window, days | 5 | Halbe Breite des Kalenderfensters in Tagen: Das Datum gilt als Monatswechsel, solange es nicht weiter als dieser Wert vom ersten oder letzten Tag entfernt ist. |
| Volume | 1 | Ordervolumen in Lots für die Eröffnung des Longs; der Ausstieg schließt die tatsächlich offene Größe. |
| Candles | 00:30:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist einen als Kerze typisierten Konverter mit dem Eigenschaftspfad OpenTime.Day, der den Kalendertag als schlichte Zahl liefert.
- Der Formelbaustein faltet diese Zahl zum Abstand zum nächsten Monatsrand, sodass eine Schwelle zugleich das Monatsende und den Monatsanfang abdeckt.
- Zwei Vergleichsbausteine teilen den Kalender in das Fenster und den Rest; zwei weitere vergleichen die Position mit einer Nullkonstante.
- Jedes logische UND verbindet eine Kalenderbedingung mit einer Positionsbedingung: Die erste löst einen Eröffnungsbaustein aus, die zweite einen Schließen-Baustein, der seine Größe aus der Position selbst nimmt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
