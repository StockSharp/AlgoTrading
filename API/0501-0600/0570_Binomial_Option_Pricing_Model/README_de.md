# Binomial Option Pricing Model

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Bewertungsregeln

Nicht handelndes Bewertungsmodul für Optionen mit einem zweistufigen Cox–Ross–Rubinstein-Baum; es sendet keine Orders. `CalculatePrice(spot, volatility, timeToExpiry)` bewertet den konfigurierten Strike, Call/Put, europäischen/amerikanischen Stil und die Anlageklasse. Zinsen und Volatilität sind jährliche Bruchteile: `0.05` bedeutet 5%. Ein Jahr hat 365 Tage; Minuten und Stunden sind zulässig.

Für `dt = T / 2` gilt `u = exp(volatility * sqrt(dt))`, `d = 1/u`, `p = (exp(b * dt) - d) / (u - d)`. Für `Stock`, `Index`, `Currency` ist `b = r - q` (q: Dividendenrendite/Fremdzins), für `Future` ist `b = 0`. Jeder Rückwärtsschritt diskontiert mit `exp(-r * dt)`. Europäische Ausübung erfolgt am Ende; amerikanische vergleicht Fortführung und inneren Wert an jedem Knoten. Restlaufzeit null ergibt inneren Wert; Volatilität null folgt dem deterministischen Carry-Pfad mit denselben Ausübungsregeln. Nicht unterstützte Klassen/Stile und ungültige Wahrscheinlichkeiten werden ohne Begrenzung abgelehnt.

`EvaluatePrice(candle)` verarbeitet jede abgeschlossene Kerze einmal. Nach `Length` Schlusskursen lautet die Jahresschätzung `Populations-SD(close) / mean(close) * sqrt(365 Tage / Intervall)`. Sie bewertet `TimeToExpiry`, aktualisiert `LastOptionPrice`, protokolliert den Wert und zeichnet gegebenenfalls Kerzen/Abweichung. Dies ist Preisniveau-Streuung, keine Renditevolatilität oder Handelssignal.

## Parameter

| Parameter | Standardwert |
|---|---|
| `StrikePrice` | `100` |
| `DividendYield` | `0` |
| `AssetClass` | `Stock` |
| `OptionStyle` | `European` |
| `OptionType` | `Call` |
| `TimeToExpiry` | `30d` |
| `Length` | `20` |
| `CandleType` | `1m` |
| `RiskFreeRate` | `0.05` |
