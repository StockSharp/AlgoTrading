# Diagramm der Strategie Trailing Stop (EMA-Kreuzung)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein kurzes Trenddiagramm, dessen Reiz im Ausstieg und nicht im Einstieg liegt. Zwei exponentielle gleitende Durchschnitte wählen die Seite, doch der Signalpfad schließt nie einen Trade: die Bausteine zur Positionsänderung eröffnen ausschließlich, und ein Schutzbaustein führt den Trade zu seinem Take-Profit oder Stop-Loss. Der Trailing-Schalter dieses Bausteins bleibt aus, denn die Ursprungsstrategie deklariert einen Trailing-Abstand und nutzt ihn nie.

![schema](schema.svg)

## Strategieübersicht

- Ein schneller und ein langsamer ExponentialMovingAverage werden auf derselben Kerzenreihe berechnet.
- Eingestiegen wird nur aus der Neutralstellung, eine offene Position wird also weder gedreht noch aufgestockt.
- Beide Einstiegsbausteine leiten ihre eigenen Trades in den Schutzbaustein, der Take-Profit und Stop-Loss als Prozentsatz des Ausführungspreises setzt.
- Dieser Schutzbaustein ist der einzige Ausweg aus einem Trade; ein eigenes Ausstiegssignal hat das Diagramm nicht.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der schnelle EMA kreuzt den langsamen von unten nach oben, während die Position genau null ist. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Der schnelle EMA kreuzt den langsamen von oben nach unten, während die Position genau null ist. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Der Schutzbaustein schließt die Position bei 2% Take-Profit oder 1% Stop-Loss zum Einstiegspreis. Bis eines von beiden greift, wird die Gegenkreuzung ignoriert, da ein Einstieg eine neutrale Position verlangt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Fast EMA Length | 6 | Periode des schnellen exponentiellen gleitenden Durchschnitts. |
| Slow EMA Length | 18 | Periode des langsamen exponentiellen gleitenden Durchschnitts. |
| Take Profit, % | 2 | Abstand des Take-Profits, in Prozent des Einstiegspreises. |
| Stop Loss, % | 1 | Abstand des Stop-Loss, in Prozent des Einstiegspreises. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Indikatorbausteine und liefert zugleich den Preis, den der Schutzbaustein beobachtet.
- Der Kreuzungsbaustein gibt true aus, wenn der schnelle EMA über den langsamen steigt, und false, wenn er darunter fällt; ein logisches NICHT gewinnt daraus das Short-Signal.
- Ein einziger Vergleich gegen die Nullkonstante genügt als Positionsprüfung, und beide Bausteine zur Positionsänderung laufen zusätzlich im Nur-Eröffnen-Modus.
- Die eigenen Trades beider Einstiegsbausteine führen in den Schutzbaustein — er macht aus einer Ausführung ein Paar aus Take-Profit- und Stop-Loss-Order.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
