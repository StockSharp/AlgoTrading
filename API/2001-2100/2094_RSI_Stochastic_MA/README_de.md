# RSI Stochastic MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen einfachen gleitenden Durchschnitt (SMA) als Trendfilter mit RSI- und Stochastic-Oszillatoren.
Der gleitende Durchschnitt definiert die Marktrichtung. Wenn der Kurs über dem SMA liegt, sucht die Strategie Long-Einstiege;
liegt er darunter, werden Short-Einstiege gesucht. RSI- und Stochastic-Niveaus identifizieren überkaufte oder
überverkaufte Bedingungen für das Timing der Einstiege.

Positionen werden geschlossen, wenn die Oszillatoren ihre Extremzonen verlassen. Dadurch bleiben Geschäfte mit
dem vorherrschenden Trend ausgerichtet und vermeiden verlängerte Bewegungen gegen die Indikatoren.

## Parameter
- `RsiPeriod` – RSI-Berechnungsperiode.
- `RsiUpperLevel` – RSI-Überkauft-Schwelle.
- `RsiLowerLevel` – RSI-Überverkauft-Schwelle.
- `MaPeriod` – Periode des Trend-Gleitenden Durchschnitts.
- `StochKPeriod` – %K-Periode des Stochastic-Oszillators.
- `StochDPeriod` – %D-Glättungsperiode des Stochastic-Oszillators.
- `StochUpperLevel` – Stochastic-Überkauft-Niveau.
- `StochLowerLevel` – Stochastic-Überverkauft-Niveau.
- `Volume` – Auftragsvolumen.
- `CandleType` – Kerzen-Datentyp für Berechnungen.

## Indikatoren
- Einfacher Gleitender Durchschnitt
- Relative Stärke Index
- Stochastic-Oszillator

## Handelsregeln
- **Kaufen** wenn der Kurs über dem SMA liegt, RSI unter `RsiLowerLevel` ist und beide Stochastic-Linien unter `StochLowerLevel` sind.
- **Verkaufen** wenn der Kurs unter dem SMA liegt, RSI über `RsiUpperLevel` ist und beide Stochastic-Linien über `StochUpperLevel` sind.
- **Long schließen** wenn RSI oder Stochastic über ihre oberen Niveaus steigt.
- **Short schließen** wenn RSI oder Stochastic unter ihre unteren Niveaus fällt.
