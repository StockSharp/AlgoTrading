# Lego 4 Beta Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein modulares System, das aus dem MetaTrader-Skript "exp_Lego_4_Beta" übersetzt wurde. Es kombiniert mehrere gängige technische Indikatoren und ermöglicht das Aktivieren oder Deaktivieren jeder Komponente über Parameter.

## Algorithmus

1. **Moving-Average-Kreuzung** – Es werden ein schneller und ein langsamer gleitender Durchschnitt berechnet. Eine Long-Position wird eröffnet, wenn der schnelle Durchschnitt den langsamen von unten kreuzt. Bei entgegengesetzter Kreuzung wird eine Short-Position eröffnet.
2. **Stochastik-Oszillator-Filter** – Wenn aktiviert, erfordern Long-Einstiege, dass der Stochastik-%K-Wert unter dem Überverkaufsniveau liegt, und Short-Einstiege erfordern, dass %K über dem Überkaufniveau liegt.
3. **RSI-Ausstieg** – Wenn aktiviert, werden bestehende Long-Positionen geschlossen, wenn RSI über den hohen Schwellenwert steigt. Short-Positionen werden geschlossen, wenn RSI unter den niedrigen Schwellenwert fällt.

## Parameter

- `UseMaOpen` – Moving-Average-Kreuzungssignale aktivieren.
- `FastMaLength` / `SlowMaLength` – Längen der schnellen und langsamen gleitenden Durchschnitte.
- `MaType` – Typ des gleitenden Durchschnitts (SMA, EMA, WMA).
- `UseStochasticOpen` – Stochastik-Filter für Einstiege aktivieren.
- `StochLength` – Hauptperiode für die Stochastik-Berechnung.
- `StochKPeriod` / `StochDPeriod` – Glättungsperioden für die %K- und %D-Linien.
- `StochBuyLevel` / `StochSellLevel` – Überverkaufs- und Überkaufsschwellen.
- `UseRsiClose` – RSI-basierte Ausstiege aktivieren.
- `RsiPeriod` – RSI-Berechnungslänge.
- `RsiHigh` / `RsiLow` – RSI-Schwellenwerte zum Schließen von Positionen.
- `CandleType` – Kerzentyp für das Abonnement.

## Hinweise

Die Strategie verwendet das High-Level-`SubscribeCandles` mit `BindEx` zur Verarbeitung von Indikatorwerten und folgt dem empfohlenen StockSharp-Stil. Für Ein- und Ausstiege werden ausschließlich Marktorders verwendet.
