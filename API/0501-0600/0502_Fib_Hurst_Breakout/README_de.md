# Fib Hurst Breakout

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

CandleType ist intraday. Fibonacci-Spanne aus höchstem Hoch und tiefstem Tief der vorherigen LookbackPeriod abgeschlossenen UTC-Tageskerzen, ohne Signaltag. Niveaus: L+0.618×(H−L) und L+0.382×(H−L). Tages-Hurst aus vorherigen HurstPeriod Schlusskursen: H = log(R/S)/log(N); R ist die Spanne kumulierter Abweichungen vom Mittel, S die Populationsstandardabweichung. Nullvarianz sperrt Einstieg. Vorigen und aktuellen Intraday-Schluss mit demselben aktuellen Tagesniveau vergleichen: über 61.8% kreuzen bei H>0.5 kauft, unter 38.2% bei H<0.5 eröffnet Short. Nur ohne Position Volume einsteigen. MaxTradesPerDay zählt Einstiegsorder-Versuche nach historischem UTC-Tag, MaxTotalTrades pro Lauf. Nativer Quoteschutz fixiert SL = RiskPercent% und TP = RiskPercent×RiskReward% ab Ausführung. Keine Gegensignal-Umkehr oder Wartebalken.

## Parameter

| Parameter | Standard |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(15).TimeFrame() |
| `HurstPeriod` | 50 |
| `LookbackPeriod` | 20 |
| `MaxTradesPerDay` | 5 |
| `MaxTotalTrades` | 510 |
| `RiskPercent` | 2 |
| `RiskReward` | 2 |
| `DailyHistoryFile` | nicht gesetzt |

## Eingabedaten

Optionale `DailyHistoryFile`-CSV: `Date,High,Low,Close`, Datum `yyyy-MM-dd`. Eindeutige Daten und 0 < Low ≤ Close ≤ High. Nur Tage vor dem Intraday-Signaltag verwenden. Abgeschlossene Tageskerzen ersetzen den eigenen Tag; zukünftige Zeilen beeinflussen keine frühere Prüfung.

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Daten, unabhängigen Formeln, Orderseiten/-mengen, Ausführungen, Positionen und Ausstiegen geprüft. Datierte Preis-/Faktor- und Kalenderfixtures prüfen Mechanik; verschobene OHLC/Volumina sind keine mehrjährige Marktgeschichte. Zwei Ranggruppen, TopN=1 und SMA=2 sind ausdrücklich kleine Testprofile. Lange Originalfenster und Standardwerte werden separat geprüft. Keine Renditezusage.
