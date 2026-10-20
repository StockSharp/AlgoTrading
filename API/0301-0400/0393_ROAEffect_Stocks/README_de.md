# ROAEffect Stocks

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

RoaFile liefert extern veröffentlichte Gesamtkapitalrenditen in einheitlichen Dezimaleinheiten. Kein Wert wird aus Kerzenkörper oder Handelsspanne abgeleitet. Monatlich neueste zulässige ROA absteigend ordnen, floor(n/Decile) Gewinner kaufen und ebenso viele Verlierer leerverkaufen. Mindestens Decile geeignete Titel nötig.

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
| `RoaFile` | erforderlich |
| `Decile` | 10 |
| `MaxScoreAgeDays` | 400 |

## Eingabedaten

Erforderliche Bewertungs-CSV: `SecurityId,PeriodEnd,PublishedAt,Score`. PeriodEnd: `yyyy-MM-dd`; PublishedAt: UTC `yyyy-MM-ddTHH:mm:ssZ`; Score dezimal. Doppelte Revisionen und Veröffentlichung vor Periodenende ablehnen. Neueste Periode und jüngste bis zum tatsächlichen gemeinsamen CloseTime veröffentlichte Revision wählen. `MaxScoreAgeDays` zählt seit PeriodEnd. Fehlende, veraltete oder zu wenige Titel verschieben die Prüfung und erhalten Bestände; Preise ersetzen keine Bewertung.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Daten, unabhängigen Formeln, Orderseiten/-mengen, Ausführungen, Positionen und Ausstiegen geprüft. Datierte Preis-/Faktor- und Kalenderfixtures prüfen Mechanik; verschobene OHLC/Volumina sind keine mehrjährige Marktgeschichte. Zwei Ranggruppen, TopN=1 und SMA=2 sind ausdrücklich kleine Testprofile. Lange Originalfenster und Standardwerte werden separat geprüft. Keine Renditezusage.
