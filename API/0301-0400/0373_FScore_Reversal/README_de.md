# F-Score Reversal

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Monatlich die Lookback-Tagesrendite P[d] / P[d−Lookback] − 1 berechnen. Unter veröffentlichten FScore >= FHi den schwächsten Titel kaufen; bei IsShortEnabled unter FScore <= FLo den stärksten leerverkaufen. Grenzen sind inklusive, FHi muss größer als FLo sein. Echte veröffentlichte Piotroski-Werte sind ganze Zahlen 0..9; jüngste bekannte Periode/Revision und MaxReportAgeDays beachten. Jede ausgewählte Seite erhält das halbe Bruttobudget; eine fehlende Seite bleibt Bargeld. Vor den neuen Monatspositionen den alten Bestand vollständig schließen und Ausführungen abwarten.

Nur abgeschlossene Kerzen mit gleichem Zeitstempel werden gemeinsam ausgewertet. Statistiken zählen verschiedene UTC-Daten; weitere Intraday-Kerzen ersetzen den Tageswert. Die erste geeignete gemeinsame Kerze jedes Monats löst genau eine Umschichtung aus. Unvollständige Daten verschieben sie. Gleiche Werte werden nach Security.Id sortiert.

Alle Beobachtungen müssen bis zum Schluss der Signalkerze veröffentlicht sein. Intraday zählen nur Tageskurse vor dem aktuellen UTC-Datum; eine abgeschlossene Tageskerze zählt ihr eigenes Datum. Zukünftige CSV-Daten dürfen keine Entscheidung vorwärmen.

Die Veröffentlichungsgrenze ist die späteste tatsächliche CloseTime der synchronisierten abgeschlossenen Kerzen. Eine Handelssitzung kann vor Ablauf des eingestellten Kerzenintervalls enden.

## Eingabedaten

PriceHistoryFile ist eine optionale CSV positiver Tagesschlusskurse: SecurityId,Date,Close; Datum yyyy-MM-dd. Aktuelle und zukünftige Daten dürfen eine Entscheidung nicht vorwärmen. Live-Schlusskurse ersetzen den jeweiligen UTC-Tageswert. Doppelte Daten sind ungültig. Die Datei ermöglicht lange Rückblicke bei kurzen Ausführungsarchiven.

FScoreFile ist erforderlich: `SecurityId,PeriodEnd,PublishedAt,FScore`. PeriodEnd: `yyyy-MM-dd`; PublishedAt: UTC `yyyy-MM-ddTHH:mm:ssZ`. Nur ganze Werte 0..9, eindeutige Revisionen und Veröffentlichungen ab PeriodEnd sind erlaubt. Jüngste bekannte Periode/Revision wählen; MaxReportAgeDays zählt seit PeriodEnd.

## Parameter

| Parameter | Standard |
|---|---|
| `Universe` | erforderlich |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | optional — nicht gesetzt |
| `Lookback` | `21` |
| `FHi` | `7` |
| `FLo` | `3` |
| `IsShortEnabled` | `true` |
| `MaxReportAgeDays` | `550` |
| `FScoreFile` | erforderlich |

## Ausführung

Universe muss explizit angegeben werden. Preise und Capital sind in USD je gehandelter Einheit angegeben. Capital ist ein festes Budget; Leverage bestimmt das Bruttoengagement. Es werden keine Stop-Loss- oder Take-Profit-Orders hinzugefügt.

Zielmengen zu null auf VolumeStep runden. Vor neuen Positionen tatsächliche Vollschließung abwarten; ausgeführte Positionen abgleichen, MinVolume/MaxVolume beachten und große Anpassungen auf spätere Kerzen verteilen. MinTradeUsd filtert Anpassungen auf Nichtnullziele; vollständige Schließung umgeht den Filter.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Ausführungsdaten sowie expliziten Zins-, Finanz-, Bewertungs-, Text- und Aufwärmfixtures geprüft. Kalenderfixtures verschieben Archiv-OHLC/Volumina und setzen Ausführungskurse; dies ist keine mehrjährige Markthistorie. Unabhängige Formeln, Orderrichtung/-menge, echte Ausführungen, Restpositionen und gesperrte Termine werden geprüft. Zwei Gruppen, K=1 und AssetDecile=1 sind deklarierte Mechanikprofile, kein Nachweis für ein vollständiges Handelsuniversum. Keine Renditezusage.
