# Accrual Anomaly

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Jahresberichte nach (NetIncome − OperatingCashFlow) / TotalAssets sortieren. Das niedrigste Deciles-Segment kaufen, das höchste verkaufen. Jedes Extrem enthält floor(Anzahl / Deciles) Titel; mindestens Deciles geeignete Berichte sind nötig. Standardmonat ist Mai.

Einmal jährlich im RebalanceMonth wird auf der ersten geeigneten gemeinsamen abgeschlossenen Kerze umgeschichtet. Später veröffentlichte Berichte beeinflussen keine frühere Entscheidung. MaxReportAgeDays zählt ab PeriodEnd.

## Eingabedaten

FundamentalsFile ist erforderlich: SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding. PeriodEnd: yyyy-MM-dd; PublishedAt: yyyy-MM-ddTHH:mm:ssZ (UTC). Aktiva und Aktienzahl müssen positiv sein. Verwendet wird die neueste Periode mit ihrer bis zum Signal veröffentlichten jüngsten Revision. Doppelte Revisionen oder Veröffentlichungen vor Periodenende sind ungültig. Keine Finanzkennzahl wird aus Kerzen erzeugt.

## Parameter

| Parameter | Standardwert |
|---|---|
| `Universe` | erforderlich |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `50` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `FundamentalsFile` | erforderlich |
| `RebalanceMonth` | `5` |
| `MaxReportAgeDays` | `550` |

## Ausführung

Universe muss explizit angegeben werden. Preise und Capital sind in USD je gehandelter Einheit angegeben. Capital ist ein festes Budget; Leverage bestimmt das Bruttoengagement. Es werden keine Stop-Loss- oder Take-Profit-Orders hinzugefügt.

Jede Seite erhält die Hälfte von Capital × Leverage, gleichmäßig auf ihre Titel verteilt. Stückzahlen basieren auf dem Signalschlusskurs und werden zu null auf VolumeStep gerundet. Tatsächliche Ausführungen bestimmen den Bestand. Offene Orders werden abgewartet, MinVolume/MaxVolume eingehalten und große Anpassungen auf spätere Kerzen verteilt. MinTradeUsd gilt nicht für vollständige Glattstellungen.

## Prüfung

Ausführungen werden ausschließlich mit dem vorhandenen BTC/TON-Archiv geprüft. Explizite Berichts-, Options- und Vorwärmdaten sind mechanische Testdaten, keine echten Unternehmens- oder Optionsdaten. Kürzere Fenster, zwei Gruppen und März als jährlicher Termin sind benannte Testprofile, kein Nachweis für unveränderte langfristige Standardwerte. Es wird keine Rendite behauptet.
