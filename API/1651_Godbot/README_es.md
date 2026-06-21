# Estrategia Godbot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera usando Bandas de Bollinger combinadas con medias móviles para detectar reversiones y fuerza de tendencia.

## Lógica
- Funciona en un marco temporal de velas principal (predeterminado 30 minutos).
- Calcula las Bandas de Bollinger y una EMA en este marco temporal.
- Por separado calcula una DEMA en un marco temporal superior (predeterminado 1 día) para determinar la tendencia global.
- Cierra posiciones largas cuando el precio vuelve a caer por debajo de la banda superior de Bollinger.
- Cierra posiciones cortas cuando el precio sube de nuevo por encima de la banda inferior de Bollinger.
- Abre largo cuando el precio cruza por encima de la banda inferior mientras tanto DEMA como EMA están subiendo.
- Abre corto cuando el precio cruza por debajo de la banda superior mientras tanto DEMA como EMA están cayendo.

## Parámetros
- **Bollinger Period** – período de las Bandas de Bollinger.
- **Bollinger Deviation** – multiplicador de anchura para las bandas.
- **EMA Period** – período para el filtro de tendencia EMA.
- **DEMA Period** – período para la DEMA en el marco temporal superior.
- **Candle Type** – marco temporal usado para los cálculos de Bandas de Bollinger y EMA.
- **DEMA Candle Type** – marco temporal superior usado para la DEMA.

## Notas
- Solo se mantiene una posición a la vez.
- La estrategia utiliza órdenes de mercado para entradas y salidas.
- Los datos de DEMA deben acumularse antes de que comience a operar.
