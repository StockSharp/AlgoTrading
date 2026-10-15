# Einfache Multi-Zeitrahmen-Moving-Average-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Logik von `simple_multiple_time_frame_moving_average.mq4`. Sie gleicht Trends über zwei Zeitrahmen mithilfe einfacher gleitender Durchschnitte ab.

## Strategielogik
- Berechnet SMA mit Periode `Length` auf 1-Stunden- und 4-Stunden-Kerzen.
- Geht Long, wenn beide SMAs steigen.
- Geht Short, wenn beide SMAs fallen.
- Schließt eine Long-Position, wenn einer der SMAs nach unten dreht.
- Schließt eine Short-Position, wenn einer der SMAs nach oben dreht.
- Es kann jeweils nur eine Position aktiv sein.

## Parameter
- **MA Length** (`Length`): Periode für beide gleitenden Durchschnitte.
- **Short Time Frame** (`ShortCandleType`): Zeitrahmen für den ersten SMA (Standard: 1 Stunde).
- **Long Time Frame** (`LongCandleType`): Zeitrahmen für den zweiten SMA (Standard: 4 Stunden).

Das Handelsvolumen wird aus der `Volume`-Eigenschaft der Strategie entnommen.

## Hinweise
Diese Implementierung konzentriert sich auf die stündlichen und vierstündigen Durchschnitte der ursprünglichen MQL-Version und lässt ungenutzte Berechnungen höherer Zeitrahmen weg.
