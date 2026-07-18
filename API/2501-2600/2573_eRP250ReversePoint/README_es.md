# Estrategia eRP250ReversePoint
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una portación a StockSharp del asesor experto de MetaTrader 5 `e_RP_250`. El sistema original opera reversiones detectadas por un indicador personalizado *rPoint*. Dado que ese indicador no está disponible en StockSharp, la conversión recrea el mismo comportamiento con rastreadores de precio máximo y mínimo móviles. Cuando aparece un nuevo máximo o mínimo de swing, la estrategia invierte la posición y adjunta la misma lógica de stop-loss, take-profit y trailing opcional que la versión MQL.

El código fuente original no publicó resultados de rendimiento verificados, por lo que debe realizar su propia evaluación antes de desplegar la estrategia en producción.

## Lógica de trading

- Suscribirse a velas definidas por el parámetro `CandleType` (velas de 5 minutos por defecto).
- Rastrear el máximo más alto y el mínimo más bajo en las últimas `ReversePoint` barras (250 por defecto).
- Cuando el candle actual establece un nuevo máximo más alto, cerrar cualquier posición larga y abrir una posición corta.
- Cuando el candle actual establece un nuevo mínimo más bajo, cerrar cualquier posición corta y abrir una posición larga.
- Los niveles protectores de stop-loss y take-profit se expresan en puntos de precio y se reproducen a través de `StartProtection`.
- Los stops trailing opcionales bloquean ganancias una vez que el precio se mueve el número de puntos configurado.

Solo hay una posición activa en cualquier momento. La estrategia también bloquea órdenes duplicadas durante el mismo candle recordando el último tiempo de ejecución, replicando la protección `TimeN` del script MQL.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPoints` | Distancia en puntos de precio para la orden de take-profit (predeterminado **15**). Establecer en cero para deshabilitar la toma de ganancias automática. |
| `StopLossPoints` | Distancia en puntos de precio para la orden de stop-loss (predeterminado **999**). Establecer en cero para operar sin un stop fijo. |
| `TrailingStopPoints` | Distancia opcional de stop trailing en puntos de precio (predeterminado **0** deshabilita la lógica de trailing). |
| `ReversePoint` | Número de velas usadas para detectar puntos de reversión. Los valores más grandes reaccionan más lento pero filtran el ruido. |
| `CandleType` | Agregación de velas a analizar. El predeterminado es un marco temporal de 5 minutos pero puede cambiar a cualquier `DataType`. |

## Gestión de posición

- `StartProtection` aplica las mismas distancias de stop-loss y take-profit que el experto de MT5.
- El stop trailing rastrea el precio más favorable después de la entrada y sale cuando el precio revierte el monto configurado.
- Las señales de reversión del lado opuesto cierran inmediatamente la posición actual antes de abrir una nueva.

## Notas de uso

- Asegúrese de que la fuente de datos soporte el tipo de vela seleccionado, de lo contrario no se generarán señales.
- La estrategia depende de precios decimales. Verifique que la propiedad `PriceStep` del instrumento refleje correctamente el valor del punto.
- Pruebe diferentes valores de `ReversePoint` para adaptar la sensibilidad al rompimiento a la volatilidad del instrumento operado.
