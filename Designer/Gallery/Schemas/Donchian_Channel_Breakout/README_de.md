# Diagramm der Donchian-Kanal-Ausbruchstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die älteste Trendfolgeidee überhaupt: Der Indikator Donchian Channels zeichnet das höchste Hoch und das tiefste Tief der letzten N Kerzen, und die Strategie steigt ein, sobald eine Kerze außerhalb dieses Kanals schließt. Sie ist stets im Markt und dreht beim Gegenausbruch von Long auf Short und zurück.

![schema](schema.svg)

## Strategieübersicht

- Die Donchian Channels werden auf abgeschlossenen Kerzen berechnet: das obere Band ist das höchste Hoch der Periode, das untere das tiefste Tief.
- Beide Bänder werden um eine Kerze verzögert, damit der aktuelle Schlusskurs mit einem bereits abgeschlossenen Kanal verglichen wird — sonst würde die Kerze das Band selbst anheben, das sie durchbrechen soll.
- Die aktuelle Position geht in jede Entscheidung ein, und zum Ordervolumen wird der Betrag der Position addiert, sodass eine Marktorder die alte Seite schließt und die neue eröffnet.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Kerze schließt über dem oberen Band der Vorkerze und die Position ist nicht long. Die Order kauft das Basisvolumen zuzüglich des Positionsbetrags: Ein Short dreht auf Long, aus der Neutralstellung entsteht ein Long.
- **Short-Einstieg**: Die Kerze schließt unter dem unteren Band der Vorkerze und die Position ist nicht short. Die Order verkauft das Basisvolumen zuzüglich des Positionsbetrags: Ein Long dreht auf Short, aus der Neutralstellung entsteht ein Short.
- **Ausstieg**: Es gibt weder Stop noch Ziel noch einen eigenen Ausstiegsbaustein: Die Position wird gehalten, bis der Gegenausbruch sie dreht — genau wie in der ursprünglichen Strategie.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Channel period | 20 | Anzahl der Kerzen, über die höchstes Hoch und tiefstes Tief gebildet werden. |
| Volume | 1 | Basisordervolumen in Lots; beim Drehen kommt der Betrag der Position hinzu. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikator Donchian Channels und über einen Konverter den Schlusskurs.
- Zwei Konverter lesen die Werte UpperBand und LowerBand aus dem Indikator, zwei Bausteine für den Vorwert verschieben sie um eine Kerze.
- Zwei Vergleichsbausteine prüfen den Schlusskurs gegen die verschobenen Bänder, zwei weitere vergleichen die Position mit null, und ein logisches UND fügt je eine Bedingung zum Einstiegssignal zusammen.
- Ein Formelbaustein berechnet das Drehvolumen als Basisvolumen plus Positionsbetrag und speist beide Bausteine zur Positionsänderung.
- Der Originalcode verwendet standardmäßig einen Kanal über 1000 Minutenkerzen; das Diagramm nutzt 20 Kerzen auf dem Fünf-Minuten-Chart, den Wert aus der README der Strategie und ihrem Optimierungsbereich, damit es auf einem Monat Historie tatsächlich handelt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
