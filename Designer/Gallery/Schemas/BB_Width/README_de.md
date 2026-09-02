# Diagramm der Strategie zur Ausweitung der Bollinger-Bandbreite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Signal ist der Abstand zwischen den beiden Bollinger-Bändern, nicht die Berührung durch den Kurs. Ein Formelbaustein zieht das untere Band vom oberen ab, und das Ergebnis wird eine Kerze lang festgehalten, damit sich die beiden Messwerte vergleichen lassen. Sobald sich die Bänder öffnen, geht das Diagramm eine Position ein; die Seite entscheidet allein, ob die Kerze über oder unter dem mittleren Band geschlossen hat.

![schema](schema.svg)

## Strategieübersicht

- Die Bollinger-Bänder liefern drei Linien auf einmal; drei Konverter-Bausteine holen oberes Band, unteres Band und Mittelband aus demselben Indikatorwert.
- Die Bandbreite berechnet ein Formelbaustein, ein Vorwert-Baustein hält sie fest, sodass Ausweitung ein einfacher Vergleich zweier Zahlen wird.
- Die Richtung ist keine Ausbruchsprüfung: Jede Ausweitung eröffnet einen Trade, das Mittelband sagt nur, ob es ein Long oder ein Short wird. Genau so verzweigt die Originalstrategie.
- Sobald die Breite nicht mehr wächst, feuern beide Schließen-Bausteine und die offene Seite wird glattgestellt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Breite ist größer als auf der vorigen Kerze, die Kerze schloss über dem Mittelband und die Position ist neutral. Die Order kauft das gemeinsame Volumen zum Marktpreis.
- **Short-Einstieg**: Die Breite ist größer als auf der vorigen Kerze, die Kerze schloss auf oder unter dem Mittelband und die Position ist neutral. Die Order verkauft das gemeinsame Volumen zum Marktpreis.
- **Ausstieg**: Die Breite wächst nicht mehr, liegt also auf oder unter der Breite der vorigen Kerze. Beide Schließen-Bausteine werden ausgelöst, und derjenige, der zur offenen Seite passt, stellt sie zum Marktpreis glatt. Die Originalstrategie hat weder Stop-Loss noch Take-Profit, dieses Diagramm ebenso wenig.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Bollinger Period | 20 | Glättungsperiode der Bollinger-Bänder, die bestimmt, wie schnell die Breite reagiert. |
| Bollinger Width | 2 | Multiplikator der Standardabweichung der Bänder; ein größerer Wert vergrößert den Abstand zwischen ihnen. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein versorgt den Bollinger-Bänder-Indikator und getrennt davon einen Konverter, der den Schlusskurs liest.
- Der Formelbaustein nimmt das obere Band als a und das untere als b und gibt ihre Differenz als Bandbreite zurück.
- Die Breite läuft sowohl in den Vorwert-Baustein als auch direkt in zwei Vergleiche, sodass Ausweitung und deren Ende aus demselben Zahlenpaar gelesen werden.
- Jedes logische UND verbindet Ausweitung, Seite des Mittelbands und Prüfung auf Neutralstellung; die Ausstiegsbausteine hängen direkt am Vergleich für die Verengung.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
