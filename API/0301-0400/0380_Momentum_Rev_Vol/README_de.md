# Momentum Rev Vol

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Je Titel werden verschiedene abgeschlossene Tagesschlusskurse P verwendet: M = P[t]/P[t−Lookback12]−1, R = P[t]/P[t−Lookback1]−1. V ist die Stichproben-Standardabweichung der letzten VolWindow einfachen Tagesrenditen (Divisor N−1). Bewertung = WM×M − WR×R − WV×V, ohne Querschnittsnormalisierung. Absteigend sortieren: floor(n/Decile) Gewinner kaufen und ebenso viele Verlierer leerverkaufen. Mindestens Decile geeignete Titel sind nötig. Mengen bleiben zwischen Monatsprüfungen fixiert.

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
| `CandleType` | TimeSpan.FromMinutes(1440).TimeFrame() |
| `PriceHistoryFile` | nicht gesetzt |
| `Lookback12` | 252 |
| `Lookback1` | 21 |
| `VolWindow` | 60 |
| `WM` | 1 |
| `WR` | 1 |
| `WV` | 1 |
| `Decile` | 10 |

## Eingabedaten

Optionale `PriceHistoryFile`-CSV: `SecurityId,Date,Close`, positive Kurse, eindeutige Titel/Datum-Paare, Datum `yyyy-MM-dd`. Intraday nur frühere UTC-Tage verwenden; eine abgeschlossene Tageskerze trägt ihren Schlusskurs bei. Zukünftige Zeilen greifen früheren Prüfungen nicht vor. Ausführungskerzen ersetzen ihren Tageswert.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Daten, unabhängigen Formeln, Orderseiten/-mengen, Ausführungen, Positionen und Ausstiegen geprüft. Datierte Preis-/Faktor- und Kalenderfixtures prüfen Mechanik; verschobene OHLC/Volumina sind keine mehrjährige Marktgeschichte. Zwei Ranggruppen, TopN=1 und SMA=2 sind ausdrücklich kleine Testprofile. Lange Originalfenster und Standardwerte werden separat geprüft. Keine Renditezusage.
