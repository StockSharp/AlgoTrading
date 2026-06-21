# EMA-Crossover-Strategie mit Filtern
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet mehrere Exponentiell Gleitende Durchschnitte (EMAs), um Kreuzungen mit zusätzlichen Trendfiltern zu handeln.

Die Strategie kauft, wenn die 100-EMA die 200-EMA nach oben kreuzt, während die 9-EMA über der 50-EMA liegt. Sie verkauft short, wenn die 100-EMA die 200-EMA nach unten kreuzt und die 9-EMA unter der 50-EMA liegt. Long-Positionen werden geschlossen, wenn die 100-EMA die 50-EMA nach unten kreuzt; Short-Positionen werden geschlossen, wenn die 100-EMA die 50-EMA nach oben kreuzt.

## Parameter
- Kerzentyp
- EMA-9-Länge
- EMA-50-Länge
- EMA-100-Länge
- EMA-200-Länge
