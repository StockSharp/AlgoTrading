# Estrategia MACD Stochastic 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce la lógica del experto MetaTrader "MACD Stochastic 2" con la API de alto nivel de StockSharp. Combina un filtro de oscilación MACD de tres barras con un oscilador estocástico para buscar reversiones de momentum cerca de regiones de sobrecompra y sobreventa. El riesgo se controla mediante stops y take-profits específicos por dirección y un trailing stop opcional que opera en unidades de pips.

## Descripción general

- Funciona en cualquier instrumento y marco temporal proporcionado mediante el parámetro `CandleType`.
- Usa la línea principal de MACD para confirmar mínimos/máximos locales, mientras que el histograma y la línea de señal del MACD permanecen disponibles para visualización.
- Confirma entradas con una lectura de %K del estocástico por debajo de 20 para largos y por encima de 80 para cortos.
- Adapta el manejo de pips de MetaTrader derivando el tamaño del pip del step de precio del instrumento, multiplicando por 10 cuando el símbolo tiene 3 o 5 decimales.

## Lógica de negociación

### Entrada larga

1. Los valores de la línea principal de MACD de la vela actual y las dos anteriores completadas están todos por debajo de cero.
2. El valor actual de MACD es mayor que el anterior, mientras que el anterior es menor que el de hace dos barras (mínimo local).
3. %K del estocástico está por debajo de 20 (sobrevendido).
4. No hay posición larga existente abierta (`Position <= 0`). Cualquier posición corta se cierra antes de entrar en el nuevo largo.

### Entrada corta

1. Los valores de la línea principal de MACD de la vela actual y las dos anteriores completadas están todos por encima de cero.
2. El valor actual de MACD es menor que el anterior, mientras que el anterior es mayor que el de hace dos barras (máximo local).
3. %K del estocástico está por encima de 80 (sobrecomprado).
4. No hay posición corta existente abierta (`Position >= 0`). Cualquier posición larga se cierra antes de entrar en el nuevo corto.

### Gestión del riesgo y salidas

- **Stop fijo / Take Profit:** Cada dirección tiene distancias de stop-loss y take-profit independientes basadas en pips. Los pips se convierten a offsets de precio absolutos usando el tamaño del pip calculado.
- **Trailing Stop:** Cuando está habilitado, el trailing stop se activa después de que el precio avanza más allá de la distancia de trailing. El stop solo se sube/baja cuando el movimiento supera el paso de trailing configurado para evitar excesiva rotación de órdenes.
- **Señales opuestas:** Entrar en una señal opuesta primero cierra la posición existente y luego abre la nueva con el volumen de negociación configurado.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `TradeVolume` | `1` | Volumen de orden enviado con cada nueva operación. |
| `StopLossBuyPips` | `50` | Distancia en pips del stop-loss para largos. Establezca en `0` para desactivar. |
| `StopLossSellPips` | `50` | Distancia en pips del stop-loss para cortos. Establezca en `0` para desactivar. |
| `TakeProfitBuyPips` | `50` | Distancia en pips del take-profit para largos. Establezca en `0` para desactivar. |
| `TakeProfitSellPips` | `50` | Distancia en pips del take-profit para cortos. Establezca en `0` para desactivar. |
| `TrailingStopPips` | `0` | Distancia del trailing stop en pips. `0` desactiva el trailing. |
| `TrailingStepPips` | `5` | Ganancia mínima en pips antes de actualizar el trailing stop. Debe mantenerse positivo cuando el trailing está activo. |
| `MacdFastPeriod` | `12` | Longitud de la EMA rápida para MACD. |
| `MacdSlowPeriod` | `26` | Longitud de la EMA lenta para MACD. |
| `MacdSignalPeriod` | `9` | Longitud del suavizado de señal para MACD. |
| `StochasticKPeriod` | `5` | Período de lookback para el %K estocástico. |
| `StochasticDPeriod` | `3` | Período de suavizado para el %D estocástico. |
| `StochasticSlowing` | `3` | Suavizado adicional aplicado al %K estocástico. |
| `CandleType` | `marco temporal de 1h` | Tipo de vela (marco temporal) utilizado para los cálculos de indicadores. |

## Notas

- El cálculo del tamaño del pip refleja el experto original de MetaTrader: `pip = PriceStep` y se multiplica por 10 cuando el instrumento cotiza con 3 o 5 decimales.
- Los umbrales del estocástico (20/80) permanecen como constantes al igual que en el script original. Ajústalos directamente en el código si se necesitan niveles personalizados.
- La estrategia opera solo en velas completamente cerradas, garantizando consistencia con la ejecución al cierre de barra de MetaTrader.

## Uso

1. Configura el instrumento deseado, `CandleType` y el volumen antes de iniciar la estrategia.
2. Ajusta los parámetros de stop, take-profit y trailing para adaptarlos a la volatilidad del instrumento.
3. Opcionalmente optimiza las longitudes de MACD y estocástico usando el optimizador de StockSharp gracias a los parámetros expuestos.
4. Monitorea los objetos del gráfico (velas, MACD, estocástico, operaciones propias) añadidos automáticamente cuando hay un área de gráfico disponible.
