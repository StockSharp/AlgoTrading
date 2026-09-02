# Diagramm der Ichimoku-Strategie mit Tenkan/Kijun-Kreuzung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Ichimoku-System wird hier vollständig genutzt: Das schnelle Linienpaar liefert das Signal, und die Wolke entscheidet, ob dieses Signal zulässig ist. Auslöser ist die Kreuzung von Tenkan-sen und Kijun-sen, und eine Position wird nur eröffnet, wenn der Schlusskurs auf derselben Seite der Kumo-Wolke liegt, in die die Kreuzung zeigt.

![schema](schema.svg)

## Strategieübersicht

- Ein einziger Ichimoku-Baustein erzeugt alle Linien, und vier Konverter lesen Tenkan-sen, Kijun-sen, Senkou Span A und Senkou Span B aus seinem zusammengesetzten Wert.
- Zwei Formelbausteine falten die beiden Senkou-Linien zu Ober- und Unterkante der Wolke, sodass je Seite ein Vergleich genügt, um den Schlusskurs zur Wolke ins Verhältnis zu setzen.
- Eingestiegen wird nur aus der Neutralstellung, und das wird doppelt geprüft: durch den Vergleich der Position mit null und durch die Eröffnungsbedingung des Orderbausteins selbst.
- Die Ausstiege sind eigene Bausteine: Die Gegenkreuzung oder ein Schlusskurs, der wieder in die Wolke zurückfällt, holt die Position nach Hause, und die Schließen-Bausteine beziehen ihre Größe aus der offenen Position.
- Das Original ignoriert nach einer Ausführung 500 Kerzen lang jedes Signal und verzögert damit auch seine Ausstiege; ein Balkenzähler lässt sich aus diesen Bausteinen nicht bauen, also entfällt die Pause und das Diagramm handelt häufiger als das Original.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Tenkan-sen kreuzt Kijun-sen von unten nach oben, der Schlusskurs liegt über der Oberkante der Wolke und die Position ist neutral. Die Order kauft das feste Volumen und eröffnet den Long.
- **Short-Einstieg**: Tenkan-sen kreuzt Kijun-sen von oben nach unten, der Schlusskurs liegt unter der Unterkante der Wolke und die Position ist neutral. Die Order verkauft das feste Volumen und eröffnet den Short.
- **Ausstieg**: Ein Long wird geschlossen, wenn Tenkan-sen wieder unter Kijun-sen kreuzt oder der Schlusskurs unter die Unterkante der Wolke fällt; beim Short gilt das Spiegelbild. Die Schließen-Order bemisst sich an der Position, sodass das Diagramm in die Neutralstellung zurückkehrt statt zu drehen, und es gibt weder Stop-Loss noch Take-Profit - genau wie im Original.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Tenkan period | 9 | Periode von Tenkan-sen, dem Mittelwert aus höchstem Hoch und tiefstem Tief über so viele Kerzen. |
| Kijun period | 26 | Periode von Kijun-sen, gleich gebildet, aber über ein längeres Fenster. |
| Senkou Span B period | 52 | Periode von Senkou Span B, der langsameren der beiden Wolkengrenzen. |
| Volume | 1 | Ordervolumen in Lots für die Eröffnung; die Ausstiege schließen die tatsächlich offene Größe. |
| Candles | 00:01:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Ichimoku-Indikator und einen Konverter für den Schlusskurs.
- Tenkan-sen und Kijun-sen treffen sich in einem Kreuzungsbaustein, dessen Ausgang die bullische Kreuzung ist; ein logisches NICHT davon ergibt die bärische.
- Die beiden Wolkenvergleiche werden von Ein- und Ausstiegen gemeinsam genutzt: oberhalb der Wolke wird ein Long eröffnet und ein Short geschlossen, unterhalb umgekehrt.
- Jeder Einstieg läuft über ein logisches UND mit der Neutralprüfung, jeder Ausstieg über ein logisches ODER, sodass entweder die Kreuzung oder der Wolkenbruch genügt, um einen Schließen-Baustein auszulösen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
