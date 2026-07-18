# Estrategia de MACD Cleaner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **MACD Cleaner** es una conversión del asesor experto "MACD Cleaner" de MetaTrader 5. Analiza velas completadas de un único marco temporal y coloca operaciones cuando la línea principal del MACD aumenta o disminuye de forma monótona durante tres barras cerradas consecutivas. El sistema siempre mantiene como máximo una posición direccional y cambia cuando el momentum se invierte.

## Lógica de negociación
- En cada vela terminada la estrategia lee la línea MACD calculada con los períodos de rápido, lento y señal configurados.
- Si los últimos tres valores de MACD son no decrecientes, la estrategia prepara una entrada larga. Si existe una posición corta se cierra primero, luego se abre una nueva posición larga.
- Si los últimos tres valores de MACD son no crecientes, la estrategia prepara una entrada corta. Las posiciones largas existentes se cierran antes de abrir la corta.
- Los niveles de stop-loss y take-profit de protección se evalúan en los máximos y mínimos de las velas usando los desplazamientos basados en pips.
- Cuando los parámetros de trailing están habilitados, el stop se mueve en la dirección de la operación una vez que el precio avanza al menos el paso de trailing configurado.
- Todas las órdenes de salida se emiten como órdenes de mercado usando el volumen de posición agregado para asegurar que toda la posición esté cerrada.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `CandleType` | Velas de 1 hora | Marco temporal usado para el cálculo de MACD y evaluación de órdenes. |
| `TradeVolume` | 1 | Volumen base enviado para una nueva posición. Si el lado opuesto está abierto, el volumen de posición absoluto se agrega para cerrarlo antes de revertir. |
| `StopLossPips` | 35 | Distancia de stop-loss en pips desde el precio de entrada. Establecer en cero para desactivar el stop. |
| `TakeProfitPips` | 30 | Distancia de take-profit en pips desde el precio de entrada. Establecer en cero para desactivar el objetivo. |
| `TrailingStopPips` | 0 | Distancia del trailing stop. Cuando es cero la lógica de trailing está desactivada. |
| `TrailingStepPips` | 5 | Movimiento favorable mínimo (en pips) requerido antes de ajustar el trailing stop. Se ignora cuando el trailing stop está desactivado. |
| `MacdFastPeriod` | 15 | Longitud de la EMA rápida para el indicador MACD. |
| `MacdSlowPeriod` | 33 | Longitud de la EMA lenta para el indicador MACD. |
| `MacdSignalPeriod` | 11 | Longitud de la EMA de señal para el indicador MACD. |

## Gestión de órdenes
- Salidas largas: la estrategia emite una orden de venta de mercado cuando se alcanza el stop-loss, take-profit o nivel de trailing.
- Salidas cortas: una orden de compra de mercado cierra la posición bajo las mismas condiciones, reflejadas para operaciones cortas.
- Después de que la posición se cierra completamente, el estado del trailing se restablece para que la siguiente operación comience con niveles frescos.

## Notas
- El tamaño de pip se deriva automáticamente del instrumento. Para símbolos con 3 o 5 decimales el pip equivale a diez pasos de precio mínimo, imitando la implementación original de MetaTrader.
- La lógica solo evalúa velas completadas y no actúa sobre cambios intrabarra.
- Para desactivar la gestión de riesgo establezca las distancias en pips correspondientes en cero. El trailing requiere tanto `TrailingStopPips` como un `TrailingStepPips` positivo para funcionar.
