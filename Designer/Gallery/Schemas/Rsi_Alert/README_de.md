# Diagramm der RSI-Alert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm setzt RSI-Extremwerte in Trades und verständliche Meldungen um. Es verarbeitet abgeschlossene Fünf-Minuten-Kerzen, kauft bei 30 oder darunter und verkauft bei 70 oder darüber ausschließlich ohne offene Position und versieht jeden ausgeführten Einstieg mit einem prozentualen Schutz. Jedes angenommene Signal hält außerdem den numerischen RSI-Wert fest, formatiert ihn und schreibt eine Meldung.

![schema](schema.svg)

## Strategieübersicht

- Ein einziger Strom ausschließlich abgeschlossener Fünf-Minuten-Kerzen steuert den Indikator, die Positionsmomentaufnahme, die Einstiegsentscheidungen, die Prüfung der Schutzpreise und den Chart.
- RelativeStrengthIndex verwendet eine Periode von 14. Sein Filter für ausschließlich fertig gebildete Werte ist deaktiviert (`IsFormed = false`), sodass Werte der Aufwärmphase nicht allein deshalb unterdrückt werden, weil der Indikator noch nicht fertig gebildet ist.
- Ein Formula-Baustein mit dem Ausdruck `a` wandelt den RSI-IndicatorValue in einen numerischen Wert um, den Vergleiche und Meldungen verwenden.
- Der numerische RSI wird mit dem überverkauften und dem überkauften Niveau verglichen. Jedes Richtungssignal wird mit einer während der Auswertung der aktuellen Kerze aufgenommenen Positionsmomentaufnahme verbunden, und beide Einstiegsbausteine verwenden die Bedingung Open position.
- Ausgeführte Einstiege aktivieren den Positionsschutz mit 2% Take-Profit und 1% Stop-Loss. Angenommene Einstiegssignale leiten außerdem den festgehaltenen RSI-Wert über einen Formatierer an eine Meldung vom Typ Log weiter.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der numerische RSI liegt auf oder unter dem Oversold Level und die Positionsmomentaufnahme zeigt keine offene Position. Das Diagramm kauft das eingestellte Volumen zum Marktpreis und schreibt eine Kaufmeldung mit dem Signalwert.
- **Short-Einstieg**: Der numerische RSI liegt auf oder über dem Overbought Level und die Positionsmomentaufnahme zeigt keine offene Position. Das Diagramm verkauft das eingestellte Volumen zum Marktpreis und schreibt eine Verkaufsmeldung mit dem Signalwert.
- **Ausstieg**: Der Positionsschutz schließt den Trade, wenn der Schlusskurs einer abgeschlossenen Kerze das Take-Profit-Niveau von 2% oder das Stop-Loss-Niveau von 1% relativ zum Einstieg erreicht. Ein entgegengesetztes RSI-Signal kehrt eine offene Position nicht um, und die Ausführung einer Schutzorder kann keinen weiteren Einstieg auf derselben Kerze auslösen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| RSI Period | 14 | Anzahl der Kerzen zur Berechnung des RelativeStrengthIndex. |
| Oversold Level | 30 | RSI-Werte auf oder unter diesem Niveau erlauben einen Long-Einstieg, wenn das Diagramm keine offene Position hat. |
| Overbought Level | 70 | RSI-Werte auf oder über diesem Niveau erlauben einen Short-Einstieg, wenn das Diagramm keine offene Position hat. |
| Take Profit | 2% | Abstand des schützenden Take-Profits vom Einstiegspreis. |
| Stop Loss | 1% | Abstand des schützenden Stop-Losses vom Einstiegspreis. |
| Volume | 0.01 | Volumen der Einstiegsorder in Lots. |
| Candles | 00:05:00 | Fünf-Minuten-Zeiteinheit der Kerzen; nur abgeschlossene Kerzen werden verarbeitet. |

## Diagrammdetails

- Der Ausgang des [Candles](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Bausteins löst zuerst die Momentaufnahme der aktuellen Position aus, aktualisiert dann den [Indicator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Baustein und zuletzt einen [Converter](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) für den Schlusskurs.
- Der RSI-Ausgang gelangt in einen [Formula](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/formula.html)-Baustein mit dem Ausdruck `a`. Sein numerischer Ausgang erreicht beide Wertelatches für Meldungen, bevor einer der Schwellenwertvergleiche ausgewertet wird.
- Zwei [Comparison](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Bausteine prüfen den numerischen RSI mit `<=` und `>=` gegen die gemeinsamen Werte Oversold Level und Overbought Level.
- Der [Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html)-Wert wird für die Auswertung der aktuellen Kerze von einem [Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)-Baustein festgehalten und mit null verglichen. Zwei [Logical condition](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)-Bausteine verbinden dieses Ergebnis für die neutrale Position mit den Long- und Short-RSI-Signalen.
- Beide [Modify position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Einstiegsbausteine verwenden Marktorders mit der Bedingung Open position und erhalten `0.01` aus einem gemeinsamen Volumenwert.
- Die MyTrade-Ausgänge beider Einstiegsbausteine speisen den [Position protection](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)-Baustein. Der Schlusskurskonverter versorgt seinen Price-Eingang, und der Baustein verwendet 2% Take-Profit und 1% Stop-Loss.
- Jedes verbundene Einstiegssignal löst sein eigenes RSI-Wertelatch aus. [String format](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) erzeugt `RSI {0:0.0} <= 30 — buy` oder `RSI {0:0.0} >= 70 — sell`, und [Notification](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html)-Bausteine vom Typ Log veröffentlichen die Meldungen.
- Das Chart panel erhält abgeschlossene Kerzen, RSI-Werte, beide Einstiegstrade-Ströme und die Trades der Schutzausstiege.

## Verwendung

Importieren Sie die `.json`-Datei in Designer und führen Sie sie im Backtester mit historischen Daten aus. Beobachten Sie die formatierten RSI-Meldungen im Protokoll und prüfen Sie die Schutzausstiege anhand der Kerzenschlusskurse. Wenn Sie einen der RSI-Schwellenwerte ändern, aktualisieren Sie auch die zugehörige Formatvorlage, damit der Meldungstext korrekt bleibt.
