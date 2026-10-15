# Estrategia Shuriken Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la funcionalidad de la herramienta MQL original *Shuriken Lite*. Realiza un seguimiento de las operaciones ejecutadas en la cuenta y las agrupa por identificadores numéricos conocidos como **magic numbers**. Para cada grupo la estrategia calcula:

- Número de operaciones
- Operaciones ganadoras y perdedoras
- Beneficio o pérdida total en pips
- Factor de beneficio

Las estadísticas se registran después de cada nueva operación cuando la visualización de puntuación está habilitada.

## Parámetros

- **Magic Numbers** — lista separada por comas de identificadores usados para agrupar operaciones. Cada identificador debe coincidir con el valor numérico incluido en el comentario de la orden.
- **Show Scores** — habilitar o deshabilitar el registro de estadísticas.

## Uso

1. Establezca los magic numbers deseados en el parámetro.
2. Ejecute la estrategia junto a otras estrategias que coloquen comentarios numéricos en sus órdenes.
3. Consulte el registro para ver las métricas de rendimiento agregadas.
