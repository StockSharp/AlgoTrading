# Estrategia MA Crossover ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
La estrategia **MA Crossover ADX** es un port directo del asesor experto de MetaTrader `MA_Crossover_ADX`. Combina la pendiente de una media móvil exponencial (EMA) con confirmación del Average Directional Index (ADX) para participar solo en entornos tendenciales. La implementación StockSharp procesa velas completadas de un marco configurable y sincroniza actualizaciones de EMA y ADX antes de emitir señales. Las distancias protectoras de stop loss y take profit se adjuntan automáticamente a cada nueva posición mediante los parámetros de riesgo basados en puntos.

## Indicadores y datos
- **Media móvil exponencial (EMA):** actúa como filtro principal de tendencia. La estrategia sigue los tres últimos valores de EMA para calcular dos pendientes consecutivas, imitando las comprobaciones `StateEMA(0)` y `StateEMA(1)` del EA original.
- **Average Directional Index (ADX):** proporciona la línea principal de fuerza de tendencia y los indicadores direccionales positivo/negativo (DI+/DI-). El diferencial entre DI+ y DI- replica la condición `StateADX(0)` del EA, mientras que la línea principal exige una fuerza mínima.
- **Serie de precios de cierre:** el cierre de la vela anterior se compara con la EMA anterior para asegurar que el mercado se separó de la media antes de tomar la entrada.

Todos los indicadores operan sobre la misma suscripción de velas, lo que garantiza que los valores de EMA y ADX estén finalizados para la misma barra exacta antes de tomar decisiones.

## Lógica de negociación
### Entrada larga
1. La pendiente actual de EMA (`EMA[0] - EMA[1]`) es positiva.
2. La pendiente previa de EMA (`EMA[1] - EMA[2]`) también es positiva, señalando aceleración.
3. El cierre de la vela anterior está por encima del valor EMA anterior.
4. La línea principal ADX es mayor que el umbral configurado.
5. DI+ supera a DI-, indicando dominio direccional alcista.

Cuando todas las reglas se alinean y no hay posición abierta, la estrategia envía una orden de compra a mercado con el volumen configurado. Si existe una posición corta, se cierra tan pronto como aparecen las condiciones alcistas.

### Entrada corta
1. La pendiente actual de EMA es negativa.
2. La pendiente previa de EMA también es negativa.
3. El cierre de la vela anterior está por debajo del valor EMA anterior.
4. La línea principal ADX es mayor que el umbral.
5. DI- supera a DI+, resaltando momentum bajista.

Se coloca una orden de venta a mercado cuando las cinco condiciones se cumplen y la estrategia está plana. Las posiciones largas abiertas se cierran de inmediato si aparecen los filtros bajistas.

### Reglas de salida
- **Posiciones largas:** salir cuando se materializan las condiciones de entrada corta, asegurando que el sistema abandone los largos cuando el momentum del mercado gira a la baja.
- **Posiciones cortas:** salir cuando se materializan las condiciones de entrada larga.
- **Órdenes protectoras:** `StartProtection` adjunta órdenes de stop loss y take profit calculadas desde el `PriceStep` del instrumento multiplicado por las distancias configuradas en puntos. Estas órdenes siguen la posición activa según el motor nativo de órdenes protectoras de StockSharp.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `AdxPeriod` | 33 | Número de barras usadas para calcular ADX. |
| `AdxThreshold` | 22 | Valor mínimo de la línea principal ADX requerido para validar una tendencia. |
| `EmaPeriod` | 39 | Longitud de la EMA usada para detectar pendiente. |
| `StopLossPoints` | 400 | Distancia de stop loss medida en puntos del instrumento (multiplicada por `PriceStep`). |
| `TakeProfitPoints` | 900 | Distancia de take profit medida en puntos del instrumento. |
| `TradeVolume` | 0.1 | Volumen enviado con cada nueva orden de mercado. |
| `CandleType` | Marco de 1 hora | Tipo de vela que alimenta todos los cálculos de indicadores. |

## Notas de uso
- Asegúrese de que el instrumento proporcione un `PriceStep` válido. Si no hay paso disponible, la estrategia usa `1` punto por defecto para poder calcular las órdenes protectoras.
- Los parámetros son aptos para optimización mediante `SetCanOptimize(true)`, lo que permite backtesting u optimización con distintas combinaciones de EMA/ADX.
- Todos los comentarios de la implementación C# están escritos intencionalmente en inglés, según las directrices del proyecto.
