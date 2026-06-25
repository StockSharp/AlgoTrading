# Estrategia Binario
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Binario es un sistema de ruptura de entrada con stop que rodea al precio con dos envolventes de media móvil calculadas sobre los máximos y mínimos de las velas. Cuando el precio opera entre las envolventes, la estrategia coloca órdenes de stop simétricas para capturar la siguiente expansión direccional. Las órdenes heredan offsets fijos de stop-loss y take-profit que reflejan el asesor experto de MetaTrader 5.

El port a StockSharp mantiene la idea central aprovechando características de la API de alto nivel como suscripciones de velas, vinculación de indicadores y gestión automatizada de órdenes. Se consumen datos de Nivel-1 para estimar el spread bid/ask actual, necesario para reproducir los offsets de entrada originales.

## Lógica de operación
1. Construir dos medias móviles (superior en máximos, inferior en mínimos) usando métodos y período configurables.
2. Cuando el último cierre está entre las medias:
   - Colocar un buy-stop por encima de la media superior más el buffer de diferencia configurado y el spread actual.
   - Colocar un sell-stop por debajo de la media inferior menos el mismo buffer.
3. Cada orden pendiente almacena sus propios niveles de stop-loss y take-profit derivados de las medias móviles, `PointValue` y parámetros basados en pips.
4. Cuando se ejecuta una orden, se cancela la orden pendiente opuesta y se registran nuevas órdenes de protección (stop-loss y take-profit) para la posición abierta.
5. La lógica de stop de seguimiento ajusta el stop cuando el precio avanza al menos `TrailingStopPips + TrailingStepPips` desde el precio de entrada, coincidiendo con el comportamiento incremental de la implementación MQL.
6. Cuando la posición cambia de larga a corta (o viceversa), las órdenes de protección existentes se cancelan para evitar conflictos.

## Parámetros
- `CandleType` – marco temporal usado para los cálculos.
- `MaPeriod` – longitud de ambas medias móviles.
- `MaShift` – desplazamiento de barra aplicado a cada media móvil (0 reproduce el comportamiento por defecto del EA).
- `HighMaMethod` / `LowMaMethod` – métodos de suavizado (`SMA`, `EMA`, `SMMA`, `WMA`, `LWMA`).
- `PointValue` – valor de precio absoluto que representa un pip para el símbolo operado (0.0001 para la mayoría de los pares FX, 0.01 para pares JPY, etc.).
- `DifferencePips` – buffer entre las medias y las órdenes pendientes, expresado en pips.
- `TakeProfitPips` – distancia del objetivo de beneficio en pips.
- `TrailingStopPips` – distancia del stop de seguimiento en pips (establecer en cero para deshabilitar el seguimiento).
- `TrailingStepPips` – ganancia adicional mínima en pips requerida antes de ajustar el stop nuevamente.
- `Volume` (heredado de `Strategy`) – tamaño de orden base; las órdenes de reversión añaden automáticamente el tamaño absoluto de la posición para cambiar completamente la exposición.

Todos los parámetros basados en pips se traducen a precios absolutos via `PointValue`, reflejando la conversión `Point * digits_adjust` realizada en la versión MT5.

## Gestión de órdenes
- Las órdenes de stop pendientes permanecen activas solo mientras la estrategia está plana en su lado respectivo (sin posición larga para un nuevo buy-stop, sin posición corta para un nuevo sell-stop).
- Después de que se activa una entrada, la estrategia envía órdenes de stop-loss y take-profit coincidentes y elimina el stop-entry opuesto no utilizado.
- Las reversiones de posición cancelan las órdenes de protección existentes antes de registrar nuevas, previniendo stops huérfanos.

## Comportamiento de seguimiento
- Posiciones largas: una vez que el precio gana al menos `TrailingStopPips + TrailingStepPips` pips, el stop se mueve a `close - TrailingStopPips` siempre que el movimiento supere el stop anterior en al menos `TrailingStepPips`.
- Posiciones cortas: cuando el precio cae por el mismo umbral, el stop se baja a `close + TrailingStopPips`, también respetando el filtro de paso.
- El seguimiento usa el cierre de la vela más reciente como sustituto del valor `PriceCurrent()` de MT5.

## Requisitos de datos
- Velas del `CandleType` seleccionado.
- Cotizaciones de Nivel-1 para recuperar los mejores precios bid/ask y calcular el spread. Cuando el spread no está disponible, la estrategia recurre al paso de precio mínimo del instrumento o `PointValue`.

## Diferencias con la versión MetaTrader 5
- El dimensionamiento de posición se controla a través de la propiedad `Volume` de StockSharp en lugar de la combinación Lots/Risk original.
- Las órdenes de protección se recrean cuando el seguimiento modifica los precios porque las órdenes de stop de StockSharp no pueden modificarse en su lugar.
- Los precios de ejecución informados por MyTrades se aproximan por los precios de órdenes almacenados; ajuste `PointValue` y los parámetros de pips para que coincidan con las especificaciones del broker.
- La estrategia se ejecuta en velas terminadas, equivalente a habilitar "experto en cada tick" con evaluación de apertura de barra en el script MT5.

## Notas de uso
1. Establezca `PointValue` según la relación tick-a-pip del instrumento.
2. Configure los métodos y el período de media móvil para que coincidan con su plantilla MT5.
3. Elija distancias de pip adecuadas para los componentes de diferencia, take-profit y seguimiento.
4. Asegúrese de que los datos de Nivel-1 estén disponibles para que el componente de spread pueda aplicarse con precisión.
