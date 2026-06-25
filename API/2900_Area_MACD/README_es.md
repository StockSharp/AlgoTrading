# Estrategia de Área MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Área MACD evalúa el equilibrio entre el impulso alcista y bajista usando la línea principal del MACD. Para cada vela, la estrategia acumula la suma de todos los valores positivos del MACD y la suma absoluta de todos los valores negativos del MACD durante una ventana de historial configurable. El lado dominante define la dirección de trading: un área positiva más fuerte favorece las posiciones largas, mientras que un área negativa más fuerte favorece la exposición corta. Un interruptor inverso permite operar contra la tendencia detectada cuando sea necesario.

La implementación utiliza la API de alto nivel de StockSharp con suscripciones de velas y vínculos de indicadores. Solo se procesan las velas completadas y toda la lógica de trading está encapsulada dentro del manejador `ProcessCandle`.

## Indicadores y Datos
- **MACD (Media Móvil de Convergencia/Divergencia)** con períodos rápido, lento y de señal configurables.
- **Velas** de un marco temporal definido por el usuario (30 minutos por defecto).

## Reglas de Trading
1. **Entrada Larga** – Cuando el área positiva acumulada del MACD es mayor que el área negativa absoluta acumulada. Si el modo inverso está habilitado, la condición se invierte.
2. **Entrada Corta** – Cuando el área negativa absoluta acumulada del MACD domina. El modo inverso intercambia el comportamiento.
3. **Gestión de Posición** – Cuando aparece una nueva señal de entrada, la estrategia cierra cualquier posición opuesta antes de abrir la nueva para que solo se mantenga una única posición direccional.

## Gestión de Riesgo
- **Stop Loss** – Distancia fija en pips medida desde el precio de entrada. Convertida automáticamente a unidades de precio usando el paso de precio del instrumento.
- **Take Profit** – Objetivo de ganancia fijo en pips usando las mismas reglas de conversión.
- **Trailing Stop** – Stop de seguimiento opcional que se activa una vez que la posición se mueve en ganancias por `TrailingStopPips + TrailingStepPips`. El stop luego sigue el precio con una brecha definida por `TrailingStopPips` y solo se mueve hacia adelante cuando el precio avanza al menos `TrailingStepPips` más. Ambos valores deben ser mayores que cero para habilitar la lógica de trailing.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `OrderVolume` | Volumen de orden usado para entradas de mercado. | 1 |
| `HistoryLength` | Número de velas almacenadas para la comparación de área MACD. | 60 |
| `MacdFastLength` | Período de EMA rápida para el MACD. | 12 |
| `MacdSlowLength` | Período de EMA lenta para el MACD. | 26 |
| `MacdSignalLength` | Período de EMA de señal para el MACD. | 9 |
| `ReverseSignals` | Si está habilitado, intercambia las condiciones de entrada larga y corta. | false |
| `StopLossPips` | Distancia de stop loss expresada en pips. | 100 |
| `TakeProfitPips` | Distancia de take profit en pips. | 150 |
| `TrailingStopPips` | Distancia del trailing stop en pips. Establecer en cero para deshabilitar el trailing. | 5 |
| `TrailingStepPips` | Progreso adicional requerido antes de actualizar el trailing stop. Establecer en cero para deshabilitar el trailing. | 5 |
| `CandleType` | Marco temporal de velas usado por la suscripción. | Marco temporal de 30 minutos |

## Notas de Uso
1. Adjuntar la estrategia a un portafolio y un instrumento, luego ajustar los parámetros para el mercado objetivo.
2. Asegurarse de que tanto `TrailingStopPips` como `TrailingStepPips` sean mayores que cero para habilitar la protección de trailing. De lo contrario, el trailing se ignora y solo los niveles de stop loss/take profit están activos.
3. Monitorear los mensajes de registro para información sobre eventos de stop-loss, take-profit y trailing. Todos los registros se producen en inglés según lo requerido.

## Idea Original
La conversión se basa en el asesor experto de MetaTrader 5 "Area MACD". La versión StockSharp mantiene el concepto central de comparar áreas MACD mientras integra la gestión de riesgo y el manejo de indicadores a través de la API de alto nivel del framework.
