# Earnings Quality Factor

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Gewinnqualität = (OperatingCashFlow − NetIncome) / TotalAssets. An der ersten geeigneten synchronen Kerze im RebalanceMonth den jüngsten Geschäftsjahresbericht und die bis zum Kerzenschluss veröffentlichte letzte Revision wählen. Das höchste Deciles-Segment kaufen und das niedrigste verkaufen, jeweils floor(Anzahl geeigneter Titel / Deciles). Einmal jährlich umschichten und Bestände bis zur nächsten Prüfung halten. Das Alter seit PeriodEnd darf MaxReportAgeDays nicht überschreiten.

Einmal jährlich im RebalanceMonth wird auf der ersten geeigneten gemeinsamen abgeschlossenen Kerze umgeschichtet. Später veröffentlichte Berichte beeinflussen keine frühere Entscheidung. MaxReportAgeDays zählt ab PeriodEnd.

Alle Beobachtungen müssen bis zum Schluss der Signalkerze veröffentlicht sein. Intraday zählen nur Tageskurse vor dem aktuellen UTC-Datum; eine abgeschlossene Tageskerze zählt ihr eigenes Datum. Zukünftige CSV-Daten dürfen keine Entscheidung vorwärmen.

Die Veröffentlichungsgrenze ist die späteste tatsächliche CloseTime der synchronisierten abgeschlossenen Kerzen. Eine Handelssitzung kann vor Ablauf des eingestellten Kerzenintervalls enden.

## Eingabedaten

FundamentalsFile ist erforderlich: SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding. PeriodEnd: yyyy-MM-dd; PublishedAt: yyyy-MM-ddTHH:mm:ssZ (UTC). Aktiva und Aktienzahl müssen positiv sein. Verwendet wird die neueste Periode mit ihrer bis zum Signal veröffentlichten jüngsten Revision. Doppelte Revisionen oder Veröffentlichungen vor Periodenende sind ungültig. Keine Finanzkennzahl wird aus Kerzen erzeugt.

## Parameter

| Parameter | Standard |
|---|---|
| `Universe` | erforderlich |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `RebalanceMonth` | `7` |
| `MaxReportAgeDays` | `550` |
| `FundamentalsFile` | erforderlich |

## Ausführung

Universe muss explizit angegeben werden. Preise und Capital sind in USD je gehandelter Einheit angegeben. Capital ist ein festes Budget; Leverage bestimmt das Bruttoengagement. Es werden keine Stop-Loss- oder Take-Profit-Orders hinzugefügt.

Jede Seite erhält die Hälfte von Capital × Leverage, gleichmäßig auf ihre Titel verteilt. Stückzahlen basieren auf dem Signalschlusskurs und werden zu null auf VolumeStep gerundet. Tatsächliche Ausführungen bestimmen den Bestand. Offene Orders werden abgewartet, MinVolume/MaxVolume eingehalten und große Anpassungen auf spätere Kerzen verteilt. MinTradeUsd gilt nicht für vollständige Glattstellungen.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Ausführungsdaten sowie expliziten Zins-, Finanz-, Bewertungs-, Text- und Aufwärmfixtures geprüft. Kalenderfixtures verschieben Archiv-OHLC/Volumina und setzen Ausführungskurse; dies ist keine mehrjährige Markthistorie. Unabhängige Formeln, Orderrichtung/-menge, echte Ausführungen, Restpositionen und gesperrte Termine werden geprüft. Zwei Gruppen, K=1 und AssetDecile=1 sind deklarierte Mechanikprofile, kein Nachweis für ein vollständiges Handelsuniversum. Keine Renditezusage.
