# Diagramm der Strategie Hammer / Inverted Hammer mit SMA-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Hammer ist eine Kerze mit kleinem Körper, langem unterem Schatten und fast ohne oberen: Der Kurs wurde innerhalb der Kerze weit nach unten gedrückt und bis zum Schluss zurückgekauft. Der Inverted Hammer ist sein Spiegelbild. Für sich genommen treten diese Formen überall auf, deshalb entscheidet ein einfacher gleitender Durchschnitt, wo sie es wert sind: Ein Hammer wird nur unterhalb des Durchschnitts gekauft, ein Inverted Hammer nur oberhalb verkauft.

![schema](schema.svg)

## Strategieübersicht

- Zwei Kerzenmuster-Bausteine definieren einen Körper größer als null, einen Schatten länger als den doppelten Körper und den gegenüberliegenden Schatten kürzer als den halben Körper.
- Die eingebauten Muster Hammer und Inverted Hammer werden bewusst nicht verwendet, weil sie die Schatten an der Kerzenlänge statt am Körper messen.
- Der einfache gleitende Durchschnitt der Schlusskurse teilt den Chart in eine billige und eine teure Hälfte und ist zugleich Einstiegsfilter und Ausstiegslinie.
- Die Positionsprüfung sorgt dafür, dass ein Muster nur aus der Neutralstellung gehandelt wird.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Musterbaustein meldet einen Hammer, die Kerze schloss unter dem gleitenden Durchschnitt und die Position ist neutral. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Der Musterbaustein meldet einen Inverted Hammer, die Kerze schloss über dem gleitenden Durchschnitt und die Position ist neutral. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Ein Long wird geschlossen, sobald eine Kerze über dem gleitenden Durchschnitt schließt, ein Short, sobald sie darunter schließt, beides über Bausteine zur Positionsänderung im Schließmodus. Die Rückkehr zum Durchschnitt hält eine Position eine sinnvolle Strecke, bevor sie geschlossen wird.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts, der die Muster filtert und die Trades schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Musterbausteine, den gleitenden Durchschnitt und einen Konverter, der den Schlusskurs aus der Kerze holt.
- Zwei Vergleichsbausteine stellen diesen Schlusskurs dem Durchschnitt gegenüber und werden je zweimal genutzt: als Einstiegsfilter der einen und als Ausstiegsauslöser der anderen Seite.
- Der Positionsbaustein wird mit einer Nullkonstante verglichen, und jedes logische UND verbindet das Muster, die Seite des Durchschnitts und diesen Schutz.
- Beide Einstiegsbausteine senden Marktorders und beziehen ihr Volumen aus einer gemeinsamen Konstante; die beiden Ausstiegsbausteine arbeiten im Schließmodus und greifen nur, wenn es etwas zu schließen gibt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
