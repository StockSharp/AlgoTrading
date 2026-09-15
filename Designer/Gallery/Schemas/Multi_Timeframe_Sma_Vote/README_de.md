# Diagramm der Multi-Timeframe-SMA-Abstimmungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein einzelner gleitender Durchschnitt, der nach oben dreht, sagt wenig aus; drehen jedoch drei zugleich nach oben, auf drei verschiedenen Zeitrahmen, dann ist das ein handelbarer Trend. Jeder Zeitrahmen gibt eine Stimme von plus eins, minus eins oder null ab, Sync fasst die drei Stimmen zu einem Satz zusammen, und eine Position wird nur eröffnet, wenn die Abstimmung einstimmig ausfällt.

![schema](schema.svg)

## Strategieübersicht

- Drei Kerzenblöcke lesen dasselbe Instrument in fünf Minuten, fünfzehn Minuten und einer Stunde, und jeder von ihnen gibt ausschließlich abgeschlossene Kerzen weiter.
- Jede Reihe speist ihren eigenen einfachen gleitenden Durchschnitt, und ein Previous value-Block hält denselben Durchschnitt so fest, wie er einen Balken zuvor stand.
- Eine Formel macht aus dem Paar eine Stimme: plus eins, wenn der Durchschnitt über seinem früheren Wert liegt, minus eins, wenn er darunter liegt, null, wenn er sich nicht bewegt hat.
- Die Fünf-Minuten- und die Fünfzehn-Minuten-Stimme werden in Variablen gehalten und von der Stundenstimme freigegeben, sodass alle drei Linien Sync als ein und derselbe Zeitpunkt erreichen.
- Sync hält je eine Linie pro Stimme und lässt die drei gemeinsam weiter; ohne ihn träfe der Stundenwert eine Stunde nach dem Fünf-Minuten-Wert ein und die drei ließen sich nie vergleichen.
- Nach Sync wird jede Stimme mit null verglichen, und zwei logische Bedingungen stellen die einzige Frage, auf die es im Diagramm ankommt: Zeigen alle drei in dieselbe Richtung?
- Das Urteil wird auf die nächste abgeschlossene Fünf-Minuten-Kerze gerastet, den Takt, auf den jede Order datiert wird, und Position modify eröffnet zum Marktpreis aus einer flachen Position.
- Die Position, im selben Fünf-Minuten-Takt abgetastet, wird mit null verglichen: Ein gegenteiliges Urteil gegen eine offene Position schließt sie zum Marktpreis, und die neue Seite wird auf der folgenden Kerze eingegangen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Alle drei Stimmen stehen im Stundentakt auf plus eins: Der Fünf-Minuten-, der Fünfzehn-Minuten- und der Stunden-Durchschnitt liegen jeweils über ihrem eigenen Wert einen Balken zuvor. Das Urteil wird auf die nächste abgeschlossene Fünf-Minuten-Kerze übertragen, wo Position modify das Ordervolumen zum Marktpreis kauft, und das nur aus einer flachen Position.
- **Short-Einstieg**: Alle drei Stimmen stehen im selben Takt auf minus eins: Jeder Durchschnitt liegt unter seinem eigenen Wert einen Balken zuvor. Auf der nächsten abgeschlossenen Fünf-Minuten-Kerze verkauft Position modify das Ordervolumen zum Marktpreis, auch hier nur aus einer flachen Position.
- **Ausstieg**: Es gibt weder Take-Profit noch Stop-Loss noch Timer. Eine Position besteht so lange, bis sich die drei Zeitrahmen andersherum ausrichten: Das gegenteilige Urteil schließt sie zusammen mit dem Positionsvergleich auf der Handelskerze zum Marktpreis, und der Einstieg in die neue Seite folgt auf der nächsten Kerze, sobald die Position wieder flach ist. Eine gemischte Abstimmung ändert nichts und lässt die Position unangetastet.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Fast Candles | 00:05:00 | Zeitrahmen der schnellsten Stimme und zugleich der Takt, auf dem Urteile umgesetzt und Orders datiert werden. |
| Medium Candles | 00:15:00 | Zeitrahmen der mittleren Stimme. |
| Slow Candles | 01:00:00 | Zeitrahmen der langsamsten Stimme und zugleich der Takt, auf dem die drei Stimmen gesammelt und ausgezählt werden. |
| Fast SMA Length | 13 | Glättungslänge des Fünf-Minuten-Durchschnitts. |
| Medium SMA Length | 13 | Glättungslänge des Fünfzehn-Minuten-Durchschnitts. |
| Slow SMA Length | 13 | Glättungslänge des Stunden-Durchschnitts. |
| Order Volume | 1 | Ordergröße in Lots, für beide Einstiegsseiten verwendet; ein Ausstieg schließt, was die Position gerade hält. |

## Diagrammdetails

- Alle drei Durchschnitte geben ausschließlich formierte, endgültige Werte aus, sodass eine noch entstehende Kerze eine Stimme niemals bewegen kann.
- Erst die Reduktion jedes Zeitrahmens auf das Vorzeichen seiner Steigung macht die drei überhaupt vergleichbar: Ein Stunden-Durchschnitt und ein Fünf-Minuten-Durchschnitt leben auf verschiedenen Skalen, plus eins und minus eins dagegen nicht.
- Die beiden schnelleren Stimmen gelangen über eine Variable in Sync, die von der Stundenstimme freigegeben wird. Das ist Absicht: Linien, die einzeln nacheinander eintreffen, ließen Sync auf einem Satz sitzen, der nie vollständig wird, und alles dahinter würde nie wieder auslösen.
- Sync versieht den freigegebenen Satz mit dem Zeitstempel des Moments, zu dem der Satz gehört, und dieser liegt hinter der Uhr zurück, sobald die Stunde geschlossen ist. Auf dem Weg zur Order liest nichts diesen Stempel: Das Urteil wird ein zweites Mal auf die abgeschlossene Fünf-Minuten-Kerze gerastet, die den Handel trägt.
- Beide Einstiege sind an eine offene Position gebunden, sodass ein auf jeder Fünf-Minuten-Kerze wiederholtes Urteil keine Orders stapeln kann: Solange eine Position besteht, wird die Wiederholung schlicht abgelehnt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
