# Fed Model

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Universe-Reihenfolge: [Aktien-ETF, optionaler Geldmarkt-ETF]. Nur Tageskerzen und den Börsenkalender des ersten Titels verwenden. Am Schluss des letzten Handelstags jedes Monats sind RegressionMonths aufeinanderfolgende, bis dahin veröffentlichte Monatswerte nötig. Mit Achsenabschnitt y = EarningsYield − BondYield auf x = 0..N−1 regressieren: slope = Σ((x−mean(x))×(y−mean(y))) / Σ(x−mean(x))²; Prognose = mean(y) + slope×(N−mean(x)). Positiv: Aktien; negativ: Geldmarkt-ETF oder Bargeld. Null behält das Regime. Nur bei Regimewechsel handeln, ohne monatliche Größenanpassung. Vor dem Neukauf die alte Position vollständig ausführen lassen.

Alle Beobachtungen müssen bis zum Schluss der Signalkerze veröffentlicht sein. Intraday zählen nur Tageskurse vor dem aktuellen UTC-Datum; eine abgeschlossene Tageskerze zählt ihr eigenes Datum. Zukünftige CSV-Daten dürfen keine Entscheidung vorwärmen.

Die Veröffentlichungsgrenze ist die späteste tatsächliche CloseTime der synchronisierten abgeschlossenen Kerzen. Eine Handelssitzung kann vor Ablauf des eingestellten Kerzenintervalls enden.

## Eingabedaten

YieldHistoryFile ist erforderlich: `Month,PublishedAt,EarningsYield,BondYield`. Month: `yyyy-MM`; PublishedAt: UTC `yyyy-MM-ddTHH:mm:ssZ`. Gleiche Renditeeinheiten verwenden. Nur bis zum Entscheidungsschluss veröffentlichte Revisionen verwenden. Aufeinanderfolgende Monate bis einschließlich Prüfmonat sind nötig; fehlende Werte verzögern die Prognose. Doppelte Revisionen und Veröffentlichung vor dem Beobachtungsmonat sind ungültig. Beobachtete Markt-/Fundamentaldaten statt inverser Preise verwenden.

## Parameter

| Parameter | Standard |
|---|---|
| `Universe` | erforderlich |
| `Capital` | `100000` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `RegressionMonths` | `12` |
| `YieldHistoryFile` | erforderlich |

## Ausführung

Universe ausdrücklich angeben. Preise und Capital in USD je Handelseinheit. Long-Exposure ist auf min(Capital, gemeldeter Portfolio.CurrentValue) begrenzt; ohne positiven gemeldeten Wert kein Kaufbudget. Keine Hebelung, Stop-Loss- oder Gewinnmitnahmeorders.

Zielmengen zu null auf VolumeStep runden. Vor neuen Positionen tatsächliche Vollschließung abwarten; ausgeführte Positionen abgleichen, MinVolume/MaxVolume beachten und große Anpassungen auf spätere Kerzen verteilen. MinTradeUsd filtert Anpassungen auf Nichtnullziele; vollständige Schließung umgeht den Filter.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Ausführungsdaten sowie expliziten Zins-, Finanz-, Bewertungs-, Text- und Aufwärmfixtures geprüft. Kalenderfixtures verschieben Archiv-OHLC/Volumina und setzen Ausführungskurse; dies ist keine mehrjährige Markthistorie. Unabhängige Formeln, Orderrichtung/-menge, echte Ausführungen, Restpositionen und gesperrte Termine werden geprüft. Zwei Gruppen, K=1 und AssetDecile=1 sind deklarierte Mechanikprofile, kein Nachweis für ein vollständiges Handelsuniversum. Keine Renditezusage.
