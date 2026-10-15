# Estrategia Expert ZZLWA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port de alto nivel de StockSharp del asesor experto original **ExpertZZLWA** de MetaTrader 5. El EA ofrecía tres modos de operación distintos y un dimensionamiento de posición martingala opcional. El port mantiene la estructura del experto original adaptándola a velas e indicadores de StockSharp:

1. **Modo Original** – alterna entre operaciones largas y cortas en cada barra completada siempre que no haya posición abierta.
2. **Modo ZigZag Addition** – recrea el comportamiento del indicador personalizado "ZigZag LW Addition" rastreando nuevos máximos y mínimos oscilantes mediante valores máximos/mínimos móviles.
3. **Modo Moving Average Test** – replica la lógica de cruce de MA suavizada (150) vs MA simple (10) del código MQL.

Todos los modos usan offsets de stop loss y take profit de protección configurables expresados en puntos de precio. La estrategia soporta dimensionamiento martingala opcional donde una nueva operación se incrementa por un multiplicador tras una pérdida realizada, limitada por un volumen máximo.

## Lógica de trading

### Modo Original

- Trabaja solo con velas finalizadas.
- Cuando no hay posición abierta, la estrategia alterna entre órdenes de mercado largas y cortas en cada nueva barra.
- El stop loss y take profit se registran a través del helper integrado `StartProtection`.
- Una vez que una operación cierra (ya sea en stop o en objetivo), la dirección opuesta se activa para la siguiente barra.

### Modo ZigZag Addition

- Se suscribe a la serie de velas seleccionada y mantiene indicadores `Highest` y `Lowest` móviles.
- Detecta un máximo oscilante cuando el máximo de la vela toca el valor más alto actual mientras la dirección oscilante anterior no era alcista. Esto recrea las señales de buffer de compra/venta de "ZigZag LW Addition".
- Detecta un mínimo oscilante cuando el mínimo de la vela toca el valor más bajo móvil de manera opuesta.
- Genera una orden de mercado en la dirección señalada inmediatamente después del cierre de la vela.

### Modo Moving Average Test

- Construye una media móvil suavizada con longitud 150 y una media móvil simple con longitud 10 (igual que la implementación MQL).
- Produce una señal larga cuando la MA suavizada cruza por encima de la MA simple de la barra anterior a la barra actual.
- Produce una señal corta cuando la MA suavizada cruza por debajo de la MA simple.
- Las señales se procesan solo en velas cerradas.

### Manejo del Martingala

- Tras recibir cada operación propia, la estrategia rastrea la posición neta y el precio de entrada promedio.
- Cuando una posición se cierra completamente, se registra el beneficio realizado de la última operación.
- Si la operación cerró con pérdida y el martingala está habilitado, el volumen del siguiente orden se convierte en `último_volumen * MartingaleMultiplier` (limitado por `MaximumVolume`).
- Si la operación cerró con beneficio o el martingala está deshabilitado, la estrategia vuelve al volumen base.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `StopLossPoints` | 600 | Distancia al stop de protección en puntos de precio. |
| `TakeProfitPoints` | 700 | Distancia al take profit en puntos de precio. |
| `BaseVolume` | 0.01 | Tamaño de orden predeterminado cuando no se aplica martingala. |
| `UseMartingale` | false | Habilita el dimensionamiento martingala cuando se establece en true. |
| `MartingaleMultiplier` | 2 | Multiplicador aplicado al último volumen de operación tras una pérdida. |
| `MaximumVolume` | 10 | Volumen máximo permitido para el dimensionamiento martingala. |
| `Mode` | Original | Modo de operación: `Original`, `ZigZagAddition` o `MovingAverageTest`. |
| `ZigZagTerm` | LongTerm | Preset de sensibilidad para el modo ZigZag (ShortTerm, MediumTerm, LongTerm). |
| `SlowMaPeriod` | 150 | Período de la MA suavizada usada en el modo MA Test. |
| `FastMaPeriod` | 10 | Período de la MA simple usada en el modo MA Test. |
| `CandleType` | Marco temporal de 15 minutos | Tipo de vela suscrita para procesamiento. |

## Notas

- Los offsets de stop/take se multiplican por el `PriceStep` del instrumento, coincidiendo con el comportamiento de `_Point` de MetaTrader.
- La estrategia usa exclusivamente la API de alto nivel de StockSharp (`SubscribeCandles` + vinculación de indicadores).
- Los presets de sensibilidad ZigZag se corresponden con longitudes de Highest/Lowest de 12 (Corto), 24 (Medio) y 48 (Largo). Ajústelos si se requiere una amplitud de oscilación diferente.
- El rastreador martingala depende de las notificaciones de operaciones propias; asegúrese de que la estrategia se ejecute en un entorno donde los fills se informan correctamente.

## Diferencias de conversión vs MQL

- La versión MQL interactuaba con un indicador compilado `ZigZag LW Addition`. En StockSharp aproximamos los buffers usando máximos/mínimos móviles, lo que entrega señales similares sin binarios externos.
- La colocación de órdenes se basa en `BuyMarket` / `SellMarket` y el helper de protección gestionado en lugar de tickets de órdenes manuales.
- El cálculo histórico de lotes en el experto original usaba el historial de deals del terminal. El port replica este comportamiento analizando operaciones propias en tiempo real y almacenando el último volumen de operación cerrado y el beneficio.
- Las entradas de deslizamiento y número mágico de MQL se omiten porque StockSharp no las necesita para órdenes de mercado en este contexto.
