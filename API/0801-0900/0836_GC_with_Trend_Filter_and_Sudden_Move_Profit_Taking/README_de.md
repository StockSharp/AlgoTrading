# GC-Strategie mit Trendfilter und Gewinnmitnahme bei Plötzlichen Bewegungen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen 5/25-SMA-Crossover mit einem 75-Perioden-Trendfilter und einer ADX-Bestätigung. Positionen werden geschlossen, wenn der Kurs mehr als einen bestimmten Prozentsatz vom vorherigen Schlusskurs abweicht, um plötzliche Bewegungen zu erfassen.

## Details
- **Einstieg**: Long, wenn SMA 5 über SMA 25 kreuzt, Kurs über SMA 75 und ADX über dem Schwellenwert. Short bei umgekehrten Bedingungen.
- **Ausstieg**: Entgegengesetztes Signal oder plötzliche Bewegung, die den konfigurierten Prozentsatz überschreitet.
- **Indikatoren**: SMA, Average Directional Index.
- **Märkte**: Beliebig.
