# RSI Estrategia de alerta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **RSI Estrategia de alerta** reproduce el comportamiento del asesor experto MetaTrader 5 "RSI Alerta" dentro del marco StockSharp. El robot original buscaba lecturas del índice de fuerza relativa (RSI) que cruzaran niveles profundamente de sobreventa (≤20) o sobrecompra (≥80) y enviaba inmediatamente notificaciones de alerta mientras abría posiciones en el mercado. La versión convertida mantiene esta filosofía basada en eventos: escucha las velas completadas, evalúa el RSI y automáticamente invierte la posición enviando órdenes de mercado cuando se alcanzan los umbrales configurados.

## Lógica de trading
1. Suscríbase a la serie de velas configurada (predeterminado: período de tiempo de 1 minuto) e introduzca los precios de cierre en un indicador `RelativeStrengthIndex`.
2. Ignore las velas incompletas y espere hasta que el indicador RSI esté completamente formado. Esto refleja el experto MQL, que solo evaluó las condiciones una vez por nueva barra.
3. Generar señales comerciales:
   - **Señal de compra** – RSI ≤ `OversoldLevel`. La estrategia cierra cualquier exposición corta y abre una posición larga con el volumen configurado.
   - **Señal de venta** – RSI ≥ `OverboughtLevel`. La estrategia cierra cualquier exposición larga y abre una posición corta con el volumen configurado.
4. Las órdenes siempre se realizan con `BuyMarket`/`SellMarket`, por lo que no hay órdenes pendientes, niveles de stop-loss o take-profit. La implementación MetaTrader permitía entradas SL/TP opcionales, pero de forma predeterminada dependía de la gestión manual. El puerto StockSharp se centra en la conversión de alerta a operación y deja la gestión de riesgos a módulos externos (por ejemplo, `StartProtection()` o controles a nivel de cartera).

La estrategia se mantiene plana entre señales. Cuando aparece un disparador opuesto, invierte la posición agregando suficiente volumen para aplanar la exposición existente antes de entrar en la nueva dirección, exactamente como lo hizo el EA original al disparar alertas consecutivas.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `OrderVolume` | 0,01 | Tamaño comercial para órdenes de mercado. Al revertir, la estrategia agrega la cantidad requerida para cubrir la posición existente antes de volver a ingresar. |
| `RsiPeriod` | 30 | RSI período promedio. Debe ser un número entero positivo. |
| `OverboughtLevel` | 80 | RSI umbral que emite una señal de venta. Se puede optimizar para ajustar la agresividad. |
| `OversoldLevel` | 20 | RSI umbral que emite una señal de compra. |
| `CandleType` | 1 minuto `TimeFrameCandle` | Fuente de datos de velas utilizada para el cálculo de RSI. Cámbielo para analizar marcos de tiempo más altos. |

Todos los parámetros se exponen a través de `StrategyParam<T>` para que aparezcan en el diseñador StockSharp, se puedan guardar en ajustes preestablecidos XML y admitan escenarios de optimización.

## Notas de implementación
- El StockSharp API de alto nivel se utiliza en todo momento: las velas se obtienen a través de `SubscribeCandles()` y el RSI se actualiza a través de `subscription.Bind(indicator, callback)`. No se requiere manipulación manual del búfer ni copia histórica.
- La propiedad base `Strategy.Volume` está sincronizada con el parámetro `OrderVolume` para que la inversión de posición funcione correctamente incluso si el usuario cambia el tamaño del lote en tiempo de ejecución.
- Los comentarios en línea y la documentación XML están escritos en inglés para cumplir con los requisitos del proyecto.
- La salida del gráfico es opcional pero compatible: cuando la estrategia se ejecuta dentro del diseñador, traza las velas de precios, las operaciones ejecutadas y los valores del indicador RSI.

## Consejos de uso
- Combine la estrategia con módulos externos de stop-loss/take-profit si es necesario un control de riesgos automatizado.
- Optimice los umbrales de RSI al adaptarse a mercados con diferentes regímenes de volatilidad.
- Aumente el marco de tiempo de la vela para las configuraciones de swing o mantenga la serie predeterminada de 1 minuto para alertas de estilo especulación, como en el script original.
