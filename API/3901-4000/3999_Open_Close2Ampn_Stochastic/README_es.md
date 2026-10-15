# Abrir Cerrar2 Ampn Stochastic Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Puerto del MetaTrader 4 expertos *open_close2ampnstochastic_strategy* reconstruido sobre la API de alto nivel de StockSharp.
- Utiliza un oscilador Stochastic clásico (longitud 9, suavizado 3/3) junto con un filtro de acción del precio de dos barras: la vela actual debe continuar la dirección de la anterior antes de enviar una orden.
- Diseñado para operaciones de una sola posición. La fuente de vela predeterminada es una hora, pero se puede ingresar cualquier período de tiempo a través del parámetro `CandleType`.

## Lógica de señal
1. **Guardia de entrada**: solo se puede abrir una posición a la vez. Cuando la estrategia es plana, evalúa la última vela completamente formada:
   - **Entrada larga** cuando la línea principal Stochastic está por encima de la línea de señal *y* tanto la apertura como el cierre de la última vela están por debajo de sus valores anteriores (continuación de la presión a la baja seguida de la fuerza del oscilador).
   - **Entrada corta** cuando la línea principal Stochastic está debajo de la línea de señal *y* la vela muestra una apertura y un cierre más altos que la anterior (empuje hacia arriba con confirmación del oscilador bajista).
2. **Reglas de salida**: mientras existe una posición, las mismas condiciones se reflejan al revés:
   - **Cierre largo** cuando la línea principal cae por debajo de la línea de señal y la nueva vela imprime precios de apertura/cierre más altos.
   - **Cierre en corto** cuando la línea principal se eleva por encima de la línea de señal y la nueva vela imprime precios de apertura/cierre más bajos.
3. **Guardia de reducción**: replica la salida de emergencia de MT4: si la magnitud de la pérdida flotante (PnL realizado + estimación actual basada en velas) alcanza `MaximumRisk × account_margin`, la estrategia liquida la posición inmediatamente. StockSharp no expone el *AccountMargin* de MetaTrader, por lo que el puerto se aproxima a él a través de `Portfolio.BlockedValue` y vuelve a `Portfolio.CurrentValue` cuando el margen bloqueado no está disponible.

## Gestión monetaria
- **BaseVolume** refleja la entrada original `Lots` y se utiliza cuando no hay información de cuenta disponible.
- Si existe una valoración de la cartera, el tamaño bruto del pedido pasa a ser `Portfolio.CurrentValue × MaximumRisk / 1000`, coincidiendo con el tamaño original basado en `AccountFreeMargin`.
- Después de cada operación perdedora, la siguiente posición se reduce en `losses / DecreaseFactor`; el contador de rachas se reinicia después de una operación rentable. El tamaño resultante nunca puede caer por debajo de `MinimumVolume`, que por defecto es 0,1 lotes como el script MQL.
- Todos los volúmenes calculados están alineados con los límites del instrumento (`VolumeStep`, `MinVolume`, `MaxVolume`) antes de enviar órdenes de mercado.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `BaseVolume` | decimales | `0.1` | Tamaño de la orden alternativa cuando no se puede calcular el tamaño basado en el riesgo. |
| `MaximumRisk` | decimales | `0.3` | Fracción del capital utilizada tanto para el dimensionamiento dinámico como para la protección de reducción. Establezca en `0` para deshabilitar los cálculos de riesgo. |
| `DecreaseFactor` | decimales | `100` | Divisor aplicado después de pérdidas consecutivas. Los valores más altos ralentizan la reducción. |
| `MinimumVolume` | decimales | `0.1` | Suelo absoluto para el volumen calculado. |
| `StochasticLength` | entero | `9` | Período de retrospectiva del oscilador Stochastic. |
| `StochasticKLength` | entero | `3` | Período de suavizado de la línea %K. |
| `StochasticDLength` | entero | `3` | Período de suavizado de la línea de señal %D. |
| `CandleType` | `DataType` | `TimeFrame(1h)` | Fuente de vela utilizada para controlar el indicador y los filtros de precios. |

## Notas de implementación
- El PnL flotante requerido por la salida de emergencia se estima con el último cierre de vela y `Strategy.PositionPrice`. Esto refleja la intención de `AccountProfit` en MetaTrader, pero los cálculos reales del corredor pueden diferir.
- Si el conector no expone ni el margen bloqueado ni el valor de la cartera, la protección contra caídas permanece inactiva mientras la estrategia aún se negocia usando `BaseVolume`.
- `StartProtection()` está habilitado al inicio para que los mecanismos de protección de StockSharp (detener/tomar rutas, reconexiones) reflejen la red de seguridad presente en la versión MQL.

## Diferencias con el experto original
- MetaTrader el redondeo de lotes se emula utilizando los metadatos del instrumento disponibles a través de StockSharp. Verifique los valores `VolumeStep`/`MinVolume` del valor negociado para que el tamaño de la posición coincida con las restricciones del corredor.
- El código MT4 se evaluó tick por tick mientras se protegía con `Volume[0]`. El puerto solo procesa velas completadas, lo que evita señales duplicadas y es el patrón recomendado para las estrategias StockSharp.
- Las métricas de la cuenta son aproximaciones; si confía en límites de margen estrictos, ajuste `MaximumRisk` o anule la protección para que coincida con las fórmulas exactas del corredor.
