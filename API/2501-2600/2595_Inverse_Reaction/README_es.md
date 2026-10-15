# Estrategia de Reacción Inversa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Reacción Inversa es un sistema de reversión a la media inspirado en el asesor experto original de MetaTrader "IREA". Reacciona a movimientos de barra única inusualmente grandes y anticipa una reacción inversa en la siguiente barra. La estrategia calcula un nivel de confianza dinámico a partir de rangos de velas recientes y solo opera cuando los movimientos de precio superan ese nivel pero permanecen dentro de los límites definidos por el usuario. Solo puede estar abierta una posición a la vez.

## Lógica de trading
1. **Indicador de Reacción Inversa** – Para cada vela completada la estrategia mide el cambio apertura/cierre y alimenta su valor absoluto a una media móvil simple de longitud `MaPeriod`. El cambio promediado se multiplica por `Coefficient` para formar un umbral dinámico similar al Nivel de Confianza Dinámico (DCL) del indicador original.
2. **Validación de señal** – El cambio absoluto apertura/cierre de la última vela debe ser mayor que el umbral dinámico, mayor que `MinCriteriaPoints * PriceStep`, y menor que `MaxCriteriaPoints * PriceStep`. Las señales se ignoran si la vela anterior ya cumplió la misma condición, lo cual refleja el asesor experto original.
3. **Dirección** – Un cambio negativo (vela bajista) sugiere un rebote al alza, por lo que se abre una posición larga. Un cambio positivo implica una expectativa de reversión bajista y desencadena una posición corta. Las nuevas operaciones se envían solo cuando no hay posición existente.
4. **Gestión de riesgos** – Después de la entrada, la estrategia monitorea las velas subsiguientes. Si el precio toca los niveles predefinidos de stop-loss o take-profit (convertidos de puntos a precios absolutos usando el `PriceStep` del instrumento), cierra inmediatamente la posición abierta usando órdenes de mercado. `StartProtection()` también se habilita para soportar las protecciones integradas de StockSharp.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `StopLossPoints` | Distancia de stop-loss en puntos (multiplicado por `PriceStep`). |
| `TakeProfitPoints` | Distancia de take-profit en puntos. |
| `TradeVolume` | Volumen usado para cada orden de mercado. |
| `SlippagePoints` | Configuración informativa que refleja la versión MQL; actualmente no se aplica a las órdenes. |
| `MinCriteriaPoints` | Distancia mínima apertura/cierre (en puntos) requerida para una señal válida. |
| `MaxCriteriaPoints` | Distancia máxima apertura/cierre permitida (en puntos). |
| `Coefficient` | Multiplicador usado para construir el umbral de confianza dinámico. |
| `MaPeriod` | Longitud de la media móvil usada dentro del indicador. Debe ser al menos 3. |
| `CandleType` | Marco temporal de las velas procesadas (por defecto: 1 hora). |

## Directrices de uso
- Asegúrese de que el instrumento seleccionado tenga un `PriceStep` válido. Cuando no está disponible, la estrategia recurre a un paso de 1.0, lo que puede distorsionar los umbrales.
- Ajuste `MinCriteriaPoints` y `MaxCriteriaPoints` para adaptarlos a la volatilidad del marco temporal elegido. Una ventana demasiado estrecha filtrará la mayoría de las señales, mientras que una ventana demasiado amplia permitirá movimientos extremadamente grandes que pueden no revertir.
- El `Coefficient` predeterminado de 1.618 replica el escalado de ratio áureo del indicador original. Los valores más altos demandan velas de mayor amplitud antes de operar.
- Dado que las posiciones se cierran por órdenes de mercado en el próximo cierre de vela que viola los niveles de stop o target, la ejecución real puede diferir de los niveles límite exactos. Considere probar con datos intradía para un control más preciso si es necesario.
- Solo se mantiene una posición a la vez. La estrategia esperará que la operación actual se cierre antes de reaccionar a una nueva señal.

## Notas
- Realice backtesting de la configuración en datos históricos antes de usarlo en vivo. El EA original fue diseñado para mercados FX; puede requerirse ajuste de parámetros para otros activos.
- El parámetro `SlippagePoints` se conserva para completitud pero intencionalmente no se usa porque StockSharp maneja el deslizamiento de forma diferente a MetaTrader.
- Asegúrese de que `MaPeriod` se mantenga en 3 o más; los valores más pequeños estaban prohibidos en la implementación original y pueden llevar a umbrales inestables.
