# Estrategia E-News Lucky
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia E-News Lucky** es un puerto de StockSharp del asesor experto de MetaTrader `e-News-Lucky`. El sistema automatiza el clásico enfoque de ruptura por noticias:

- En un `PlacementTime` configurable, envía órdenes tanto de compra stop como de venta stop alrededor del precio actual, desplazadas por `DistancePips`.
- Cuando se ejecuta cualquier orden pendiente, la orden opuesta se cancela inmediatamente. Los niveles iniciales de protección de stop-loss y take-profit se adjuntan según los desplazamientos en pips configurados.
- Se puede habilitar un trailing stop mediante `TrailingStopPips` y `TrailingStepPips` para asegurar ganancias a medida que la operación se mueve en la dirección favorable.
- En el `CancelTime` configurado, todas las órdenes pendientes restantes se eliminan y cualquier posición abierta se cierra para evitar mantener riesgo fuera de la ventana de trading.

La estrategia usa datos de velas (`CandleType`, 1 minuto por defecto) solo para rastrear los tiempos programados y actualizar el trailing stop. No depende de cálculos de indicadores.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Volume` | Volumen de orden para cada entrada pendiente. La estrategia envía órdenes simétricas de compra stop y venta stop con este volumen. |
| `StopLossPips` | Distancia entre el precio de entrada y el stop-loss de protección, expresada en pips. Establecer en cero para deshabilitar el stop. |
| `TakeProfitPips` | Distancia entre el precio de entrada y el objetivo de beneficio en pips. Establecer en cero para deshabilitar el objetivo. |
| `TrailingStopPips` | Distancia del trailing stop en pips. El motor de trailing se activa solo cuando este valor es mayor que cero. |
| `TrailingStepPips` | Ganancia mínima en pips requerida antes de que el trailing stop se mueva nuevamente. Previene actualizaciones excesivas del stop en mercados laterales. |
| `DistancePips` | Desplazamiento (en pips) del precio actual usado para colocar las órdenes stop. |
| `PlacementTime` | Hora del día (tiempo del broker/servidor) en que se colocan las órdenes pendientes. Por defecto: 10:30. |
| `CancelTime` | Hora del día en que se cancelan las órdenes pendientes y se cierran las posiciones abiertas. Por defecto: 22:30. |
| `CandleType` | Serie de velas usada para programación y trailing. Por defecto: marco temporal de 1 minuto. |

## Notas de implementación
- El tamaño de pip sigue la lógica de MetaTrader: si el símbolo tiene 3 o 5 dígitos, la estrategia multiplica el paso de precio por 10 para trabajar en unidades de pip.
- Todos los precios se normalizan al paso de precio del instrumento antes de enviar órdenes.
- Los trailing stops comparan el último cierre contra `PositionPrice` y solo mueven el stop de protección cuando la ganancia supera tanto `TrailingStopPips` como `TrailingStepPips`.
- Las órdenes pendientes se recrean cada día de trading cuando se alcanza el tiempo de colocación. Las verificaciones del tiempo de cancelación garantizan que toda la exposición esté plana al final de la ventana.

## Consejos de uso
1. Adjunte la estrategia a un instrumento líquido con spreads ajustados; las distancias de ruptura asumen un comportamiento de precio similar al de las noticias.
2. Establezca `PlacementTime` y `CancelTime` de acuerdo con el calendario económico de interés.
3. Ajuste las distancias en pips para coincidir con la volatilidad del instrumento. Los valores más grandes reducen la posibilidad de falsos disparadores, mientras que los valores más pequeños pueden capturar movimientos más tempranos pero aumentan el riesgo de whipsaw.
4. Deshabilite el trailing manteniendo `TrailingStopPips` en cero si se prefieren stops fijos.
5. Monitoree el deslizamiento y el spread durante noticias de alto impacto para asegurar que las órdenes pendientes se ejecuten como se espera.
