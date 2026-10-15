# BlauTvi-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den MQL5-Expert `Exp_BlauTVI` in eine StockSharp-High-Level-Strategie. Sie verwendet den **Blau True Volume Index (TVI)**, um Umkehrungen im Tick-Volumen-Fluss zu erkennen.

## Idee

Der True Volume Index trennt Up-Ticks und Down-Ticks und glättet sie mit drei exponentiellen gleitenden Durchschnitten. Der Endwert oszilliert zwischen -100 und +100 und repräsentiert die Dominanz von Käufern oder Verkäufern. Die Strategie eröffnet eine Long-Position, wenn der Indikator nach einem Rückgang nach oben dreht, und eine Short-Position, wenn der Indikator nach einem Anstieg nach unten dreht. Bestehende Positionen in der entgegengesetzten Richtung werden geschlossen.

## Parameter

- `Length1` – erste Glättungsperiode für Up- und Down-Ticks.
- `Length2` – zweite Glättungsperiode.
- `Length3` – abschließende auf den TVI angewendete Glättungsperiode.
- `CandleType` – für Berechnungen verwendeter Kerzentyp (Standard: 4-Stunden-Zeitrahmen).
- `Allow Buy Open` – Öffnen von Long-Positionen aktivieren.
- `Allow Sell Open` – Öffnen von Short-Positionen aktivieren.
- `Allow Buy Close` – Schließen von Long-Positionen bei Verkaufssignal aktivieren.
- `Allow Sell Close` – Schließen von Short-Positionen bei Kaufsignal aktivieren.
- `Enable Stop Loss` – Stop-Loss-Schutz in Punkten verwenden.
- `Stop Loss` – Stop-Loss-Wert in Punkten.
- `Enable Take Profit` – Take-Profit-Schutz in Punkten verwenden.
- `Take Profit` – Take-Profit-Wert in Punkten.
- `Volume` – Ordervolumen in Lots.

## Signale

1. **Kauf** – wenn der vorherige TVI-Wert niedriger als der davor ist und der aktuelle TVI-Wert größer als der vorherige ist. Falls aktiviert, werden bestehende Short-Positionen geschlossen.
2. **Verkauf** – wenn der vorherige TVI-Wert höher als der davor ist und der aktuelle TVI-Wert kleiner als der vorherige ist. Falls aktiviert, werden bestehende Long-Positionen geschlossen.

Es werden nur abgeschlossene Kerzen verarbeitet und alle Berechnungen verwenden das Tick-Volumen der Kerze. Stop-Loss und Take-Profit sind optional und werden in Preispunkten ausgedrückt.

## Hinweise

Die Strategie verwendet die High-Level-API: Sie abonniert Kerzen, berechnet den Indikator intern mit `ExponentialMovingAverage`-Instanzen und verwaltet Positionen mit den Methoden `BuyMarket` und `SellMarket`. Das Diagramm zeigt den TVI-Indikator zusammen mit den von der Strategie ausgeführten Trades.
