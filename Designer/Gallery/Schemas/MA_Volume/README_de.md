# Diagramm einer Durchschnittskreuzung mit Volumenbestätigung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Kreuzung des gleitenden Durchschnitts allein reagiert auf jedes Zucken des Kurses. Dieses Diagramm akzeptiert eine Kreuzung nur zusammen mit einem echten Aktivitätssprung: Die Kerze, die den SimpleMovingAverage kreuzt, muss um einen festgelegten Faktor mehr umsetzen als die Kerze davor. Die Gegenkreuzung gibt die Position zurück, und dort wird kein Volumen mehr verlangt.

![schema](schema.svg)

## Strategieübersicht

- Ein SimpleMovingAverage der Kerze bildet die Linie, die der Schlusskurs kreuzen muss, und ein einziger Kreuzungsbaustein macht aus den zwei Reihen ein einzelnes Aufwärts- oder Abwärtsereignis.
- Der Volumenfilter vergleicht die Kerze mit ihrem eigenen Vorgänger und nicht mit einem Durchschnitt: Ein Baustein für den vorherigen Wert hält das Volumen der Vorkerze, eine Formel multipliziert es mit dem Faktor, und ein Vergleich prüft die neue Kerze gegen das Ergebnis.
- Eingestiegen wird nur aus der Neutralstellung und nur mit Volumenbestätigung; ausgestiegen wird allein bei der Gegenkreuzung, genau wie im C#-Original.
- Das Original friert nach jeder Order 150 Bars lang den Handel ein; einen Bar-Zähler gibt es hier als Baustein nicht, daher entfällt diese Pause und das Diagramm handelt häufiger.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs kreuzt den Durchschnitt nach oben, das Volumen dieser Kerze liegt über dem Volumen der Vorkerze mal Faktor, das vorherige Volumen selbst ist größer als null und die Position ist neutral. Der Baustein kauft das gemeinsame Volumen zum Marktpreis.
- **Short-Einstieg**: Der Schlusskurs kreuzt den Durchschnitt nach unten, bei derselben Volumenbestätigung und neutraler Position. Der Baustein verkauft das gemeinsame Volumen zum Marktpreis.
- **Ausstieg**: Ein Long wird von der ersten Abwärtskreuzung geschlossen, ein Short von der ersten Aufwärtskreuzung, ohne jede Volumenbedingung; beide Schließbausteine laufen im Schließmodus und werden nur tätig, wenn es etwas zu schließen gibt. Weder die Ursprungsstrategie noch dieses Diagramm führt Stop-Loss oder Take-Profit.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts, den der Schlusskurs kreuzen muss. |
| Volume factor | 1.2 | Um welchen Faktor die aktuelle Kerze das Volumen der Vorkerze übertreffen muss, damit ein Einstieg gilt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist einen Konverter für das Gesamtvolumen, einen für den Schlusskurs und den gleitenden Durchschnitt.
- Die Volumenkette besteht aus vorherigem Wert, Formel und Vergleich; ein zweiter Vergleich gegen null verhindert, dass die allererste Kerze den Filter geschenkt bekommt.
- Ein Kreuzungsbaustein und ein logisches NICHT decken beide Richtungen ab: der eigene Ausgang ist die Aufwärtskreuzung, der negierte die Abwärtskreuzung.
- Zwei logische UND bauen die Einstiege aus Kreuzung, Volumen und neutraler Position, zwei weitere die Ausstiege aus der Gegenkreuzung und dem Vorzeichen der Position.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
