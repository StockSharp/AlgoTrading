# Estrategia de Rompimiento de Niveles de Vela Anterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el asesor experto de MetaTrader "Previous Candle Breakdown". Espera a que el precio rompa por encima o por debajo de la vela de referencia anterior con una sangría configurable medida en pasos de precio. La implementación se basa en las APIs de alto nivel de StockSharp con suscripciones de velas para cálculos de niveles y suscripciones de ticks para decisiones de ejecución.

## Lógica de trading
1. Al cierre de cada vela de referencia (4 horas por defecto), la estrategia almacena el máximo y mínimo de la vela anterior y los desplaza por `IndentSteps * Security.PriceStep` para construir niveles de ruptura.
2. Se monitorizan los precios de tick (últimas operaciones). Una entrada larga se activa cuando el precio alcanza el nivel superior y una entrada corta cuando el precio cae por el nivel inferior.
3. Un filtro de media móvil opcional requiere que la MA rápida (con desplazamiento opcional hacia adelante) se mantenga por encima de la MA lenta para las operaciones largas y por debajo para las cortas. Establecer cualquier período de MA a cero deshabilita el filtro.
4. Las operaciones solo se permiten dentro de la ventana de sesión configurada entre `StartTime` y `EndTime`. Se admiten sesiones que cruzan la medianoche.
5. La ganancia flotante se monitorea continuamente: los stops, objetivos y reglas de trailing cierran posiciones existentes antes de que una señal de ruptura pueda activar reversiones.

## Gestión de riesgo
- **StopLossSteps / TakeProfitSteps** — distancias en pasos de precio desde el precio de entrada. Los pasos se convierten mediante `distance = steps * Security.PriceStep`.
- **TrailingStopSteps / TrailingStepSteps** — habilita una salida de trailing una vez que la posición se mueve a favor al menos la distancia de trailing. El stop se mueve más solo cuando la ganancia avanza por el paso de trailing.
- **ProfitClose** — cierra todas las posiciones una vez que la ganancia no realizada (`Position * (último precio - PositionPrice)`) supera el umbral. Establecer a `0` para deshabilitar.
- **MaxNetPosition** — limita la posición neta absoluta para que la estrategia no pueda piramidarse más allá de esa cantidad. El tamaño de la posición en sí se controla por la propiedad `Volume` de la estrategia.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco temporal de referencia usado para calcular los niveles de ruptura. |
| `IndentSteps` | Desplazamiento por encima/debajo del máximo/mínimo de la vela anterior expresado en pasos de precio. |
| `FastMaPeriod` / `FastMaShift` | Longitud de la media móvil rápida y desplazamiento hacia adelante opcional (barras). |
| `SlowMaPeriod` / `SlowMaShift` | Longitud de la media móvil lenta y desplazamiento hacia adelante opcional (barras). |
| `StopLossSteps` | Distancia de stop loss en pasos de precio. |
| `TakeProfitSteps` | Distancia de take profit en pasos de precio. |
| `TrailingStopSteps` | Distancia del trailing stop (0 deshabilita el trailing). |
| `TrailingStepSteps` | Ganancia mínima requerida antes de que el trailing stop avance. Debe ser > 0 cuando se use trailing. |
| `ProfitClose` | Objetivo de ganancia flotante que cierra todas las posiciones. |
| `MaxNetPosition` | Posición neta absoluta máxima permitida. |
| `StartTime` / `EndTime` | Límites de la ventana de trading. |

## Notas de uso
- Establezca la propiedad `Volume` de la instancia de estrategia para controlar el tamaño de la orden. El dimensionamiento de posición basado en riesgo de la versión de MetaTrader no está portado intencionalmente.
- Las medias móviles usan medias móviles simples (`SMA`). Si se requieren otros modos de suavizado, extienda la estrategia en consecuencia.
- El umbral de cierre por ganancia usa la ganancia no realizada en unidades de precio del instrumento (cantidad × diferencia de precio). Ajuste el umbral para que coincida con su instrumento.
- La estrategia opera en un entorno de netting; las operaciones de reversión envían órdenes de mercado en la dirección opuesta, cerrando automáticamente primero la exposición actual.
- El trailing stop requiere un valor positivo de `TrailingStepSteps`; de lo contrario, la estrategia lanza una excepción durante el inicio.

## Diferencias con la versión MQL original
- La gestión de dinero basada en lotes fijos o porcentaje de riesgo no está implementada; los usuarios de StockSharp deben gestionar el tamaño a través de la propiedad `Volume` o gestores de portafolio externos.
- Solo se admiten medias móviles simples; el original permitía diferentes tipos de MA.
- La lógica de cierre por ganancia usa el PnL flotante calculado desde el precio promedio de posición en lugar de moneda de cuenta, porque los datos específicos de swap/comisión del bróker no están disponibles directamente.
- El registro es manejado por StockSharp; los mensajes detallados de resultado de operaciones de MetaTrader se omiten.
