# Return Asymmetry Commodity

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Aus WindowDays abgeschlossenen Tagesrenditen r: U = Σmax(r,0), D = Σmax(−r,0). Nach (U−D)/(U+D) ordnen, bei U+D=0 mit Wert null. Die begrenzte Größe erhält die Reihenfolge von U/D auch bei ausschließlich positiven oder negativen Renditen, ohne künstlichen Mindestnenner. Sie misst die Auf-/Abwärtsbilanz, nicht die statistische Schiefe. TopN Gewinner kaufen, TopN andere Verlierer leerverkaufen; mindestens 2×TopN geeignete Titel nötig.

Universe muss explizit angegeben werden. Preise und Capital sind in USD je gehandelter Einheit angegeben. Capital ist ein festes Budget; Leverage bestimmt das Bruttoengagement. Es werden keine Stop-Loss- oder Take-Profit-Orders hinzugefügt.

Nur abgeschlossene Kerzen mit gleichem Zeitstempel werden gemeinsam ausgewertet. Statistiken zählen verschiedene UTC-Daten; weitere Intraday-Kerzen ersetzen den Tageswert. Die erste geeignete gemeinsame Kerze jedes Monats löst genau eine Umschichtung aus. Unvollständige Daten verschieben sie. Gleiche Werte werden nach Security.Id sortiert.

Jede Seite erhält die Hälfte von Capital × Leverage, gleichmäßig auf ihre Titel verteilt. Stückzahlen basieren auf dem Signalschlusskurs und werden zu null auf VolumeStep gerundet. Tatsächliche Ausführungen bestimmen den Bestand. Offene Orders werden abgewartet, MinVolume/MaxVolume eingehalten und große Anpassungen auf spätere Kerzen verteilt. MinTradeUsd gilt nicht für vollständige Glattstellungen.

## Parameter

| Parameter | Standard |
|---|---|
| `Universe` | erforderlich |
| `Capital` | 100000 |
| `Leverage` | 1 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `PriceHistoryFile` | nicht gesetzt |
| `WindowDays` | 120 |
| `TopN` | 5 |

## Eingabedaten

Optionale `PriceHistoryFile`-CSV: `SecurityId,Date,Close`, positive Kurse, eindeutige Titel/Datum-Paare, Datum `yyyy-MM-dd`. Intraday nur frühere UTC-Tage verwenden; eine abgeschlossene Tageskerze trägt ihren Schlusskurs bei. Zukünftige Zeilen greifen früheren Prüfungen nicht vor. Ausführungskerzen ersetzen ihren Tageswert.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Daten, unabhängigen Formeln, Orderseiten/-mengen, Ausführungen, Positionen und Ausstiegen geprüft. Datierte Preis-/Faktor- und Kalenderfixtures prüfen Mechanik; verschobene OHLC/Volumina sind keine mehrjährige Marktgeschichte. Zwei Ranggruppen, TopN=1 und SMA=2 sind ausdrücklich kleine Testprofile. Lange Originalfenster und Standardwerte werden separat geprüft. Keine Renditezusage.
