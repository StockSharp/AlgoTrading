# Estrategia ColorJFatl Digit TM Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia ColorJFatl Digit TM Plus es un port directo del asesor experto de MetaTrader 5 *Exp_ColorJFatl_Digit_Tm_Plus*. Opera reversiones de pendiente de una Línea de Tendencia Adaptativa Rápida (FATL) suavizada con una Media Móvil Jurik (JMA). El indicador original publica tres colores (arriba, plano, abajo). La estrategia reacciona cuando el color en la última barra terminada cambia y alinea la posición con la nueva pendiente.

La implementación en StockSharp mantiene el comportamiento de alto nivel de la versión MQL: las órdenes se generan en velas cerradas, los exits basados en tiempo son opcionales, y la entrada de dimensionamiento de lote está representada por el parámetro `TradeVolume`.

## Lógica de señales

1. **Cálculo del indicador**
   - Los precios se alimentan a través del filtro digital FATL de 39 taps suministrado con el indicador original.
   - La serie filtrada se suaviza con una Media Móvil Jurik. La longitud, el precio aplicado y la precisión de redondeo pueden personalizarse mediante parámetros.
   - El estado de color se determina por el signo de la diferencia entre los valores suavizados actual y anterior: `2` para pendiente alcista, `0` para pendiente bajista y `1` para neutral/sin cambio.

2. **Condiciones de entrada**
   - **Entrada larga** – habilitada por `EnableBuyEntries`. Se activa cuando el color de la barra actual se convierte en `2` mientras el color de la barra anterior era menor que `2`. Cualquier posición corta existente se cierra primero cuando `EnableSellExits` es true.
   - **Entrada corta** – habilitada por `EnableSellEntries`. Se activa cuando el color de la barra actual se convierte en `0` mientras el color anterior era mayor que `0`. Cualquier posición larga existente se cierra primero cuando `EnableBuyExits` es true.
   - Solo puede haber una posición abierta a la vez. Las órdenes se envían al cierre de la vela de confirmación.

3. **Condiciones de salida**
   - **Salidas por reversión de pendiente** – cuando la pendiente se invierte en la dirección opuesta, el flag correspondiente `EnableBuyExits` o `EnableSellExits` cerrará la posición abierta.
   - **Salida basada en tiempo** – si `UseTimeExit` está habilitado, una posición se cierra después de mantenerla durante `HoldingMinutes` minutos.
   - **Niveles de protección** – `StopLossPoints` y `TakeProfitPoints` se expresan en pasos de precio. Se evalúan en cada vela finalizada comparando el máximo/mínimo de sesión con el precio de entrada.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Cantidad usada para entradas de mercado. |
| `StopLossPoints` | Distancia del stop de protección en pasos de precio. Establecer en `0` para deshabilitar. |
| `TakeProfitPoints` | Distancia del objetivo de ganancia en pasos de precio. Establecer en `0` para deshabilitar. |
| `EnableBuyEntries` / `EnableSellEntries` | Habilitar o deshabilitar entradas largas/cortas. |
| `EnableBuyExits` / `EnableSellExits` | Habilitar o deshabilitar salidas basadas en pendiente. |
| `UseTimeExit` | Habilita la lógica de salida temporizada. |
| `HoldingMinutes` | Período de tenencia en minutos cuando el exit temporizado está activo. |
| `CandleType` | Marco temporal usado para cálculos (por defecto 4 horas). |
| `JmaLength` | Longitud de suavizado de la Media Móvil Jurik aplicada a la salida FATL. |
| `AppliedPrices` | Fuente de precio para el filtro digital (cierre, apertura, mediana, Demark, etc.). |
| `RoundingDigits` | Número de dígitos usados al redondear la línea suavizada. |
| `SignalBar` | Offset de la barra terminada usada para evaluar el estado del indicador. |

## Notas

- La estrategia procesa solo velas completamente finalizadas y por lo tanto funciona bien con backtests históricos.
- `AppliedPrices.Demark` reproduce el mismo cálculo que el indicador MQL original.
- Debido a que StockSharp maneja la ejecución de órdenes de forma asíncrona, el seguimiento interno del precio de entrada se actualiza cada vez que se abre una nueva posición y se borra cada vez que se envía una orden de salida.
