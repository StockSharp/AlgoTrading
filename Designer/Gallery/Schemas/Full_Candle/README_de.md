# Diagramm der Momentum-Strategie mit voller Kerze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine volle Kerze eröffnet an einem Ende ihrer Spanne und schließt am anderen: Die Schatten zusammen nehmen höchstens einen kleinen Teil des Abstands von Tief zu Hoch ein. So eine Bar ist ein einziger ununterbrochener Schub, und das Diagramm schließt sich ihr in Richtung des Körpers an, solange ein exponentieller gleitender Durchschnitt dieser Richtung zustimmt. Der Trade bekommt ein festes Ziel von einem Bruchteil eines Prozents — und sonst nichts.

![schema](schema.svg)

## Strategieübersicht

- Konverter lesen Eröffnung, Hoch, Tief und Schluss der abgeschlossenen Kerze, zwei Formelbausteine messen, wie viel der Spanne die Schatten beanspruchen.
- Das bullische Maß ist oberer plus unterer Schatten einer steigenden Kerze, mit hundert skaliert und gegen den Schattenanteil an der vollen Spanne gestellt; das bärische Maß ist sein Spiegelbild.
- Ein exponentieller gleitender Durchschnitt der Schlusskurse ist der Trendfilter: volle bullische Kerzen werden nur darüber gekauft, volle bärische nur darunter verkauft.
- Ein Baustein zum Positionsschutz schließt jeden Trade mit einem festen Take-Profit — dem einzigen Ausstieg, den die Originalstrategie kennt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Das bullische Schattenmaß liegt unter null, die Kerze ist also gestiegen und ihre Schatten blieben im erlaubten Anteil der Spanne; der Schluss liegt über der EMA und die Position ist nicht bereits long. Die Order kauft die Volumenkonstante plus einen offenen Short und dreht damit den Short in einer einzigen Order in einen Long.
- **Short-Einstieg**: Das bärische Schattenmaß liegt unter null, der Schluss liegt unter der EMA und die Position ist nicht bereits short. Die Order verkauft die Volumenkonstante plus einen offenen Long und dreht ihn in einer Order in einen Short.
- **Ausstieg**: Der Schutzbaustein nimmt bei 0,3 Prozent vom Einstiegskurs Gewinn mit — genau die Zahl, die im Original fest im Code steht; einen Stop-Loss gibt es nicht, weil das Original keinen hat. Zwei Unterschiede sind wichtig. Der Schutzbaustein beobachtet den Kurs innerhalb der Bar, das Original prüft nur den Schluss einer abgeschlossenen Kerze, das Ziel greift hier also etwas früher. Und die Pause des Originals von fünfzehn Kerzen nach jedem Trade entfällt: Ein Balkenzähler lässt sich nur bauen, indem ein Signal ins Diagramm zurückgeführt wird, was den Graphen zu einer Schleife schließen würde. Ein Umkehrsignal wird deshalb sofort gehandelt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| EMA Length | 20 | Periode des exponentiellen gleitenden Durchschnitts, der als Trendfilter dient. |
| Shadow share, % | 10 | Größter Anteil der Kerzenspanne von Tief zu Hoch in Prozent, den beide Schatten zusammen einnehmen dürfen. |
| Take profit, % | 0.3 | Abstand des Take-Profits vom Einstiegskurs in Prozent. |
| Volume | 1 | Ordervolumen in Lots; die Umkehrorder legt die Größe der zu schließenden Position obendrauf. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. Die Originalstrategie rechnet auf Fünfzehn-Minuten-Kerzen; hier sind es fünf Minuten, damit das Muster auf der mitgelieferten Historie oft genug vorkommt. |

## Diagrammdetails

- Jede Formel zieht das erlaubte Schattenbudget von den tatsächlichen Schatten ab, ein Wert unter null bedeutet also eine Kerze mit vollem Körper; die Konstante mit dem Schattenanteil speist beide Formeln.
- Die Richtung braucht keinen eigenen Vergleich: Das bullische Maß ist für eine steigende Kerze geschrieben und auf einer fallenden — wie auch auf einer Kerze ohne Spanne — stets positiv, ein Wert unter null heißt also bereits, dass die Kerze gestiegen ist.
- Der Positionsbaustein läuft zwei Wege: in die Vergleiche gegen null, die die Einstiege absichern, und in die Volumenformel, die den Betrag der Position zur Konstante addiert, damit eine Marktorder die Gegenseite schließt und die neue Seite eröffnet.
- Beide Einstiegsbausteine geben ihre eigenen Trades an den Schutzbaustein weiter, der den Take-Profit registriert; der Schlusskurs geht als Preisreferenz in denselben Baustein.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
