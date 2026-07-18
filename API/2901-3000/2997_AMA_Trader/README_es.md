# Estrategia AMA Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia AMA Trader replica el comportamiento del experto original de MetaTrader 5 "AMA Trader". Combina la Media Móvil Adaptativa de Kaufman (AMA) con el Índice de Fuerza Relativa (RSI) para promediar en operaciones contra retrocesos de corto plazo mientras el precio permanece en el lado prevaleciente del filtro de tendencia adaptativa. La implementación de StockSharp usa la API de alto nivel con suscripciones de velas y vinculación de indicadores para permanecer cerca de la lógica original mientras mantiene compatibilidad total con el modelo de ejecución de StockSharp.

## Supuestos de Mercado
- **Tipo de instrumento**: diseñado para FX spot o CFD, pero aplicable a cualquier instrumento de tendencia que soporte promediación.
- **Marco temporal**: velas de un minuto por defecto, configurables a través del parámetro `CandleType`.
- **Sesiones**: sin manejo de sesión explícito. Las señales se evalúan en cada vela terminada.

## Indicadores
1. **Media Móvil Adaptativa de Kaufman (AMA)**
   - Suaviza la acción del precio con parámetros para las constantes de suavizado rápido y lento (`AmaFastPeriod`, `AmaSlowPeriod`) y la longitud de promediado (`AmaLength`).
   - Define la dirección principal de la tendencia. Las operaciones largas solo se consideran cuando el precio de cierre está por encima de la AMA; las operaciones cortas solo cuando está por debajo.
2. **Índice de Fuerza Relativa (RSI)**
   - Evaluado con período `RsiLength` en el cierre de la vela.
   - `StepLength` controla cuántos valores recientes de RSI deben confirmar un estado de sobrecompra/sobreventa. Un valor de 0 revierte a verificar solo la última barra, imitando la implementación MQL donde `StepLength == 0` se trata como 1.
   - `RsiLevelDown` (predeterminado 30) y `RsiLevelUp` (predeterminado 70) definen los umbrales de sobreventa y sobrecompra respectivamente.

## Lógica de Trading
1. **Validación de barra**
   - Las operaciones se evalúan solo en velas terminadas y cuando la estrategia está en línea y se le permite operar.
2. **Gestión de beneficios antes de nuevas entradas**
   - Si el beneficio no realizado de todas las posiciones abiertas supera `ProfitTarget`, la estrategia cierra cada posición abierta y espera la próxima señal.
   - Si el beneficio realizado desde el último reinicio crece más de `WithdrawalAmount`, todas las posiciones se cierran y el punto de control del beneficio realizado se actualiza. Esto imita la mecánica de retiro del experto original (no se elimina efectivo real; solo se reinicia el punto de control).
3. **Entradas largas**
   - Condición: precio de cierre > AMA y al menos uno de los valores de RSI inspeccionados está por debajo de `RsiLevelDown`.
   - Acción: enviar una orden de compra a mercado. Si la exposición larga actual pierde dinero (PnL no realizado negativo basado en el precio de entrada promedio rastreado), se envía una orden de compra de promediado adicional.
4. **Entradas cortas**
   - Condición: precio de cierre < AMA y al menos uno de los valores de RSI inspeccionados está por encima de `RsiLevelUp`.
   - Acción: enviar una orden de venta a mercado. Si la exposición corta actual pierde, se envía una orden de venta de promediado adicional.
5. **Seguimiento de posición**
   - Las ejecuciones se procesan en `OnOwnTradeReceived`. Se rastrean precios promedio y volúmenes separados para la exposición larga y corta, lo que permite estimaciones precisas de PnL no realizado incluso cuando el mercado alterna entre compras y ventas.

## Gestión de Riesgos
- **Volumen de promediado**: cada entrada usa el `LotSize` fijo. Cuando ocurren pérdidas, el algoritmo duplica añadiendo una orden extra en la misma dirección.
- **Objetivo de beneficio no realizado**: `ProfitTarget` (predeterminado 50 unidades monetarias) fuerza una salida completa cuando los beneficios flotantes alcanzan el nivel especificado.
- **Punto de control de beneficio realizado**: `WithdrawalAmount` (predeterminado 1000) cierra todas las posiciones una vez que el PnL realizado acumulado supera el umbral, tras lo cual el punto de control se reinicia al PnL realizado actual.
- **Protección manual**: no se configura stop-loss o take-profit automático más allá del objetivo de beneficio no realizado. Los usuarios pueden habilitar controles de riesgo externos si es necesario.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Tipo de datos de vela o marco temporal para cálculos de indicadores. |
| `LotSize` | Volumen fijo para cada orden de mercado. |
| `RsiLength` | Período de promediado RSI. |
| `StepLength` | Número de valores recientes de RSI examinados (0 revierte a 1). |
| `RsiLevelUp` | Umbral de sobrecompra del RSI para señales cortas. |
| `RsiLevelDown` | Umbral de sobreventa del RSI para señales largas. |
| `AmaLength` | Período de suavizado AMA. |
| `AmaFastPeriod` | Constante de suavizado rápido AMA. |
| `AmaSlowPeriod` | Constante de suavizado lento AMA. |
| `ProfitTarget` | Beneficio no realizado requerido para aplanar todas las posiciones (0 deshabilita la regla). |
| `WithdrawalAmount` | Incremento de beneficio realizado que activa una salida completa (0 deshabilita la regla). |

## Notas de Implementación
- Uso de API de alto nivel: las velas se suscriben a través de `SubscribeCandles`, y AMA/RSI se vinculan a la suscripción mediante `.Bind`. El delegado de procesamiento recibe valores decimales sin procesar, evitando el acceso manual a valores del indicador.
- El monitoreo de posición depende de acumuladores privados actualizados dentro de `OnOwnTradeReceived`. Esto refleja la inspección de posiciones del experto MQL sin recurrir a getters agregados prohibidos.
- Las órdenes se envían con `BuyMarket` y `SellMarket`, usando el `LotSize` actual. El aplanamiento usa argumentos de volumen explícitos para que tanto la exposición larga como la corta se puedan limpiar.
- La versión de StockSharp usa el precio de cierre de la vela en lugar de la comprobación de ask/bid de MetaTrader al evaluar la relación AMA, que es la información más cercana disponible dentro de un flujo de trabajo basado en velas.

## Diferencias del Experto de MetaTrader
- `WithdrawalAmount` actualiza un punto de control interno en lugar de llamar a `TesterWithdrawal`, porque el backtester de StockSharp no admite retiros sintéticos.
- Las opciones de desplazamiento de AMA y precio aplicado del EA original no están expuestas. Los indicadores de StockSharp operan con precios de cierre de velas.
- Las comisiones y swaps no se añaden explícitamente al cálculo del PnL no realizado; el entorno de ejecución de StockSharp maneja las comisiones internamente cuando se liquidan las operaciones.

## Consejos de Uso
- Considera combinar la estrategia con límites de riesgo a nivel de portafolio o el módulo de protección integrado si el promediado es demasiado agresivo para el instrumento negociado.
- Optimiza los ajustes de AMA y RSI por instrumento. Los marcos temporales más bajos a menudo se benefician de períodos AMA más cortos y umbrales RSI más amplios.
- Monitorea las reducciones cuando `StepLength` > 1, ya que el promediado puede ejecutarse varias veces durante movimientos fuertes en contra de la tendencia.
