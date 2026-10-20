# Lexical Density Filings

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Die jüngste veröffentlichte Klartextmeldung jedes Titels lesen. Tokens sind Unicode-Buchstaben/Markierungen mit invarianter .NET-Kleinschreibung. Lexikalische Dichte = verschiedene normalisierte Tokens / alle Tokens. Nur an den ersten drei Handelstagen im Februar, Mai, August und November nach dem Börsenkalender des ersten Titels investieren. Innerhalb dieses Fensters auf mindestens Quintile Berichte warten; sonst den nächsten Prüfmonat abwarten. Höchste Dichte kaufen, niedrigste verkaufen, jeweils floor(geeignete Anzahl / Quintile). Dazwischen Bestände halten.

Alle Beobachtungen müssen bis zum Schluss der Signalkerze veröffentlicht sein. Intraday zählen nur Tageskurse vor dem aktuellen UTC-Datum; eine abgeschlossene Tageskerze zählt ihr eigenes Datum. Zukünftige CSV-Daten dürfen keine Entscheidung vorwärmen.

Die Veröffentlichungsgrenze ist die späteste tatsächliche CloseTime der synchronisierten abgeschlossenen Kerzen. Eine Handelssitzung kann vor Ablauf des eingestellten Kerzenintervalls enden.

## Eingabedaten

FilingsFile ist erforderlich: `SecurityId,PeriodEnd,PublishedAt,DocumentPath`. Datumsformate: `yyyy-MM-dd` und UTC `yyyy-MM-ddTHH:mm:ssZ`. DocumentPath verweist auf UTF-8-Klartext, absolut oder relativ zum Manifest. Keine Kommas im Pfad. Leere Texte, doppelte Revisionen und Veröffentlichung vor PeriodEnd sind ungültig. Nur bis zum Kerzenschluss veröffentlichte jüngste Periode/Revision bis MaxReportAgeDays verwenden. Der erste Titel braucht einen Börsenkalender mit Feiertagen.

Wortgrenzen folgen Unicode-Textelementen mit Buchstabe oder kombinierendem Zeichen als Anfangskategorie; Satzzeichen und Ziffern trennen Tokens. Invariante Groß-/Kleinschreibung, ohne Stemming oder Akzentnormalisierung.

## Parameter

| Parameter | Standard |
|---|---|
| `Universe` | erforderlich |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `Quintile` | `5` |
| `MaxReportAgeDays` | `550` |
| `FilingsFile` | erforderlich |

## Ausführung

Universe muss explizit angegeben werden. Preise und Capital sind in USD je gehandelter Einheit angegeben. Capital ist ein festes Budget; Leverage bestimmt das Bruttoengagement. Es werden keine Stop-Loss- oder Take-Profit-Orders hinzugefügt.

Jede Seite erhält die Hälfte von Capital × Leverage, gleichmäßig auf ihre Titel verteilt. Stückzahlen basieren auf dem Signalschlusskurs und werden zu null auf VolumeStep gerundet. Tatsächliche Ausführungen bestimmen den Bestand. Offene Orders werden abgewartet, MinVolume/MaxVolume eingehalten und große Anpassungen auf spätere Kerzen verteilt. MinTradeUsd gilt nicht für vollständige Glattstellungen.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Ausführungsdaten sowie expliziten Zins-, Finanz-, Bewertungs-, Text- und Aufwärmfixtures geprüft. Kalenderfixtures verschieben Archiv-OHLC/Volumina und setzen Ausführungskurse; dies ist keine mehrjährige Markthistorie. Unabhängige Formeln, Orderrichtung/-menge, echte Ausführungen, Restpositionen und gesperrte Termine werden geprüft. Zwei Gruppen, K=1 und AssetDecile=1 sind deklarierte Mechanikprofile, kein Nachweis für ein vollständiges Handelsuniversum. Keine Renditezusage.
