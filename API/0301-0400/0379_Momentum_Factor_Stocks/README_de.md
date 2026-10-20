# Momentum Factor Stocks

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Monatlich LookbackDays + 1 abgeschlossene Tageskurse verlangen. Momentum = P[d−SkipDays] / P[d−LookbackDays] − 1; SkipDays muss kleiner als LookbackDays sein. Die letzten SkipDays Renditeintervalle werden ausgelassen; es werden nicht zwei ROC-Indikatoren subtrahiert. Das höchste Quintile-Segment kaufen und das niedrigste verkaufen, jeweils floor(geeignete Anzahl / Quintile). Gerundete native Mengen bis zur nächsten Monatsprüfung halten.

Nur abgeschlossene Kerzen mit gleichem Zeitstempel werden gemeinsam ausgewertet. Statistiken zählen verschiedene UTC-Daten; weitere Intraday-Kerzen ersetzen den Tageswert. Die erste geeignete gemeinsame Kerze jedes Monats löst genau eine Umschichtung aus. Unvollständige Daten verschieben sie. Gleiche Werte werden nach Security.Id sortiert.

Alle Beobachtungen müssen bis zum Schluss der Signalkerze veröffentlicht sein. Intraday zählen nur Tageskurse vor dem aktuellen UTC-Datum; eine abgeschlossene Tageskerze zählt ihr eigenes Datum. Zukünftige CSV-Daten dürfen keine Entscheidung vorwärmen.

## Eingabedaten

PriceHistoryFile ist eine optionale CSV positiver Tagesschlusskurse: SecurityId,Date,Close; Datum yyyy-MM-dd. Aktuelle und zukünftige Daten dürfen eine Entscheidung nicht vorwärmen. Live-Schlusskurse ersetzen den jeweiligen UTC-Tageswert. Doppelte Daten sind ungültig. Die Datei ermöglicht lange Rückblicke bei kurzen Ausführungsarchiven.

## Parameter

| Parameter | Standard |
|---|---|
| `Universe` | erforderlich |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | optional — nicht gesetzt |
| `LookbackDays` | `252` |
| `SkipDays` | `21` |
| `Quintile` | `5` |

## Ausführung

Universe muss explizit angegeben werden. Preise und Capital sind in USD je gehandelter Einheit angegeben. Capital ist ein festes Budget; Leverage bestimmt das Bruttoengagement. Es werden keine Stop-Loss- oder Take-Profit-Orders hinzugefügt.

Jede Seite erhält die Hälfte von Capital × Leverage, gleichmäßig auf ihre Titel verteilt. Stückzahlen basieren auf dem Signalschlusskurs und werden zu null auf VolumeStep gerundet. Tatsächliche Ausführungen bestimmen den Bestand. Offene Orders werden abgewartet, MinVolume/MaxVolume eingehalten und große Anpassungen auf spätere Kerzen verteilt. MinTradeUsd gilt nicht für vollständige Glattstellungen.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Ausführungsdaten sowie expliziten Zins-, Finanz-, Bewertungs-, Text- und Aufwärmfixtures geprüft. Kalenderfixtures verschieben Archiv-OHLC/Volumina und setzen Ausführungskurse; dies ist keine mehrjährige Markthistorie. Unabhängige Formeln, Orderrichtung/-menge, echte Ausführungen, Restpositionen und gesperrte Termine werden geprüft. Zwei Gruppen, K=1 und AssetDecile=1 sind deklarierte Mechanikprofile, kein Nachweis für ein vollständiges Handelsuniversum. Keine Renditezusage.
