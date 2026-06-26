# Estrategia de Hoop Master Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convertida del asesor experto de MetaTrader 5 **"Hoop master 2"** por Vladimir Karputov.
- Construye una caja de ruptura alrededor del precio actual y arma tanto órdenes de compra como de venta stop cada vez que cierra una nueva vela.
- Automáticamente replica el comportamiento de MT5 de duplicar el tamaño del lote después de una operación perdedora y restablecerlo después de un ciclo rentable.

## Lógica de negociación
1. Suscribirse a la serie de velas configurada y esperar solo velas terminadas. Una nueva vela actúa como el "tick" que vuelve a armar las órdenes pendientes.
2. Cuando la estrategia está plana:
   - Colocar un **buy stop** `IndentPips` puntos por encima del último cierre.
   - Colocar un **sell stop** `IndentPips` puntos por debajo del último cierre.
   - Convertir los pips de MetaTrader en unidades de precio absolutas usando el `PriceStep` del instrumento y el ajuste de dígitos fraccionarios (×10 para cotizaciones de 3 o 5 decimales).
3. Cada orden pendiente almacena sus propios niveles de stop-loss y take-profit. Una vez que la orden se ejecuta, la orden opuesta se cancela y la protección almacenada se recrea con órdenes nativas de bolsa (`SellStop`/`SellLimit` para largos, `BuyStop`/`BuyLimit` para cortos).
4. Si una orden protectora cierra la posición, la orden adjunta restante se cancela para evitar salidas duplicadas.
5. La lógica de trailing stop opcional mueve el stop protector a favor de la operación una vez que el precio ha avanzado al menos `TrailingStopPips` y la mejora supera `TrailingStepPips`.
6. Después de cada ciclo de plano a plano, se evalúa el PnL realizado. Un ciclo negativo multiplica el volumen de trabajo por `LossMultiplier`; de lo contrario, el volumen se restablece al `Volume` base.

## Parámetros
| Parámetro | Descripción | Predeterminado | Notas |
|-----------|-------------|---------|-------|
| `Volume` | Tamaño de orden base usado al armar nuevas órdenes pendientes. | Propiedad `Volume` de la estrategia | Se duplica después de un ciclo perdedor según `LossMultiplier`. |
| `StopLossPips` | Distancia de stop-loss en pips de MetaTrader. | `25` | Convertido a precio usando el asistente de tamaño de pip. `0` desactiva el stop. |
| `TakeProfitPips` | Distancia de take-profit en pips de MetaTrader. | `70` | Convertido a precio. `0` desactiva el objetivo. |
| `TrailingStopPips` | Distancia entre el precio y el trailing stop. | `0` | Establecer en `0` para desactivar el trailing. |
| `TrailingStepPips` | Mejora mínima antes de mover el trailing stop. | `5` | Solo se usa cuando `TrailingStopPips` es mayor que cero. |
| `IndentPips` | Desplazamiento añadido al último cierre al armar órdenes pendientes. | `15` | Asegura que las órdenes stop estén fuera del ruido de precio inmediato. |
| `LossMultiplier` | Multiplicador aplicado al siguiente ciclo después de una pérdida. | `2` | Implementa el dimensionamiento de posición estilo martingala del EA MT5. |
| `CandleType` | Tipo/marco temporal de vela que activa el re-armado. | `Marco temporal de 1 hora` | Cambiar para coincidir con el gráfico usado en las pruebas. |

## Gestión monetaria y protecciones
- Cada entrada ejecutada reconstruye inmediatamente su stop-loss y take-profit como órdenes reales de bolsa para que las protecciones funcionen incluso si la estrategia se desconecta.
- `StartProtection()` se invoca durante el inicio para liquidar posiciones extraviadas de ejecuciones anteriores.
- La lógica de trailing ajusta las órdenes stop existentes en lugar de enviar salidas de mercado, manteniendo el comportamiento consistente con las modificaciones de MT5.

## Notas de implementación
- Sigue la API de alto nivel de StockSharp: suscripciones de velas, `BuyStop`/`SellStop` para entradas, y `BuyLimit`/`SellLimit` para órdenes de take-profit.
- Todos los comentarios textuales dentro del código están en inglés, mientras que la documentación externa (este README y traducciones) proporciona descripciones detalladas para los usuarios.
- La conversión de pips de MetaTrader respeta los símbolos de dígitos fraccionarios (3 o 5 decimales) multiplicando el paso del broker por 10, coincidiendo con la lógica `m_adjusted_point` del EA original.
