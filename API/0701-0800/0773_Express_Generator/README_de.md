# Express Generator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt einen gleitenden Durchschnitt Crossover, bestätigt durch RSI- und MACD-Signale. Die Positionsgröße verwendet einen ATR-basierten Volatilitätsfaktor und einen festen Risikoprozentsatz. Ein Trailing-Stop in Pips verwaltet die Ausstiege.

## Details

- **Einstieg Long**: Schnelle SMA kreuzt über die langsame SMA, RSI unter Überkauft, MACD-Linie kreuzt über das Signal.
- **Einstieg Short**: Schnelle SMA kreuzt unter die langsame SMA, RSI über Überverkauft, MACD-Linie kreuzt unter das Signal.
- **Ausstieg**: Trailing-Stop in Pips.
- **Positionsgröße**: Risiko-% des Eigenkapitals dividiert durch den Stopabstand, angepasst durch ATR.
- **Indikatoren**: SMA, RSI, MACD, ATR.
- **Richtung**: Beide Richtungen.
