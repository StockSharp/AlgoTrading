# Estrategia PivotEMA3RLHv4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

PivotEMA3RLHv4 es una estrategia de seguimiento de tendencias que combina el nivel de pivote diario con filtros de impulso a corto plazo. Realiza un seguimiento de un promedio móvil exponencial de 3 períodos (EMA) calculado sobre los precios de apertura de velas y lo compara con el mismo EMA calculado sobre los precios de cierre. La configuración se valida con velas Heiken Ashi para confirmar la dirección y con múltiples mediciones de rango verdadero promedio (ATR) para garantizar que la volatilidad se esté expandiendo. La estrategia opera con un solo instrumento en el período intradiario seleccionado y siempre espera a que termine la vela actual antes de tomar una decisión.

## Lógica de trading

1. **Filtro de pivote**: el EMA(3) anterior del precio de apertura debe estar por debajo (para largos) o por encima (para cortos) del nivel de pivote diario, mientras que el EMA(3) actual del precio de apertura debe cruzar al lado opuesto del pivote.
2. **Confirmación de Heiken Ashi**: la vela Heiken Ashi actual debe ser alcista (cerrar por encima de la apertura) para posiciones largas o bajista (cerrar por debajo de la apertura) para posiciones cortas.
3. **Verificación de impulso**: el EMA(3) basado en los precios de cierre debe liderar el EMA en las aperturas en la dirección comercial.
4. **Expansión de la volatilidad**: al menos uno de los valores ATR(4), ATR(8), ATR(12) o ATR(24) debe aumentar en comparación con la vela anterior, y el rango verdadero (ATR con longitud 1) debe aumentar en esta barra o haber aumentado en la barra anterior.
5. **Gestión de posiciones**: solo hay una posición activa a la vez. Los objetivos y paradas de protección se simulan internamente y se ejecutan mediante órdenes de mercado cuando se alcanzan.

Las señales de salida reflejan las reglas de entrada: cuando aparecen las condiciones opuestas, la estrategia cierra la operación actual. Además, los mecanismos opcionales de stop-loss, take-profit y trailing stop pueden cerrar una operación antes.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Plazo de trabajo de las velas estratégicas. |
| `StopLossPips` | Distancia de parada inicial en pips desde el precio de entrada. Establezca en cero para desactivar. |
| `TakeProfitPips` | Distancia objetivo de ganancias en pips. Establezca en cero para desactivar. |
| `UseTrailingStop` | Habilita o deshabilita la gestión de trailing stop. |
| `TrailingStopType` | Modo de seguimiento: 1 mantiene una distancia fija, 2 se activa después de que el precio se mueve en `TrailingStopPips`, 3 usa la escalera de múltiples etapas que se describe a continuación. |
| `TrailingStopPips` | Distancia (en pips) utilizada por el tipo de seguimiento 2. |
| `FirstMovePips` / `FirstStopLossPips` | Distancia de disparo y compensación de parada resultante para la primera etapa del tipo de seguimiento 3. |
| `SecondMovePips` / `SecondStopLossPips` | Distancia de disparo y compensación de parada resultante para la segunda etapa del tipo de seguimiento 3. |
| `ThirdMovePips` / `TrailingStop3Pips` | Distancia de disparo y distancia de seguimiento dinámica para la etapa final del tipo de seguimiento 3. |

## Modos de parada dinámica

- **Tipo 1**: reposiciona la parada para que nunca se retrase el precio más que la distancia de parada inicial.
- **Tipo 2**: espera a que el precio se mueva `TrailingStopPips` antes de bloquear las ganancias con la misma distancia.
- **Tipo 3**: utiliza hasta tres umbrales: los dos primeros mueven el stop a compensaciones predefinidas, mientras que el tercero se convierte en un trailing stop normal.

## Notas

- La estrategia se suscribe a velas diarias para calcular el nivel de pivote a partir del máximo, mínimo y cierre del día anterior.
- Los indicadores se actualizan dentro del controlador de velas utilizando únicamente barras terminadas, lo que mantiene la lógica compatible con entornos en línea y de backtesting.
- La versión original MetaTrader se basaba en paradas por parte del corredor; este puerto los simula y sale con órdenes de mercado cuando es necesario.
