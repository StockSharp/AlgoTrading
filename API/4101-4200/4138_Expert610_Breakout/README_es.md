# Estrategia de ruptura Expert610
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Expert610 Breakout es una adaptación de C# del MetaTrader 4 asesor experto `Expert610.mq4`. El robot original espera un
vela ancha y luego estaciona una orden stop de compra y una orden stop de venta alrededor de la barra anterior. El tamaño de la posición se deriva de la
porcentaje de capital libre que el comerciante está dispuesto a arriesgar, y las distancias entre el límite de pérdidas y la obtención de ganancias se expresan en pips. esto
La versión StockSharp refleja ese comportamiento utilizando el nivel alto API al tiempo que expone cada perilla de ajuste como un parámetro de estrategia.

## Lógica de trading
1. **Recopilación de datos**
   - La estrategia se suscribe a un tipo de vela configurable y almacena la barra terminada más reciente.
   - Las actualizaciones del libro de pedidos se monitorean para estimar el diferencial actual entre oferta y demanda. Cuando no hay profundidad disponible, la contribución del diferencial
El valor predeterminado es cero, reproduciendo el comportamiento original EA en corredores sin spreads reales.
2. **Filtro de volatilidad**
   - El máximo de la vela anterior menos el cierre actual y el cierre actual menos el mínimo anterior deben exceder
`ThresholdPips` (convertido a unidades de precio absoluto).
   - La apertura de la vela actual debe estar estrictamente por debajo del máximo anterior para permitir una configuración de compra y estrictamente por encima del mínimo anterior para
permitir una configuración de venta. Cuando se cumplen ambas condiciones, el algoritmo genera órdenes pendientes simétricas.
3. **Realización de pedidos**
   - Las paradas de compra se colocan en `previous high + BreakoutOffset + spread`, coincidiendo con el código MT4 donde se utiliza el precio de venta.
   - Los stop de venta se colocan en `previous low - BreakoutOffset`, manteniéndose fiel también al guión original que ignora el
diferencial en el lado de la oferta.
   - Sólo un par de órdenes pendientes puede estar activo en cualquier momento. Si una orden ya está funcionando, las nuevas señales se omitirán.
4. **Gestión de riesgos**
   - El tamaño del lote se deriva del capital libre (`Portfolio.CurrentValue - Portfolio.BlockedValue`) multiplicado por
`RiskPercent / 100`. La cantidad se redondea a `RoundingDigits` y se convierte en lotes utilizando la misma heurística que el MT4.
código: `lot = risk / stopPips * 0.1`, que supone que un pip de un lote de 0,1 equivale a una unidad de moneda de la cuenta.
   - El lote calculado está alineado con los límites de intercambio y el parámetro `MinimumVolume` antes de enviarse al lugar.
   - `StartProtection` adjunta paradas y objetivos basados en precio a cada posición resultante, de modo que los llenados reciban inmediatamente el
compensaciones configuradas `StopLossPips` y `TakeProfitPips`.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `RoundingDigits` | Se utilizan decimales al redondear los cálculos de riesgo y volumen. | `2` | Debe ser no negativo. |
| `RiskPercent` | Porcentaje de capital libre arriesgado en cada entrada. | `1` | Establezca en `0` para deshabilitar el tamaño dinámico y recurrir a `MinimumVolume`. |
| `MinimumVolume` | Límite inferior estricto para el volumen de pedidos pendientes. | `0.1` | También respeta las normas de seguridad `MinVolume` y `VolumeStep`. |
| `ThresholdPips` | Distancia mínima desde el último cierre hasta los extremos de la vela anterior. | `5` | Medido en pips y convertido con el tamaño de pip detectado. |
| `BreakoutOffsetPips` | Se agrega un búfer más allá del máximo/mínimo anterior al preparar órdenes. | `2` | Aplicado simétricamente a ambos lados. |
| `StopLossPips` | Distancia de stop-loss adjunta a las órdenes ejecutadas. | `5` | Expresado en pips y enviado a `StartProtection`. |
| `TakeProfitPips` | Distancia de obtención de beneficios asociada a las órdenes ejecutadas. | `10` | Expresado en pips; configúrelo en `0` para deshabilitar el objetivo. |
| `CandleType` | Serie de velas utilizadas para evaluar la ruptura. | `1 hour` período de tiempo | Acepta cualquier `DataType` compatible con StockSharp. |

## Notas de implementación
- El tamaño del pip se deriva de los valores `PriceStep` y `Decimals` del instrumento (los símbolos Forex de 5 y 3 dígitos reciben un valor de ×10).
ajuste) para mantener la conversión idéntica a la fórmula MQL4.
- El redondeo del tamaño de la orden respeta `VolumeStep`, se limita a `MinVolume`/`MaxVolume` y finalmente aplica el nivel de estrategia
`MinimumVolume` para que las solicitudes resultantes siempre sean negociables.
- La compensación del diferencial utiliza la mejor oferta/demanda extraída del libro de órdenes suscrito. Esto produce el mismo precio de entrada que el
Implementación de MT4 cuando la plataforma proporciona diferenciales en vivo y, de lo contrario, se degrada elegantemente.
- Las órdenes pendientes se borran del estado interno una vez que StockSharp las informa como completadas, canceladas o fallidas, lo que permite que
lógica para enviar nuevos pedidos en la siguiente vela calificada.

## Diferencias frente a la versión MQL
- El EA original redondeó tanto el riesgo como el volumen usando `Digits2Round`. El puerto mantiene esa característica pero además alinea el
resultado a pasos de volumen específicos del intercambio.
- En lugar de adjuntar precios de protección directamente a las órdenes pendientes, la estrategia StockSharp se basa en `StartProtection`, por lo que
cada posición ocupada recibe automáticamente órdenes de stop-loss y take-profit.
- La información de la cartera reemplaza las funciones MT4 `AccountBalance()` y `AccountMargin()` para obtener capital libre; si estos datos
no está disponible, la estrategia vuelve elegantemente al tamaño `MinimumVolume`.
- Todos los cálculos operan solo en velas terminadas, lo que evita el repintado dentro de la barra y hace coincidir el bucle basado en ticks `start()`
una vez que el bar cierra.
