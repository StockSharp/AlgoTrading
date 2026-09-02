# Diagramm der Hull-MA-Steigungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Hull Moving Average folgt dem Kurs mit sehr geringer Verzögerung, deshalb ist schon die Richtung seiner eigenen Steigung ein Trendsignal. Das Diagramm misst, wie weit sich der Durchschnitt seit der Vorkerze bewegt hat — als Bruchteil seines eigenen Werts — und dreht die Position auf diese Seite, sobald die Bewegung eine kleine Schwelle überschreitet. Das Original zählt 500 Minutenkerzen; hier sind es 100 Fünf-Minuten-Kerzen, derselbe Zeitraum auf der mitgelieferten Historie.

![schema](schema.svg)

## Strategieübersicht

- Gehandelt wird ausschließlich die Steigung des Hull Moving Average — der Kurs selbst wird nie mit dem Durchschnitt verglichen.
- Die Steigung ist relativ, als Bruchteil des Vorwerts ausgedrückt, sodass dieselbe Schwelle auf jedem Kursniveau passt.
- Über +0,02% will das Diagramm long sein, unter -0,02% short; innerhalb dieses Bandes passiert nichts und die offene Position bleibt bestehen.
- Nach dem ersten Signal ist die Strategie immer im Markt: kein Stop, kein Ziel und keine Neutralstellung zwischen den Trades — genau wie im Originalcode.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Hull Moving Average ist seit der Vorkerze um mehr als die Anstiegsschwelle gestiegen und die Position ist nicht long. Die Order kauft das gemeinsame Volumen plus die Größe eines offenen Shorts, sodass eine Order die Position dreht.
- **Short-Einstieg**: Der Hull Moving Average ist seit der Vorkerze um mehr als die Abfallschwelle gefallen und die Position ist nicht short. Die Order verkauft das gemeinsame Volumen plus die Größe eines offenen Longs.
- **Ausstieg**: Es gibt keinen Ausstiegsbaustein: Das entgegengesetzte Steigungssignal dreht die Position, und da das Ordervolumen den Betrag der Position bereits enthält, schließt eine einzige Marktorder die eine Seite und eröffnet die andere.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Hull MA Length | 100 | Länge des Hull Moving Average, von 500 Minutenkerzen auf 100 Fünf-Minuten-Kerzen umgerechnet. |
| Rise Threshold | 0.0002 | Relativer Anstieg des Durchschnitts je Kerze, der einen Long eröffnet; 0,0002 entspricht 0,02%. |
| Fall Threshold | -0.0002 | Relativer Rückgang des Durchschnitts je Kerze, der einen Short eröffnet; das Spiegelbild der Anstiegsschwelle. |
| Volume | 1 | Ordervolumen in Lots, bevor die offene Position addiert wird. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Ein Baustein für den Vorwert hält den Hull-Wert der vorigen Kerze und schweigt beim ersten Wert, was den übersprungenen ersten Balken des Originals nachbildet.
- Die Steigungsformel zieht den Vorwert vom aktuellen ab und teilt durch den Vorwert, wodurch die Bewegung zu einem Bruchteil wird.
- Zwei Vergleiche teilen diesen Bruchteil mit der positiven und der negativen Schwellenkonstante in drei Zustände.
- Jedes logische UND verbindet eine Steigungsbedingung mit einer Positionsprüfung, und die Volumenformel addiert den Betrag der Position zum gemeinsamen Volumen — das macht aus einem Einstieg eine Umkehr.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
