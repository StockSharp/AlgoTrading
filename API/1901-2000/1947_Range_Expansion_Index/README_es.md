# Estrategia de Índice de Expansión de Rango
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el **Índice de Expansión de Rango (REI)** de Tom DeMark para evaluar la fortaleza y debilidad del precio. El indicador compara los máximos y mínimos actuales con precios anteriores y oscila entre valores positivos y negativos.

## Cómo Funciona

- Cuando el REI sube por encima del **Nivel Inferior** (predeterminado `-60`) después de haber estado por debajo, la estrategia abre una posición **larga**.
- Cuando el REI cae por debajo del **Nivel Superior** (predeterminado `60`) después de haber estado por encima, la estrategia abre una posición **corta**.
- Las posiciones opuestas se cierran automáticamente cuando ocurre una señal opuesta.

## Parámetros

- `REI Period` – número de barras usadas en el cálculo del REI (predeterminado `8`).
- `Up Level` – umbral superior que indica debilidad del precio cuando se cruza hacia abajo (predeterminado `60`).
- `Down Level` – umbral inferior que indica fortaleza del precio cuando se cruza hacia arriba (predeterminado `-60`).
- `Candle Type` – marco temporal de velas para el cálculo del indicador (predeterminado `8 horas`).

## Uso

Adjunte la estrategia a un instrumento y ejecútela. La estrategia se suscribe a la serie de velas especificada y utiliza órdenes de mercado para entrar o salir de posiciones basándose en las señales del REI.
