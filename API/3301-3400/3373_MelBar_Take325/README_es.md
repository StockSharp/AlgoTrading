# Estrategia MelBar Take325
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia MelBar Take325 es una conversión directa del sistema Expert Advisor Studio "MelBar™Take325%™ 5.5Y NZD-USD". Opera en ambas direcciones en NZD/USD utilizando una combinación de rupturas de volumen de ticks, un filtro de oscilación basado en un promedio móvil simple de 12 períodos y un filtro de salida RSI de 14 períodos. El puerto StockSharp mantiene los parámetros de riesgo originales de un stop loss de 16 pips y un takeprofit de 45 pips, expresados ​​en distancias de pips desde el precio de entrada.

La estrategia comienza esperando un aumento en el volumen de ticks, definido como una ruptura por encima del umbral de volumen configurado. Cuando el volumen se expande, comprueba si la media móvil simple formó un punto de inflexión local dos barras antes. Un máximo local en SMA abre una operación larga, mientras que un mínimo local abre una operación corta. Sólo se puede tomar una dirección a la vez y las señales conflictivas se ignoran para evitar cambios bruscos en la misma barra.

Las posiciones abiertas se gestionan activamente. Los niveles de stop-loss y take-profit se aplican cada vez que se cierra una vela, lo que hace que el comportamiento sea similar a la versión MetaTrader. Además, el RSI de 14 períodos se utiliza para forzar salidas: las operaciones largas se cierran cuando RSI cruza hacia abajo a través del nivel configurado (predeterminado 80), y las operaciones cortas se cierran cuando RSI cruza hacia arriba a través del nivel simétrico (predeterminado 20). El máximo/mínimo de la vela procesada se compara con el precio de entrada para activar salidas de stop-loss y take-profit.

## Detalles

- **Criterios de entrada**:
  - **Filtro de volumen**: el volumen de tick de hace dos barras debe estar por debajo del umbral mientras que la barra anterior lo supera.
  - **Largo**: SMA (longitud 12) tiene un pico local hace dos barras (`SMA[t-3] < SMA[t-2]` y `SMA[t-2] > SMA[t-1]`).
  - **Breve**: SMA tiene una vaguada local (`SMA[t-3] > SMA[t-2]` y `SMA[t-2] < SMA[t-1]`).
- **Criterios de salida**:
  - **Stop-loss**: 16 pips desde la entrada, evaluado al cierre de la vela.
  - **Take-profit**: 45 pips desde la entrada, evaluado al cierre de la vela.
  - **Salida RSI larga**: RSI cruza hacia abajo por 80 (`RSI[t-3] > 80` y `RSI[t-2] < 80`).
  - **Salida corta RSI**: RSI cruza hacia arriba por 20 (`RSI[t-3] < 20` y `RSI[t-2] > 20`).
- **Parámetros predeterminados**:
  - Volumen de entrada = 0,1 lotes.
  - Umbral de volumen = 1000 unidades de volumen de tics.
  - SMA período = 12.
  - RSI período = 14.
  - RSI nivel = 80 (la salida corta usa 100 - nivel).
  - Plazo de vela = 30 minutos.
- **Mercado**: Diseñado para NZD/USD pero se puede aplicar a otros pares de divisas.
- **Estilo**: Ruptura de impulso con salidas de reversión a la media.
- **Paradas**: Stop-loss fijo y take-profit; no hay stop dinámico en el código original.
- **Complejidad**: Moderada; combina múltiples filtros pero sin escala de posición.
- **Riesgo**: Medio, ya que el stop es más ajustado que el take-profit pero ambas son distancias fijas.
