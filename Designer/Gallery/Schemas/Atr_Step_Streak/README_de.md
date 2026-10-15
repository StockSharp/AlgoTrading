# Diagramm der Strategie ATR Step Streak
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei gleitende Durchschnitte bestimmen die Richtung, gekauft wird aber nicht in dem Moment, in dem sie die Plätze tauschen. Das Diagramm wartet danach eine feste Anzahl abgeschlossener Kerzen ab und fragt erst dann, ob der Trade noch lohnt. Das Warten übernimmt ein N values-Block, der vom Vergleich der beiden Durchschnitte scharf geschaltet wird und sich meldet, sobald die ihm vorgegebene Anzahl Kerzen vergangen ist. Eine zweite Bedingung misst, wie viel Raum bis zum Rand der jüngsten Spanne bleibt, und zwar in Volatilität statt in Preis: Vor einem Kauf muss der Schlusskurs mindestens einen ATR-Schritt vom höchsten Hoch entfernt sein, vor einem Verkauf denselben Schritt über dem tiefsten Tief liegen.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünfzehn-Minuten-Kerzen speisen einen Konverter, der den Schlusskurs herauszieht, sowie fünf Indikatoren: einen schnellen und einen langsamen einfachen gleitenden Durchschnitt, einen ATR sowie das höchste Hoch und das tiefste Tief des jüngsten Kanals.
- Zwei Comparison-Blöcke vergleichen die Durchschnitte miteinander und melden sich auf jeder Kerze: Der eine ist wahr, solange der schnelle Durchschnitt über dem langsamen liegt, der andere, solange er darunter liegt.
- Jeder Vergleich schaltet seinen eigenen N values-Block scharf. Der Block nimmt den Kerzenstrom an seinem Eingang entgegen, zählt die auf das Scharfschalten folgenden abgeschlossenen Kerzen, gibt beim Ablauf der Zählung einen einzelnen Impuls aus und schaltet sich beim nächsten wahren Vergleich erneut scharf, sodass die Einstiegsseite des Diagramms in einem langsameren Takt läuft als die Marktdaten.
- Eine Formula multipliziert den ATR mit dem Schrittmultiplikator, und zwei weitere Formula-Blöcke machen aus diesem Schritt ein Paar Schutzniveaus: das höchste Hoch minus den Schritt und das tiefste Tief plus den Schritt.
- Zwei weitere Vergleiche fragen, ob der Schlusskurs noch unter dem oberen Schutzniveau oder noch über dem unteren liegt — also ob der Preis bereits so weit in den Kanal gelaufen ist, dass für den Trade kein Raum mehr bleibt.
- Ein Position-Block wird gegen eine Null-Variable geprüft, damit die Einstiegsseite weiß, ob keine Position offen ist.
- Jede Seite fasst vier Bedingungen in einem auf And gesetzten Logical condition zusammen: den Impuls von N values, den in diesem Augenblick erneut geprüften Vergleich der Durchschnitte, den Abstand zum Kanalrand und die fehlende Position. Nur wenn alle vier zusammen eintreffen, meldet sich das Gatter.
- Modify position eröffnet den Trade zum Marktpreis mit dem aus einer Variablen übernommenen Volumen, zwei weitere Modify position-Blöcke schließen ihn beim entgegengesetzten Vergleich, Position protection wird von jeder Einstiegsausführung scharf geschaltet, und das Chart-Panel zeichnet die Kerzen, beide Durchschnitte, beide Kanalextreme, alle Orders und alle Ausführungen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der schnelle Durchschnitt liegt über dem langsamen, der N values-Block dieser Seite gibt auf dieser Kerze seinen Impuls aus, der Schlusskurs liegt mindestens einen ATR-Schritt unter dem höchsten Hoch des Kanals, und es ist keine Position offen. Alle vier treffen im Long-And zusammen, und Modify position kauft das Ordervolumen zum Marktpreis. Der Block ist auf reines Eröffnen gestellt, sodass nichts gekauft wird, solange bereits eine Position gleich welcher Richtung gehalten wird.
- **Short-Einstieg**: Das Spiegelbild: Der schnelle Durchschnitt liegt unter dem langsamen, der bärische N values-Block gibt seinen Impuls aus, der Schlusskurs liegt mindestens einen ATR-Schritt über dem tiefsten Tief des Kanals, und es ist keine Position offen. Das Short-And gibt das Signal an einen Modify position-Block weiter, der das Ordervolumen zum Marktpreis verkauft — ebenfalls nur eröffnend.
- **Ausstieg**: Es gibt zwei Ausstiege. Der erste ist der Platztausch der Durchschnitte: Der nach dem Wechsel wahre Vergleich steuert einen Schließen-Block für die gehaltene Seite, und dieser Block gibt die gesamte Position zum Marktpreis. Der zweite ist Position protection — von jeder Einstiegsausführung scharf geschaltet, nimmt es den Gewinn bei 2% mit und begrenzt den Verlust bei 1%, gemessen am Schlusskurs abgeschlossener Kerzen, der in seinen Preiseingang geführt wird. Da beide Einstiegsblöcke ausschließlich eröffnen, eröffnet der Vergleich, der einen Trade beendet, nie die Gegenposition; der nächste Einstieg muss auf den nächsten Impuls warten, der das Konto ohne Position antrifft.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:15:00 | Zeitrahmen der Kerzen, auf denen das gesamte Diagramm arbeitet. Jede Zählung im Diagramm — die Durchschnitte, der ATR, der Kanal, die Wartezeit — wird in diesen Kerzen gemessen. |
| Fast SMA Length | 20 | Länge des schnellen Durchschnitts. Verkürzt man sie, tauschen die beiden Durchschnitte häufiger die Plätze, was den Warteblock öfter scharf schaltet und mehr Einstiege erzeugt. |
| Slow SMA Length | 60 | Länge des langsamen Durchschnitts. Der Abstand zwischen dieser und der schnellen Länge entscheidet, wie lange ein Trend anhalten muss, bevor er überhaupt erkannt wird. |
| Bull Streak Bars | 3 | Wie viele abgeschlossene Kerzen der N values-Block der Long-Seite zwischen dem Augenblick, in dem sich die Durchschnitte ausrichten, und dem von ihm ausgegebenen Impuls zählt. Bei eins steigt das Diagramm auf der Kerze nach der Kreuzung ein; ein großer Wert verzögert den Einstieg tief in die Bewegung hinein und vergrößert außerdem, da sich der Block nach jedem Impuls erneut scharf schaltet, den Abstand zwischen den Einstiegen. |
| Bear Streak Bars | 3 | Dieselbe Wartezeit auf der Short-Seite. Halten Sie sie gleich der Long-Seite, sofern die beiden Richtungen nicht über unterschiedlich lange Zeiträume bestätigt werden sollen. |
| ATR Length | 14 | Fenster des ATR, der die Volatilität misst. Es legt die Einheit fest, in der der Abstand zum Kanalrand ausgedrückt wird. |
| Step Multiplier | 2 | Wie viele ATRs Raum dem Preis bis zum Kanalrand bleiben müssen. Erhöht man den Wert, wird nur noch weit entfernt vom Extrem eingestiegen, was seltener vorkommt; senkt man ihn gegen null, verschwindet die Bedingung nahezu, und das verzögerte Trendsignal bleibt allein übrig. |
| Channel High Length | 20 | Über wie viele Kerzen das höchste Hoch genommen wird. Es ist die Decke, unter der ein Long-Einstieg um den Schrittabstand bleiben muss. |
| Channel Low Length | 20 | Über wie viele Kerzen das tiefste Tief genommen wird. Es ist der Boden, über dem ein Short-Einstieg um den Schrittabstand bleiben muss. Halten Sie den Wert gleich dem Fenster für das Hoch, sofern Sie keinen asymmetrischen Kanal anstreben. |
| Order Volume | 1 | Volumen, das beide Einstiegsblöcke senden. Die Schließen-Blöcke übernehmen kein Volumen: Sie senden, was die Position gerade hält. |
| Take Profit, % | 2 | Gewinnziel des Schutzblocks, als Prozentsatz des Ausführungspreises. |
| Stop Loss, % | 1 | Verlustbegrenzung des Schutzblocks, als Prozentsatz des Ausführungspreises. Zusammen mit dem Ziel entscheidet sie, wie viele Trades über den Schutz enden statt über den Platztausch der Durchschnitte. |

