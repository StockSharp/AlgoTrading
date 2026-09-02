# Diagramm der Strategie MACD + Stochastic mit Kreuzung auf der eigenen Nullseite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine MACD-Kreuzung bedeutet je nach Ort etwas anderes. Dieses Diagramm akzeptiert die bullische Kreuzung nur, solange die MACD-Linie noch unter null liegt - dort beginnt ein neuer Aufschwung -, und die bärische nur, solange sie noch darüber liegt. Die Stochastic-Linien bestätigen die Richtung, vor einem Trade muss die Position neutral sein, und ein prozentualer Stop samt Ziel führt wieder heraus.

![schema](schema.svg)

## Strategieübersicht

- Auslöser ist das Kreuzen der MACD-Linie mit ihrer Signallinie; der Vorzeichenfilter prüft den aktuellen und den vorherigen Wert der MACD-Linie, damit eine Kerze, die zugleich über null und über die Signallinie springt, nicht als frische Kreuzung durchgeht.
- Der Stochastic Oscillator ist die zweite Meinung: Ein Long will %K über %D, ein Short will %K darunter.
- Eingestiegen wird nur aus der Neutralstellung: Das Diagramm stockt nie auf und dreht nie auf ein Signal; Stop und Ziel sind der einzige Ausgang.
- Das Original ist die Portierung eines MetaTrader-Experten und misst Stop und Ziel in Pips, mit drei Handelssitzungen und einem mehrstufigen Trailing-Stop. Das Diagramm rechnet die Abstände in Prozent des Einstiegspreises um und lässt die Sitzungsfenster weg, weil das Standardfenster den ganzen Tag abdeckt.
- Zwei weitere Vereinfachungen: Die Stochastic-Bestätigung ist fest verdrahtet, während sie im Code ein standardmäßig ausgeschalteter Schalter ist, und sie vergleicht die beiden Linien nur im Jetzt, ohne zusätzlich ihre Lage vier Bars zuvor zu prüfen. Das Original läuft auf Vier-Stunden-Kerzen; das Diagramm ist auf Fünf-Minuten-Kerzen skaliert, passend zur mitgelieferten Beispielhistorie.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die MACD-Linie kreuzt ihre Signallinie nach oben, der aktuelle und der vorherige MACD-Wert liegen unter null, %K liegt über %D und die Position ist neutral. Die Order kauft ein Lot zum Marktpreis.
- **Short-Einstieg**: Die MACD-Linie kreuzt ihre Signallinie nach unten, der aktuelle und der vorherige MACD-Wert liegen über null, %K liegt unter %D und die Position ist neutral. Die Order verkauft ein Lot zum Marktpreis.
- **Ausstieg**: Der Baustein zur Positionsabsicherung schließt den Trade bei einem festen Prozentsatz vom Einstiegspreis, per Ziel oder per Stop. Einen Ausstieg auf die Gegenkreuzung des MACD gibt es nicht, genau wie im Original.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| MACD fast length | 12 | Periode der schnellen EMA im MACD. |
| MACD slow length | 26 | Periode der langsamen EMA im MACD. |
| MACD signal length | 9 | Periode der EMA, die den MACD zur Signallinie glättet. |
| Stochastic %K length | 5 | Berechnungsperiode der %K-Linie des Stochastic. |
| Stochastic %D length | 3 | Glättungslänge der %D-Linie, des gleitenden Durchschnitts von %K. |
| Volume | 1 | Ordervolumen in Lots. |
| Take profit, % | 1 | Abstand des Take-Profits in Prozent des Einstiegspreises; er ersetzt die 100 Pips des Originals. |
| Stop loss, % | 1 | Abstand des Stop-Loss in Prozent des Einstiegspreises; er ersetzt die 100 Pips des Originals. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist MACD und Stochastic Oscillator; vier Konverter holen die Werte Macd, Signal, %K und %D aus den beiden Indikatorwerten.
- Ein Kreuzungsbaustein macht aus dem MACD-Paar den bullischen Auslöser, ein NICHT-Baustein invertiert ihn zum bärischen, und ein Baustein für den Vorwert hält die MACD-Linie der letzten Kerze für die Vorzeichenprüfung bereit.
- Sieben Vergleichsbausteine bilden die Filter: vier für die beiden Nulltests, zwei für die Stochastic-Linien und einer für die Position gegen null.
- Jedes logische UND verbindet fünf Bedingungen und löst einen Baustein zur Positionsänderung aus, der eine Marktorder über die gemeinsame Volumenkonstante schickt; beide Orderbausteine geben ihren Abschluss an den Absicherungsbaustein weiter, der zusätzlich den Kerzenschluss als aktuellen Preis liest.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
