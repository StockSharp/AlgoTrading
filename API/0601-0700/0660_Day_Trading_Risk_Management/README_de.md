# Day-Trading-Strategie mit Risikomanagement
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht long, wenn die schnelle EMA die langsame EMA von unten kreuzt, während der RSI unterhalb des Überkauft-Niveaus liegt. Sie geht short, wenn die schnelle EMA die langsame EMA von oben kreuzt, während der RSI oberhalb des Überverkauft-Niveaus liegt. Das Risikomanagement verwendet feste Take-Profit- und Stop-Loss-Prozentwerte.

### Exakte Einstieg- und Schutzregeln

Standard: `CandleType=1 Minute`, `FastEmaPeriod=20`, `SlowEmaPeriod=50`, `RsiLength=14`, `RsiOverbought=70`, `RsiOversold=30`, `TakeProfitPercent=2`, `StopLossPercent=1`. Auf **abgeschlossenen Kerzen** Long, falls vorher `EMA20 <= EMA50`, jetzt `EMA20 > EMA50` und `RSI14 < 70`; Short, falls vorher `EMA20 >= EMA50`, jetzt `EMA20 < EMA50` und `RSI14 > 30`. Aus Flat oder zur Umkehr einer bestehenden Gegenposition wird `Volume + abs(Position)` gehandelt. **Ohne RSI-Bestätigung kein Einstieg**. Ausgeführte Positionen haben **nativen festen 2%-Take-Profit und 1%-Stop-Loss**, ohne Trailing, zwischen Kerzenabschlüssen aktiv. Zuvor verwendete die Umsetzung nur EMA120/450 ohne RSI oder Schutz. Die Zahlenwerte waren im ursprünglichen README nicht angegeben und sind hier explizit festgelegt.



## Parameter
- Kerzentyp
- Schnelle EMA-Länge
- Langsame EMA-Länge
- RSI-Länge
- RSI Überkauft
- RSI Überverkauft
- Take-Profit-Prozent
- Stop-Loss-Prozent
