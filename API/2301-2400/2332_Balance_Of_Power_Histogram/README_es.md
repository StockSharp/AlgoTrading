# Estrategia de Histograma de Balance Of Power
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación del experto original de MetaTrader de `MQL/16214`. Utiliza el indicador **Balance of Power** (BOP) para detectar cambios de momentum en el mercado.

## Lógica

1. La estrategia calcula el Balance of Power para cada vela completada:
   
   $$BOP = \frac{Close - Open}{High - Low}$$
2. Se comparan tres valores consecutivos de BOP.
   - Cuando el valor anterior es inferior al anterior a él y el valor actual es superior al anterior, el BOP gira hacia arriba y la estrategia entra en una posición larga.
   - Cuando el valor anterior es superior al anterior a él y el valor actual es inferior al anterior, el BOP gira hacia abajo y la estrategia entra en una posición corta.
3. La posición se cambia solo después de una vela completada para evitar señales falsas.

## Parámetros

- **CandleType** – marco temporal de las velas utilizadas para los cálculos. El predeterminado son velas de cuatro horas.

## Notas

Este port se centra en el comportamiento central de la estrategia original y no implementa las opciones avanzadas de gestión de capital de la versión MQL.
