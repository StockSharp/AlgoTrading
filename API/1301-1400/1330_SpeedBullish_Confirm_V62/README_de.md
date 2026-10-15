# Strategie SpeedBullish Strategy Confirm V6.2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Strategie, die EMA-Trendfilter, MACD-Histogramm-Crossover und RSI-Schwellenwert kombiniert. Optionale ATR- und Volumen-Filter verbessern die Signalqualität.

## Einstiegsbedingungen
- Preis über EMA10 oder EMA15 für Longs, darunter für Shorts.
- MACD-Histogramm kreuzt über null für Longs, unter null für Shorts.
- RSI größer oder kleiner als das angegebene Niveau.
- Optional: ATR muss seine gleitende Durchschnittslinie um einen Multiplikator überschreiten.
- Optional: Volumen muss die SMA um einen Multiplikator überschreiten.

## Ausstiegsbedingungen
- Entgegengesetztes Einstiegssignal.
- Take-Profit und Trailing Stop in Punkten.
- Manueller Stop-Loss in Punkten.
