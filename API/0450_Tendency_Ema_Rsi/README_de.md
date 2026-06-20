# Tendency EMA + RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie legt einen schnellen/mittleren EMA-Crossover über einen langsameren
Trend-EMA und einen RSI-Filter. Long-Trades erfordern, dass die schnelle EMA die
mittlere EMA von unten kreuzt, während beide über der langsamen Trendlinie bleiben und
die Kerze bullisch schließt. Short-Trades spiegeln diese Regeln. RSI-Extreme schließen
Positionen, und eine optionale "Schließen nach X Bars"-Funktion sichert Gewinne, wenn
der Kurs sich schnell in die erwartete Richtung bewegt.

Der Ansatz zielt darauf ab, nur an Pullback-Einstiegen teilzunehmen, die mit dem
vorherrschenden Trend übereinstimmen, und nutzt den RSI zum Ausstieg, wenn das Momentum
überdehnt wird. Er funktioniert am besten auf Intraday-Charts, wo EMA-Crossover zeitnahe
Signale liefern und mehrere Setups pro Sitzung auftreten.

## Details

- **Einstiegskriterien**:
  - Schnelle EMA kreuzt mittlere EMA von unten, beide über langsamer EMA, bullische Kerze.
  - Schnelle EMA kreuzt mittlere EMA von oben, beide unter langsamer EMA, bearische Kerze.
- **Long/Short**: Long aktiviert, Short optional.
- **Ausstiegskriterien**:
  - RSI > 70 schließt Long; RSI < 30 schließt Short.
  - Optional: Schließen nach X Bars, wenn Trade profitabel ist.
- **Stops**: Keine integriert.
- **Standardwerte**:
  - RSI-Länge = 14.
  - EMA A/B/C-Längen = 9/21/50.
  - Schließen nach X Bars = aus, X = 5.
- **Filter**:
  - Kategorie: Trend + Momentum
  - Richtung: Beide (Long Standard)
  - Indikatoren: EMA, RSI
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Kurz
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
