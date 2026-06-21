# Tiger EMA ADX RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie folgt dem Trend mithilfe einer Kreuzung zweier exponentieller gleitender Durchschnitte (EMA) und filtert Trades mit dem Average Directional Index (ADX) und dem Relative Strength Index (RSI). Der schnelle EMA wird mit dem langsamen EMA verglichen, um die Trendrichtung zu bestimmen. Trades sind nur erlaubt, wenn ADX einen konfigurierbaren Schwellenwert überschreitet und RSI innerhalb der oberen und unteren Grenzen bleibt.

Wenn keine Position offen ist und alle Bedingungen erfüllt sind, tritt die Strategie in Trendrichtung ein. Jeder Einstieg setzt feste Take-Profit- und Stop-Loss-Abstände vom Einstiegspreis. Die Position wird geschlossen, wenn eines der Level erreicht wird. Das Ordervolumen wird durch die `Volume`-Eigenschaft der Strategie definiert.

## Parameter

- **Fast EMA** – Periode des schnellen exponentiellen gleitenden Durchschnitts.
- **Slow EMA** – Periode des langsamen exponentiellen gleitenden Durchschnitts.
- **ADX Period** – Periode der ADX-Berechnung.
- **ADX Threshold** – Mindest-ADX-Wert, der für den Handel erforderlich ist.
- **RSI Period** – Periode der RSI-Berechnung.
- **RSI Upper** – Maximaler RSI-Wert für Long-Einstiege.
- **RSI Lower** – Minimaler RSI-Wert für Short-Einstiege.
- **Take Profit** – Abstand vom Einstiegspreis zum Take-Profit in Preispunkten.
- **Stop Loss** – Abstand vom Einstiegspreis zum Stop-Loss in Preispunkten.
- **Candle Type** – Zeitrahmen oder anderer Kerzentyp für Indikatorberechnungen.
