# Estrategia ProtoTipo IX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
ProtoType IX es una estrategia de seguimiento de tendencias con múltiples filtros convertida a partir del asesor experto original MetaTrader 4. El algoritmo observa Williams %R oscilaciones para detectar nuevos movimientos impulsivos y los valida con la expansión del rango verdadero promedio (ATR). Las operaciones se abren sólo cuando la relación recompensa-riesgo proyectada es lo suficientemente atractiva y se confirma la ruptura.

## Indicadores y Señales
- **Williams %R (Período configurable)**: monitorea las rotaciones de sobrecompra/sobreventa. La estrategia registra los dos máximos y mínimos más recientes que aparecen cuando el indicador abandona sus zonas extremas.
- **Rango verdadero promedio (ATR)**: mide la volatilidad actual. Las rupturas se consideran válidas cuando la distancia entre el último swing y el anterior excede `ATR × multiplier`.

## Reglas de entrada
1. Espere a que se registren los máximos y mínimos recientes.
2. Determine la dirección Williams %R. Si el indicador está por encima del umbral superior, se almacena el sesgo alcista; si está por debajo del umbral inferior, se almacena el sesgo bajista.
3. Confirme la estructura del swing con ATR:
   - Tendencia alcista: el último máximo debe exceder el máximo anterior en al menos `ATR × multiplier` y el último mínimo debe ser más alto que el mínimo anterior.
   - Tendencia bajista: el último mínimo debe caer por debajo del mínimo anterior en al menos `ATR × multiplier` y el último máximo debe ser más bajo que el máximo anterior.
4. Evalúe la relación recompensa/riesgo utilizando el precio de cierre actual:
   - **Largo**: objetivo = max(último máximo del swing, máximo del anterior); stop = max(última oscilación baja, oscilación baja anterior).
   - **Corto**: objetivo = min(último mínimo de oscilación, mínimo de oscilación anterior); stop = min (último máximo de oscilación, máximo de oscilación anterior).
5. Solo abra una posición cuando `take profit distance / stop loss distance ≥ TP/SL criteria` y la distancia objetivo sea mayor que el requisito de spread mínimo.

## Reglas de salida
- Las órdenes de protección iniciales se colocan inmediatamente después de la entrada. Los niveles de stop-loss y take-profit se convierten en incrementos de precios para utilizar StockSharp órdenes de protección.
- Una vez que expira el retraso `Zero Bar` configurado, el stop-loss se ajusta utilizando un modelo de seguimiento basado en ATR:
  - Las posiciones largas siguen el stop hasta `max(previous stop, close − 2 × ATR)`.
  - Las posiciones cortas siguen el stop hasta `min(previous stop, close + 2 × ATR)`.

## Dimensionamiento de posiciones
El tamaño del lote se estima a partir del valor de la cartera y el parámetro `Risk %`. La distancia de stop-loss en los pasos de precios se utiliza para traducir el riesgo monetario permitido en volumen. Los volúmenes se normalizan según el paso de volumen del instrumento y se limitan en `Max Order Size`.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| Williams %R Período | Longitud del indicador Williams %R. |
| Criterios WPR | Umbral absoluto que define zonas de sobrecompra/sobreventa. |
| ATR Período | Longitud del indicador de rango verdadero promedio. |
| ATR Multiplicador | Multiplicador aplicado a ATR para la validación de rupturas. |
| Barra cero | Número de barras antes de habilitar el seguimiento ATR. |
| Spread objetivo mínimo | Distancia mínima aceptable al objetivo expresada en múltiplos de dispersión. |
| Criterios TP/SL | Se requiere una relación mínima de obtención de beneficios/límite de pérdidas para iniciar una operación. |
| Órdenes máximas | Máximo de órdenes abiertas simultáneamente. |
| Tamaño máximo del pedido | Límite superior para el volumen del pedido después del dimensionamiento. |
| % de riesgo | Porcentaje de riesgo utilizado para el dimensionamiento de posiciones. |
| Tipo de vela | Tipo de datos de vela para cálculos. |

## Notas
- La estrategia se centra en un único valor pero mantiene la lógica de múltiples filtros del EA original.
- Las órdenes de protección se basan en el escalón del precio del instrumento; asegúrese de que los metadatos del instrumento estén configurados antes de ejecutar la estrategia.
- Los valores cero para el paso de volumen o el precio del paso se sustituyen por valores predeterminados razonables para mantener estable la rutina de dimensionamiento.
