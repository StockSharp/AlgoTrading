# JS Signal Baes-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein StockSharp-Port des MetaTrader Expert Advisors „JS Signal Baes". Sie bewertet gleichzeitig sechs verschiedene Zeitrahmen (standardmäßig M1, M5, M15, M30, H1, H4) und wartet, bis alle überwachten Indikatoren in derselben Marktrichtung übereinstimmen, bevor eine Position eröffnet wird. Signale können über den **Reverse**-Parameter invertiert werden für Benutzer, die entgegen dem erkannten Trend handeln möchten.

## Indikatoren und Bestätigungen
Folgende Indikatoren werden auf jedem der sechs Zeitrahmen berechnet:

- **Zwei Moving Averages** mit der ausgewählten Glättungsmethode (einfach, exponentiell, geglättet oder linear gewichtet).
- **MACD (Moving Average Convergence Divergence)** mit konfigurierbaren schnellen, langsamen und Signal-Längen.
- **RSI (Relative Strength Index)** mit einem dedizierten Periodenparameter.
- **CCI (Commodity Channel Index)** mit eigener Lookback-Länge.
- **Stochastic Oszillator** definiert durch K-, D- und Glättungsperioden.

Ein Zeitrahmen gilt als **bullisch**, wenn:

1. Schnelle MA > Langsame MA.
2. MACD-Hauptlinie > MACD-Signallinie.
3. RSI > 50.
4. CCI > 0.
5. Stochastik %K > 40.

Ein Zeitrahmen gilt als **bärisch**, wenn:

1. Schnelle MA < Langsame MA.
2. MACD-Hauptlinie < MACD-Signallinie.
3. RSI < 50.
4. CCI < 0.
5. Stochastik %K < 60.

## Handelsregeln
Eine neue netto-Position wird nur eröffnet, wenn der primäre Zeitrahmen (Standard M1) schließt und **alle sechs Zeitrahmen** gleichzeitig bullisch oder bärisch sind:

- **Long-Einstieg:** jeder Zeitrahmen ist bullisch. Wenn *Reverse* aktiviert ist, wird das Signal zu einem Short-Einstieg.
- **Short-Einstieg:** jeder Zeitrahmen ist bärisch. Wenn *Reverse* aktiviert ist, wird das Signal zu einem Long-Einstieg.

Positionen werden nicht pyramidisiert. Die Strategie wartet, bis die bestehende Position extern geschlossen wird, bevor sie auf ein neues Signal reagiert. Es gibt keine automatischen Ausstiege über die entgegengesetzte Signallogik des Original Expert Advisors hinaus.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| CciPeriod | 13 | Lookback-Länge für den Commodity Channel Index. |
| FastMaPeriod | 5 | Länge des schnellen Moving Average. |
| SlowMaPeriod | 9 | Länge des langsamen Moving Average. |
| MaMethod | LinearWeighted | Moving-Average-Glättungstyp für beide Durchschnitte. |
| MacdFastPeriod | 8 | Schnelle EMA-Länge für MACD. |
| MacdSlowPeriod | 17 | Langsame EMA-Länge für MACD. |
| MacdSignalPeriod | 9 | Signallinienlänge für MACD. |
| StochasticKPeriod | 5 | K-Periode für den stochastischen Oszillator. |
| StochasticDPeriod | 3 | D-Periode für den stochastischen Oszillator. |
| StochasticSmoothing | 3 | Glättungsfaktor für den stochastischen Oszillator. |
| RsiPeriod | 9 | RSI-Lookback-Länge. |
| ReverseSignals | false | Richtung jedes Handelssignals invertieren. |
| TimeFrame1..6 | M1, M5, M15, M30, H1, H4 | Kerzenserien für jeden Zeitrahmen. |

## Hinweise
- Die Standardparameter replizieren die in der MetaTrader-Version eingebettete Konfiguration.
- Geldverwaltung, Stop-Loss, Take-Profit und Trailing-Logik aus dem Originalcode werden nicht reproduziert; Portfolio-Level-Risikokontrollen verwenden falls erforderlich.
- Stellen Sie sicher, dass historische Daten für jeden ausgewählten Zeitrahmen verfügbar sind, damit die Indikatoren sich aufwärmen können, bevor gehandelt wird.
