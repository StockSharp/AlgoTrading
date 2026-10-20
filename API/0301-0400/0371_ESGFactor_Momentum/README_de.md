# ESG Factor Momentum

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Universe enthält bereits nach ESG gefilterte Titel; die Strategie erzeugt kein ESG-Rating. Momentum = P[d] / P[d−LookbackDays] − 1 aus LookbackDays + 1 abgeschlossenen Tageskursen. Monatlich nur den besten Titel kaufen, auch bei ausschließlich negativen Renditen. Vor jedem Neukauf alle bisherigen Positionen vollständig schließen und die Ausführungen abwarten, selbst bei unverändertem Gewinner. Das Budget ist durch den gemeldeten Portfoliowert begrenzt; keine Hebelung oder Leerverkäufe.

Nur abgeschlossene Kerzen mit gleichem Zeitstempel werden gemeinsam ausgewertet. Statistiken zählen verschiedene UTC-Daten; weitere Intraday-Kerzen ersetzen den Tageswert. Die erste geeignete gemeinsame Kerze jedes Monats löst genau eine Umschichtung aus. Unvollständige Daten verschieben sie. Gleiche Werte werden nach Security.Id sortiert.

Alle Beobachtungen müssen bis zum Schluss der Signalkerze veröffentlicht sein. Intraday zählen nur Tageskurse vor dem aktuellen UTC-Datum; eine abgeschlossene Tageskerze zählt ihr eigenes Datum. Zukünftige CSV-Daten dürfen keine Entscheidung vorwärmen.

## Eingabedaten

PriceHistoryFile ist eine optionale CSV positiver Tagesschlusskurse: SecurityId,Date,Close; Datum yyyy-MM-dd. Aktuelle und zukünftige Daten dürfen eine Entscheidung nicht vorwärmen. Live-Schlusskurse ersetzen den jeweiligen UTC-Tageswert. Doppelte Daten sind ungültig. Die Datei ermöglicht lange Rückblicke bei kurzen Ausführungsarchiven.

## Parameter

| Parameter | Standard |
|---|---|
| `Universe` | erforderlich |
| `Capital` | `100000` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | optional — nicht gesetzt |
| `LookbackDays` | `252` |

## Ausführung

Universe ausdrücklich angeben. Preise und Capital in USD je Handelseinheit. Long-Exposure ist auf min(Capital, gemeldeter Portfolio.CurrentValue) begrenzt; ohne positiven gemeldeten Wert kein Kaufbudget. Keine Hebelung, Stop-Loss- oder Gewinnmitnahmeorders.

Zielmengen zu null auf VolumeStep runden. Vor neuen Positionen tatsächliche Vollschließung abwarten; ausgeführte Positionen abgleichen, MinVolume/MaxVolume beachten und große Anpassungen auf spätere Kerzen verteilen. MinTradeUsd filtert Anpassungen auf Nichtnullziele; vollständige Schließung umgeht den Filter.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Ausführungsdaten sowie expliziten Zins-, Finanz-, Bewertungs-, Text- und Aufwärmfixtures geprüft. Kalenderfixtures verschieben Archiv-OHLC/Volumina und setzen Ausführungskurse; dies ist keine mehrjährige Markthistorie. Unabhängige Formeln, Orderrichtung/-menge, echte Ausführungen, Restpositionen und gesperrte Termine werden geprüft. Zwei Gruppen, K=1 und AssetDecile=1 sind deklarierte Mechanikprofile, kein Nachweis für ein vollständiges Handelsuniversum. Keine Renditezusage.
