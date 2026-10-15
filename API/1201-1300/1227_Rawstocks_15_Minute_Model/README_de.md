# Rawstocks 15-Minuten-Modell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Rawstocks 15 Minute Model verwendet Swing-Order-Blöcke und Fibonacci-Retracement-Level, um innerhalb einer Tagessitzung zu handeln.

## So funktioniert es
- Erkennt Swing-Hochs und -Tiefs mit einem ATR-Filter.
- Erstellt bullische und bärische Order-Blöcke und berechnet 61,8%- und 79%-Fibonacci-Level.
- Einstieg in Long, wenn der Preis einen bullischen Order-Block berührt und vor dem Einstiegs-Cutoff über einem Fibonacci-Level schließt.
- Einstieg in Short, wenn der Preis einen bärischen Order-Block testet und unter einem Fibonacci-Level schließt.
- Schließt alle Positionen um 16:30 Uhr ET.

## Parameter
- Start Hour
- Start Minute
- Last Entry Hour
- Last Entry Minute
- Force Close Hour
- Force Close Minute
- Fib Level (%)
- Min Swing Size (%)
- Risk/Reward

### Indikatoren
- Average True Range
