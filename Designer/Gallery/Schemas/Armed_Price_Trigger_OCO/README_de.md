# Strategiediagramm für scharfgeschaltete OCO-Preistrigger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm verwaltet zwei virtuelle Ausbruchstrigger um einen Donchian-Kanal, der aus abgeschlossenen Fünf-Minuten-Kerzen berechnet wird. Der beste Brief- und Geldkurs aus dem laufenden Orderbuch wird mit den letzten Kanalgrenzen verglichen; die erste zulässige Seite eröffnet eine Marktposition, die anschließend durch prozentuale Gewinn- und Verlustschwellen geschützt wird.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen speisen Donchian Channels(20), der die obere und untere Ausbruchsgrenze erzeugt.
- Bei jeder Markttiefenaktualisierung werden die letzten gebildeten Kanalwerte abgetastet und `BestAsk.Price` sowie `BestBid.Price` aus dem Orderbuch gelesen.
- Ein oberer Ausbruch darf kaufen und ein unterer Ausbruch darf verkaufen, solange `Armed` aktiviert und die Position null ist.
- Ein Flag-Baustein lässt den ersten gültigen Impuls jeder Seite durch und unterdrückt Wiederholungen, bis eine Positionsänderung beide Einmal-Gates zurücksetzt.
- Der Positionsschutz schließt den ausgeführten Einstieg bei 1% Gewinn oder 0,6% Verlust; das Diagramm zeigt Kerzen, beide Kanalgrenzen und alle Ausführungen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn `Armed` true ist, die Position null ist und der beste Briefkurs mindestens die letzte obere Donchian-Grenze erreicht, wird ein Marktkauf mit `Volume` gesendet.
- **Short-Einstieg**: Wenn `Armed` true ist, die Position null ist und der beste Geldkurs höchstens die letzte untere Donchian-Grenze erreicht, wird ein Marktverkauf mit `Volume` gesendet.
- **Ausstieg**: Die offene Position wird geschlossen, sobald das laufende Orderbuch das Gewinnziel von 1% oder die Verlustschwelle von 0,6% erreicht, jeweils vom Einstiegskurs aus gemessen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Channel Length | 20 | Anzahl abgeschlossener Kerzen für Donchian Channels. |
| Armed | true | Hauptschalter für beide Ausbruchseinstiege. |
| Take Profit, % | 1 | Prozentualer Abstand vom Einstiegskurs zum Gewinnziel. |
| Stop Loss, % | 0.6 | Prozentualer Abstand vom Einstiegskurs zur Verlustschwelle. |
| Volume | 1 | Volumen der Marktorder für beide Einstiegsrichtungen. |
| Candles | 00:05:00 | Zeitrahmen der abgeschlossenen Kerzen zur Kanalberechnung. |

## Diagrammdetails

- Die Konverter UpperBand und LowerBand extrahieren beide Donchian-Grenzen. Zwei Variable-Bausteine speichern sie und geben ihre letzten Werte im kausalen Kontext jeder Markttiefenaktualisierung aus.
- Solange Donchian Channels(20) noch nicht gebildet ist, besitzen die Grenzvariablen keinen gespeicherten Wert und die Einstiegsvergleiche bleiben inaktiv.
- Die Orderbuch-Konverterpfade `BestAsk.Price` und `BestBid.Price` liefern die laufenden Preise für die beiden Vergleiche.
- Jedes Einstiegsgate verknüpft drei boolesche Eingaben: den passenden Preisvergleich, `position = 0` und den freigegebenen Parameter `Armed`.
- Die beiden Flag-Bausteine bilden ein virtuelles OCO-Verhalten für einen Impuls, ohne wartende Börsenorders zu platzieren. Nach Ausführung einer Seite sperrt die Position ungleich null beide Einstiege, bis der Schutz sie schließt.
- Beide Modify-position-Bausteine verwenden Marktorders. Ihre Einstiegsausführungen speisen Position protection direkt, während die Markttiefe die Preise zur Prüfung der Ausstiegsniveaus liefert.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
