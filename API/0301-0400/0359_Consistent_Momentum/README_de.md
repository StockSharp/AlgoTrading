# Consistent Momentum

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Langes Momentum P[d] / P[d − LookbackDays] − 1 und kurzes Momentum P[d] / P[d − ShortLookbackDays] − 1 verwenden verschiedene Tagesdaten. LookbackDays + 1 Kurse sind nötig. Beide getrennt sortieren: Schnittmenge der höchsten Deciles kaufen, Schnittmenge der niedrigsten verkaufen, je floor(Universe-Größe / Deciles) Kandidaten. Monatlich eine Tranche mit Capital × Leverage / HoldingMonths Bruttobudget, halb je Seite; leere Seiten bleiben Bargeld. Stückzahlen und Ablaufmonat bei Erstellung fixieren. Ablauf ist Erstellungsmonat plus HoldingMonths. Nur die Änderung der Summe lebender Tranchen handeln. Spätere Parameteränderungen verschieben alte Abläufe nicht.

Nur abgeschlossene Kerzen mit gleichem Zeitstempel werden gemeinsam ausgewertet. Statistiken zählen verschiedene UTC-Daten; weitere Intraday-Kerzen ersetzen den Tageswert. Die erste geeignete gemeinsame Kerze jedes Monats löst genau eine Umschichtung aus. Unvollständige Daten verschieben sie. Gleiche Werte werden nach Security.Id sortiert.

Bei Intraday-CandleType zählen nur UTC-Daten vor dem aktuellen Signaldatum. Der unvollständige aktuelle Tag bleibt ausgeschlossen. Eine abgeschlossene Tageskerze darf ihr eigenes vollständiges Datum beitragen.

## Eingabedaten

PriceHistoryFile ist eine optionale CSV positiver Tagesschlusskurse: SecurityId,Date,Close; Datum yyyy-MM-dd. Aktuelle und zukünftige Daten dürfen eine Entscheidung nicht vorwärmen. Live-Schlusskurse ersetzen den jeweiligen UTC-Tageswert. Doppelte Daten sind ungültig. Die Datei ermöglicht lange Rückblicke bei kurzen Ausführungsarchiven.

## Parameter

| Parameter | Standardwert |
|---|---|
| `Universe` | erforderlich |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `50` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `PriceHistoryFile` | optional — nicht gesetzt |
| `LookbackDays` | `147` |
| `ShortLookbackDays` | `21` |
| `HoldingMonths` | `6` |

## Ausführung

Universe muss explizit angegeben werden. Preise und Capital sind in USD je gehandelter Einheit angegeben. Capital ist ein festes Budget; Leverage bestimmt das Bruttoengagement. Es werden keine Stop-Loss- oder Take-Profit-Orders hinzugefügt.

Stückzahlen basieren auf dem Signalschlusskurs und werden zu null auf VolumeStep gerundet. Tatsächliche Ausführungen bestimmen den Bestand. Offene Orders werden abgewartet, MinVolume/MaxVolume eingehalten und große Anpassungen auf spätere Kerzen verteilt. MinTradeUsd gilt nicht für vollständige Glattstellungen.

## Prüfung

Ausführungen werden ausschließlich mit dem vorhandenen BTC/TON-Archiv geprüft. Explizite Berichts-, Options- und Vorwärmdaten sind mechanische Testdaten, keine echten Unternehmens- oder Optionsdaten. Kürzere Fenster, zwei Gruppen und März als jährlicher Termin sind benannte Testprofile, kein Nachweis für unveränderte langfristige Standardwerte. Es wird keine Rendite behauptet.
