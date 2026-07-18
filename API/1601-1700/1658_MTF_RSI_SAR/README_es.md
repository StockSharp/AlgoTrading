# Estrategia MTF RSI SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina lecturas del **Índice de Fuerza Relativa (RSI)** en cuatro marcos temporales, **Parabolic SAR** y **Bandas de Bollinger** para capturar la continuación de tendencias después de breves retrocesos. Las señales se generan en velas de 5 minutos mientras los marcos temporales superiores actúan como filtros de confirmación.

## Concepto

1. **Filtro RSI** – Los valores de RSI en 5, 15, 30 y 60 minutos deben estar todos por encima de 50 para entradas largas o por debajo de 50 para entradas cortas. Esta confirmación multi-temporalidad busca alinear las operaciones con la tendencia más amplia.
2. **Filtro Parabolic SAR** – Los valores del Parabolic SAR en los gráficos de 5, 15 y 30 minutos deben estar por debajo de la vela actual para largos o por encima para cortos. Esto asegura que el precio esté tendiendo en la dirección deseada.
3. **Disparador de Bandas de Bollinger** – En el gráfico de 5 minutos el cierre de la vela debe romper la banda superior para largos o la banda inferior para cortos. Las Bandas de Bollinger proporcionan un disparador de sobrecompra/sobreventa.
4. **Entrada y salida** – Se abre una posición larga cuando todos los filtros activos apuntan hacia arriba. Se abre una posición corta cuando todos los filtros activos apuntan hacia abajo. La señal opuesta cierra una posición abierta.

Cualquiera de los tres filtros puede desactivarse individualmente mediante parámetros, permitiendo que la estrategia opere solo con RSI, solo con Bandas de Bollinger, solo con SAR, o cualquier combinación de los anteriores.

## Parámetros

- `UseRsi` – activar filtro RSI (predeterminado: true).
- `UseBollinger` – activar disparador de Bandas de Bollinger (predeterminado: true).
- `UseSar` – activar filtro Parabolic SAR (predeterminado: true).
- `RsiPeriod` – período de cálculo del RSI (predeterminado: 14).
- `BollingerPeriod` – número de barras para las Bandas de Bollinger (predeterminado: 20).
- `BollingerWidth` – amplitud (multiplicador de desviación estándar) para las Bandas de Bollinger (predeterminado: 2).
- `SarStep` – factor de aceleración para el Parabolic SAR (predeterminado: 0.02).
- `SarMax` – factor de aceleración máximo para el Parabolic SAR (predeterminado: 0.2).
- `CandleType` – marco temporal de vela base, 5 minutos por defecto.

## Reglas de Trading

- **Largo**: todos los filtros habilitados proporcionan señales alcistas.
- **Corto**: todos los filtros habilitados proporcionan señales bajistas.
- **Salida**: la señal opuesta cierra la posición.

## Notas

- La estrategia opera sobre un valor con cuatro suscripciones de vela: marcos temporales de 5, 15, 30 y 60 minutos.
- Diseñada como ejemplo educativo de confirmación multi-temporalidad usando la API de alto nivel de StockSharp.
- No hay stop-loss fijo ni objetivos de beneficio; la gestión del riesgo debe añadirse externamente si se requiere.
