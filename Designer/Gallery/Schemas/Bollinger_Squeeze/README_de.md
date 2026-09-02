# Diagramm der Ausbruchsstrategie Bollinger Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Ausbruchsdiagramm auf Basis der Bollinger-Bänder: Die Bänder liegen 1,8 Standardabweichungen um einen Zwanzig-Perioden-Durchschnitt, und ein Schlusskurs außerhalb davon gilt als Beginn einer Bewegung, nicht als Übertreibung, gegen die man handelt. Das Ordervolumen nimmt die offene Position stets mit, sodass jedes Signal die Seite dreht, statt sie aufzustocken.

![schema](schema.svg)

## Strategieübersicht

- Die Bollinger-Bänder werden auf abgeschlossenen Kerzen eines einzelnen Instruments berechnet; an den Entscheidungen sind nur das obere und das untere Band beteiligt.
- Das Diagramm ist ein Ausbruch und keine Rückkehr zum Mittelwert: Es kauft Stärke über dem oberen Band und verkauft Schwäche unter dem unteren Band, also genau umgekehrt zum Beispiel Bollinger_Bands in dieser Galerie.
- Das Volumen jeder Order ist das Grundvolumen plus der Betrag der laufenden Position, sodass ein Signal gegen eine offene Position sie schließt und die Gegenseite mit einer einzigen Order eröffnet.
- Trotz des Namens gibt es keinen Squeeze-Filter: Die ursprüngliche C#-Strategie berechnet die relative Bandbreite, verwendet sie aber in keiner Bedingung, und das Diagramm bleibt bei dem, was der Code tatsächlich tut.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Kerze schließt über dem oberen Bollinger-Band und die Position ist noch nicht long. Die Order kauft das Grundvolumen plus die Größe der offenen Position: aus der Neutralstellung ein Long-Einstieg, aus einem Short dessen Drehung.
- **Short-Einstieg**: Die Kerze schließt unter dem unteren Bollinger-Band und die Position ist noch nicht short. Die Order verkauft das Grundvolumen plus die Größe der offenen Position: aus der Neutralstellung ein Short-Einstieg, aus einem Long dessen Drehung.
- **Ausstieg**: Es gibt weder einen eigenen Ausstieg noch einen Schutzbaustein: Eine Position wird nur verlassen, wenn der Kurs jenseits des gegenüberliegenden Bandes schließt und die Drehorder die Seite wechselt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Bollinger Period | 20 | Anzahl der Kerzen, über die die Bänder gemittelt werden. |
| Bollinger Width | 1.8 | Multiplikator der Standardabweichung, der den Abstand der Bänder zur Mittellinie festlegt. |
| Volume | 1 | Grundvolumen der Order in Lots; die Positionsgröße kommt obendrauf. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikatorbaustein mit den Bollinger-Bändern und einen Konverter, der den Schlusskurs derselben Kerze liest.
- Zwei als Indikatorwert typisierte Konverter holen das obere und das untere Band aus dem einzigen Ausgang des Indikators.
- Zwei Vergleichsbausteine prüfen den Schlusskurs gegen die Bänder, zwei weitere vergleichen die Position mit einer Nullkonstante, und jedes logische UND verbindet eine Bandbedingung mit einer Positionsbedingung.
- Ein Formelbaustein berechnet Grundvolumen plus Betrag der Position und speist beide Bausteine zur Positionsänderung — dadurch wird aus jedem Einstieg eine Drehung.
- Die Pause von zehn Kerzen, die der Originalcode nach jedem Einstieg einhält, ist nicht nachgebildet: Unter den verfügbaren Bausteinen gibt es keinen Kerzenzähler, daher bremsen allein die Positionsprüfungen die Handelsfrequenz.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
