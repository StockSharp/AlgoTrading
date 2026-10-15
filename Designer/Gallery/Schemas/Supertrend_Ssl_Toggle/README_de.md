# Trend-Umschaltung mit Cooldown-Sperre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei exponentielle Durchschnitte, die sich auf einer schnellen Kerzenreihe kreuzen, erzeugen weit mehr Signale, als sich der Trend tatsächlich ändert, und eine Folge von Kreuzungen rund um ein einziges Kursniveau kann ein Konto mit Einstiegen füllen, die sich gegenseitig aufheben. Dieses Diagramm nimmt eine Kreuzung, verbraucht sie und sperrt diese Seite anschließend für eine feste Anzahl von Kerzen. Die Sperre ist ein Flag-Block, der Countdown, der sie wieder öffnet, ist ein Delay value-Block, und gestartet wird der Countdown von einer Combination, die die Ausführungen beider Richtungen zu einer einzigen Leitung mit der Bedeutung „ein Einstieg hat stattgefunden“ zusammenführt.

![schema](schema.svg)

## Strategieübersicht

- Fertige Kerzen einer Reihe speisen einen schnellen und einen langsamen exponentiellen gleitenden Durchschnitt; nichts im Diagramm reagiert auf eine Kerze, die noch entsteht.
- Zwei Crossing-Blöcke lesen dasselbe Durchschnittspaar mit vertauschten Eingängen, sodass der eine genau auf dem Balken wahr ist, auf dem der schnelle Durchschnitt den langsamen nach oben kreuzt, und der andere genau auf dem Balken, auf dem er ihn nach unten kreuzt.
- Ein Position-Block im Vergleich gegen null sagt, ob das Konto flat, long oder short ist, und genau diese Antwort macht aus einer Kreuzung ein erlaubtes Signal statt einer bloßen Beobachtung.
- Jede Richtung besitzt ein eigenes Flag. Das Einstiegssignal ist sein Auslöser, und ein Flag lässt einen Auslöser nur beim ersten Setzen durch, sodass das zweite und jedes weitere Signal dieser Seite stillschweigend verworfen wird.
- Die Einstiege sind Position modify-Blöcke, die nur aus einem flachen Konto heraus eröffnen, wodurch erlaubtes Signal und tatsächlich platzierte Order im Gleichschritt bleiben.
- Beide Einstiegsblöcke senden ihre Ausführungen in eine Combination, und diese eine Leitung schärft den Delay value-Block, übergibt die Ausführung an Position protection und zeichnet die Ausführungen im Chart ein.
- Delay value zählt fertige Kerzen nach der Ausführung und gibt einen Impuls ab, sobald der Zähler abgelaufen ist; dieser Impuls ist auf den Reset-Eingang beider Flags verdrahtet, und beide Seiten sind wieder einsatzbereit.
- Eine gegenläufige Kreuzung bei offener Position wird von einer zweiten Combination eingesammelt und schließt den Trade, während Take-Profit- und Stop-Loss-Abstände ihn früher beenden können.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der schnelle Durchschnitt kreuzt den langsamen auf einer fertigen Kerze nach oben, während das Konto flat ist. Die logische Bedingung, die diese beiden Fakten verbindet, löst das Long-Flag aus; ist das Flag noch von einem früheren Long gesperrt, stirbt das Signal dort und es wird nichts geordert. Andernfalls feuert das Flag einmal, Position modify kauft zum Marktpreis mit dem eingestellten Volumen, und das Flag bleibt gesetzt, bis der Countdown es freigibt.
- **Short-Einstieg**: Der schnelle Durchschnitt kreuzt den langsamen auf einer fertigen Kerze nach unten, während das Konto flat ist. Das Signal passiert das Short-Flag nach derselben Regel, und Position modify verkauft zum Marktpreis mit demselben Volumen. Die beiden Flags sind voneinander unabhängig, sodass eine gesperrte Long-Seite einen Short nicht verhindert.
- **Ausstieg**: Eine Abwärtskreuzung im Long und eine Aufwärtskreuzung im Short treffen sich in einer Combination, die ein auf Schließen gestelltes Position modify auslöst; das Schließvolumen ermittelt dieses selbst. Position protection läuft parallel auf der Einstiegsausführung und kann den Trade früher am Take-Profit- oder am Stop-Loss-Abstand beenden. Nichts wird in einer einzigen Order gedreht: Die Position geht zuerst auf flat zurück, und die Gegenseite wird später eröffnet, durch eine Kreuzung, die das Konto leer und diese Seite entsperrt vorfindet.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzenreihe, auf der alles im Diagramm läuft. Er legt zugleich die Einheit des Cooldowns fest, der in Kerzen dieser Reihe gezählt wird. |
| Fast EMA Length | 14 | Länge des schnellen exponentiellen gleitenden Durchschnitts. |
| Slow EMA Length | 40 | Länge des langsamen exponentiellen gleitenden Durchschnitts. Halten Sie sie deutlich über der schnellen; Durchschnitte ähnlicher Länge kreuzen sich ständig, und das Latch würde die gesamte Filterung übernehmen. |
| Cooldown Candles | 72 | Anzahl fertiger Kerzen, die eine Seite nach einem ausgeführten Einstieg gesperrt bleibt. Ein höherer Wert dünnt die Trades aus, ein niedrigerer lässt eine unruhige Phase mehrere Einstiege hintereinander erzeugen. |
| Volume | 1 | Größe eines Einstiegs, in Einheiten des Instruments. Beide Richtungen verwenden sie. |
| Take Profit, % | 1.5 | Take-Profit-Abstand, in Prozent des Einstiegskurses. |
| Stop Loss, % | 1 | Stop-Loss-Abstand, in Prozent des Einstiegskurses. |

## Diagrammdetails

- Ein Flag gibt niemals false aus. Es ist ein Latch, kein Gatter: Es meldet den Moment, in dem es gesetzt wird, und alles, was es blockiert, blockiert es durch Schweigen — deshalb öffnet der Reset-Eingang eine Seite wieder und nicht ein logisches Not.
- Delay value schärft sich nur aus dem leeren Zustand heraus. Eine Ausführung, die eintrifft, während der Countdown bereits läuft, verlängert ihn nicht; die Pause wird also ab der ersten Ausführung einer Serie gemessen und nicht ab der letzten.
- Den Countdown treibt die Kerzenreihe selbst an: Kerzen gehen in den Zähleingang und verringern ihn, die Pause ist damit in Balken ausgedrückt und folgt dem Zeitrahmen statt einer Wanduhr.
- Der Reset-Impuls öffnet beide Seiten auf einmal, gesperrt wird jede Seite jedoch für sich. Eine Richtung, die während der Pause gar nicht gehandelt hat, wird durch einen Impuls entsperrt, den die andere Richtung bezahlt hat — das ist der bewusste Unterschied zwischen einem Latch je Seite und einem einzelnen globalen Timer.
- Die Einstiegsbedingung verlangt ein flaches Konto, und der Einstiegsblock weigert sich absichtlich, aus etwas anderem heraus zu arbeiten: Ein Latch wird vom Signal verbraucht, ein Signal, das nicht ausgeführt werden könnte, würde also die Pause dieser Seite verschwenden.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
