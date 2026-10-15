# Lego V3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Portierung des MQL4-Expert-Advisors „Lego_v3".  
Sie kombiniert mehrere klassische Indikatoren zur Erzeugung von Ein- und Ausstiegssignalen:

- **Gleitende Durchschnitte** – schneller und langsamer SMA zur Erkennung der Trendrichtung.
- **Stochastic Oszillator** – %K- und %D-Werte definieren überverkaufte und überkaufte Zonen.
- **Awesome Oscillator** – bestätigt die Momentum-Ausrichtung mit dem Trend.
- **Average True Range** – bestimmt die Stop-Loss- und Take-Profit-Abstände.

Eine Long-Position wird eröffnet, wenn der schnelle MA über dem langsamen MA kreuzt, der Stochastic %K unter dem Kaufniveau liegt und der Awesome Oscillator positiv ist.  
Short-Positionen entstehen unter umgekehrten Bedingungen. Der ATR wird einmalig am Anfang zur Verwaltung des Schutz-Stops verwendet.

## Parameter

- `FastMaPeriod` – Periode für den schnellen gleitenden Durchschnitt.
- `SlowMaPeriod` – Periode für den langsamen gleitenden Durchschnitt.
- `StochK` – %K-Periode für den Stochastic-Oszillator.
- `StochD` – %D-Periode für den Stochastic-Oszillator.
- `StochBuy` – Kaufzonen-Schwellenwert für %K.
- `StochSell` – Verkaufszonen-Schwellenwert für %K.
- `AtrPeriod` – Periode für die ATR-Berechnung.
- `AtrMultiplier` – Multiplikator, der auf den ATR für Stop-Niveaus angewendet wird.
- `CandleType` – Zeitrahmen der verarbeiteten Kerzen.