## Diagrammdetails

- Der N values-Block ist eine Verzögerung und kein Zähler aufeinanderfolgender Bars: Er wird vom ersten wahren Vergleich scharf geschaltet, den er sieht, zählt die folgenden abgeschlossenen Kerzen und meldet sich einmal. Er prüft nicht, ob die Bedingung über das gesamte Fenster hinweg galt — deshalb wird derselbe Vergleich im Augenblick des Impulses innerhalb des And erneut abgefragt: Ein während der Wartezeit zerfallener Trend besteht diese zweite Frage nicht, und es geht keine Order hinaus.
- Ein Logical condition löscht seine Eingänge, sobald es sich gemeldet hat, sodass das Einstiegsgatter nur auf den Kerzen bewertet werden kann, die einen Impuls tragen. Zwischen zwei Impulsen aktualisieren sich die Vergleiche weiter, doch nichts erreicht die Einstiegsblöcke — und genau das hindert ein Diagramm, dessen Bedingungen stundenlang wahr sind, daran, auf jeder Kerze zu handeln.
- Die Schutzniveaus werden mit demselben ATR gezogen, der die Volatilität misst: Der Abstand, den der Preis zum Kanalrand halten muss, wächst also, wenn sich der Markt schneller bewegt, und schrumpft, wenn er ruhiger wird. Beide Kanalindikatoren beziehen die gerade bewertete Kerze mit ein, sodass das Niveau den Extremen folgt, statt ihnen hinterherzuhinken.
- Die Konstanten des Diagramms — die Null, das Ordervolumen und der Schrittmultiplikator — werden alle vom Kerzenstrom ausgelöst, sodass ihre Werte auf demselben Tick eintreffen wie die Indikatorwerte, mit denen sie verglichen und multipliziert werden.
- Die Schließen-Blöcke hängen direkt an den Vergleichen der Durchschnitte und werden daher auf jeder Kerze ausgelöst, auf der die Durchschnitte in dieser Reihenfolge stehen. Die Filterung übernimmt ihre Schließbedingung: Gibt es auf dieser Seite keine Position, tut der Block überhaupt nichts, und es verlässt keine Order das Diagramm.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
