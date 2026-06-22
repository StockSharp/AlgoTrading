# Nacht-Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Nacht-Stochastic-Strategie handelt nur während der ruhigen Nachtsession von **21:00** bis **06:00** Uhr. Sie verwendet die %K-Linie des Stochastic Oscillators, um überverkaufte und überkaufte Bedingungen zu erkennen.

Wenn der Oszillator unter das Überverkauft-Niveau fällt, wird eine Long-Position eröffnet. Wenn er über das Überkauft-Niveau steigt, wird eine Short-Position eröffnet. Jeder Trade ist durch feste Stop-Loss- und Take-Profit-Niveaus in Preispunkten geschützt.

## Details

- **Einstiegskriterien**:
  - **Long**: `%K < StochOversold` und Zeit zwischen 21:00 und 06:00.
  - **Short**: `%K > StochOverbought` und Zeit zwischen 21:00 und 06:00.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Position wird durch vordefinierten Stop-Loss oder Take-Profit geschlossen.
- **Stops**: Ja, verwendet festen Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `StopLossPoints` = 40
  - `TakeProfitPoints` = 20
  - `StochOversold` = 30
  - `StochOverbought` = 70
  - `CandleType` = 15-Minuten-Zeitrahmen
- **Filter**:
  - Kategorie: Indikatorbasiert
  - Richtung: Beide
  - Indikatoren: Stochastic Oscillator
  - Zeitrahmen: Kurzfristig
  - Handelsfenster: 21:00-06:00 Serverzeit
