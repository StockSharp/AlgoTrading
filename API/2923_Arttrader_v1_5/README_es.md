# Estrategia Arttrader v1.5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Arttrader v1.5 es un sistema de seguimiento de tendencia convertido del asesor experto original de MetaTrader 5. Combina un filtro de pendiente de media móvil exponencial (EMA) de marco temporal superior con un modelo de entrada basado en la acción del precio a corto plazo. La versión de StockSharp mantiene el comportamiento de gestión de riesgo del código fuente, incluyendo el manejo estricto de grandes brechas de velas, ventanas de tiempo para órdenes y salidas de emergencia basadas en la distancia del precio.

Se utilizan dos flujos de velas simultáneamente:

- **Velas de trading** (por defecto 5 minutos) generan entradas, salidas y todos los filtros basados en precio.
- **Velas de tendencia** (por defecto 1 hora) alimentan la EMA que mide la pendiente de la tendencia del marco temporal superior.

La estrategia opera un único instrumento con posiciones netadas. Cuando aparece una señal opuesta, la exposición existente se liquida y se envía una nueva orden de mercado en la dirección de la señal.

## Lógica de señales
1. **Filtro de pendiente EMA**
   - La EMA horaria del precio de apertura de la vela debe tener una pendiente entre `SlopeSmall` y `SlopeLarge` (convertida a unidades de precio por el valor del punto del instrumento).
   - Los trades largos requieren una pendiente positiva, los trades cortos requieren una pendiente negativa.
2. **Temporización intra-barra**
   - Las señales solo se consideran después de que hayan transcurrido `MinutesBegin` minutos en la hora actual, reflejando la verificación `TimeCurrent()` de MT5.
3. **Confirmación de acción del precio**
   - Las entradas largas necesitan una vela bajista o neutral que cierre cerca de su mínimo (`SlipBegin` define la distancia aceptable).
   - Las entradas cortas necesitan una vela alcista o neutral que cierre cerca de su máximo.
4. **Filtros de salto**
   - Cualquier brecha de apertura de una sola vela mayor que `BigJump` (en puntos ajustados) dentro de las últimas seis velas cancela las señales largas y cortas.
   - Cualquier brecha de apertura de dos velas mayor que `DoubleJump` también cancela la señal, evitando trades durante picos volátiles.

## Lógica de salida
1. **Stop inteligente temporizado**
   - Se almacena un precio de entrada de referencia con un desplazamiento opcional `Adjust` para emular el manejo del spread de MT5.
   - Cuando el cierre se mueve contra la posición al menos `StopLoss`, la estrategia espera hasta que hayan pasado `MinutesEnd` minutos de la hora y la vela muestre un patrón de recuperación (requisito `SlipEnd`). Una vez satisfecho, la posición se cierra a mercado.
2. **Stop de emergencia**
   - Si el rango de la vela toca `EmergencyLoss` de distancia del precio de llenado registrado, la posición se cierra inmediatamente. Esto refleja el stop loss del lado del broker del experto original.
3. **Take profit**
   - Una vela que toca la distancia `TakeProfit` desencadena una salida inmediata.
4. **Salvaguarda de volumen**
   - Si el volumen total de la vela anterior no supera `MinVolume`, la posición actual se cierra para evitar operar en períodos ilíquidos.

## Parámetros
| Nombre | Por defecto | Descripción |
|------|---------|-------------|
| `Volume` | 1 | Volumen de la orden de mercado. Se usa tanto para entradas como para invertir una posición opuesta. |
| `EmaPeriod` | 11 | Longitud de la EMA calculada en el marco temporal de tendencia (fuente de precio de apertura). |
| `BigJump` | 30 | Brecha máxima permitida de una vela entre aperturas consecutivas (convertida usando el paso de precio). |
| `DoubleJump` | 55 | Brecha máxima permitida entre aperturas separadas por una vela. |
| `StopLoss` | 20 | Pérdida en puntos que habilita la lógica de salida temporizada. |
| `EmergencyLoss` | 50 | Distancia de stop duro desde la entrada, ejecutado inmediatamente cuando se alcanza. |
| `TakeProfit` | 25 | Distancia del objetivo de beneficio desde la entrada. |
| `SlopeSmall` | 5 | Pendiente EMA mínima (positiva para largos, negativa para cortos) requerida para nuevos trades. |
| `SlopeLarge` | 8 | Magnitud máxima de pendiente EMA permitida para trades. |
| `MinutesBegin` | 25 | Minutos después de la parte superior de la hora antes de que se evalúen nuevas entradas. |
| `MinutesEnd` | 25 | Minutos después de la parte superior de la hora antes de que pueda salir la lógica de stop temporizado. |
| `SlipBegin` | 0 | Distancia máxima entre el cierre de la vela y el extremo usado durante la validación de entrada. |
| `SlipEnd` | 0 | Distancia máxima entre el cierre de la vela y el extremo durante la confirmación del stop. |
| `MinVolume` | 0 | Volumen mínimo de la vela anterior; los valores más bajos fuerzan una salida. |
| `Adjust` | 1 | Ajuste aplicado al almacenar el precio de referencia de entrada interno. |
| `CandleType` | Marco temporal de 5 minutos | Velas de trading usadas para entradas y salidas. |
| `TrendCandleType` | Marco temporal de 1 hora | Tipo de vela que alimenta el filtro de pendiente EMA. |

Todos los parámetros basados en precio se multiplican por el valor del punto del instrumento. Para símbolos FX con tres o cinco decimales, el código multiplica automáticamente el punto por diez, coincidiendo con el manejo de pip usado en la versión MetaTrader.

## Notas de implementación
- Ambos métodos de entrada de mercado llaman a `BuyMarket` o `SellMarket` con suficiente volumen para revertir una posición existente cuando sea necesario.
- La estrategia usa `SubscribeCandles` dos veces solo cuando los tipos de vela de trading y tendencia difieren. Cuando ambos parámetros son iguales, una única suscripción alimenta la EMA y la lógica de trade.
- El stop de emergencia y la gestión de take-profit se implementan en proceso porque StockSharp no adjunta automáticamente órdenes protectoras a las ejecuciones de mercado.
- La API de alto nivel se usa en todo momento (suscripciones `Bind`, `StartProtection`, helpers de gráfico), asegurando que el código permanezca conciso y siga las convenciones del repositorio.

## Consejos de uso
- Ajuste `MinutesBegin` y `MinutesEnd` para instrumentos con diferentes estructuras de sesión. Los valores predeterminados están diseñados para instrumentos con ritmo horario como los principales pares Forex.
- Aumente `MinVolume` en mercados donde las sequías repentinas de volumen coinciden con malos llenados (p. ej., materias primas fuera de horas de pit).
- Debido a que los filtros de salto dependen de solo seis velas, asegúrese de que el marco temporal de trading no sea demasiado grande; de lo contrario, el filtro puede ser demasiado permisivo.
- El filtro de pendiente EMA es sensible al valor del punto del instrumento. Siempre verifique que `BigJump`, `StopLoss` y parámetros similares estén correctamente escalados para el símbolo seleccionado.
