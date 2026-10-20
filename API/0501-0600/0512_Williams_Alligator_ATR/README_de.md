# Williams Alligator ATR

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Alle drei Alligator-Linien glätten Medianpreise (Hoch+Tief)/2. Length und Shift bestimmen Glättung und Verzögerung in abgeschlossenen Kerzen. Teeth wird berechnet und muss gebildet sein, ist aber kein zusätzlicher Einstiegsfilter. Sind alle Linien und Wilder-ATR gebildet, öffnet Lips−Jaw von <=0 nach >0 aus leerer Position einen Volume-Long. Von >=0 nach <0 wird der tatsächliche Long vollständig geschlossen. Niemals Short eröffnen. Beim Einstieg ATR×AtrMultiplier als nativen lokalen Marktstop-Abstand zur Ausführung fixieren. Bid/Ask können zwischen Kerzen auslösen. Keine Wartebalken, Gewinnmitnahme oder nachträgliche ATR-Stopverschiebung.

## Parameter

| Parameter | Standard |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `JawLength` | 13 |
| `TeethLength` | 8 |
| `LipsLength` | 5 |
| `JawShift` | 8 |
| `TeethShift` | 5 |
| `LipsShift` | 3 |
| `AtrPeriod` | 14 |
| `AtrMultiplier` | 2 |

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Daten, unabhängigen Formeln, Orderseiten/-mengen, Ausführungen, Positionen und Ausstiegen geprüft. Datierte Preis-/Faktor- und Kalenderfixtures prüfen Mechanik; verschobene OHLC/Volumina sind keine mehrjährige Marktgeschichte. Zwei Ranggruppen, TopN=1 und SMA=2 sind ausdrücklich kleine Testprofile. Lange Originalfenster und Standardwerte werden separat geprüft. Keine Renditezusage.
