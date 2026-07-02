# Negociación en red en un mercado volátil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el MetaTrader experto "Gridtrading_at_volatile_market.mq4" utilizando la API de alto nivel de StockSharp. Opera alrededor de Donchian límites de canal detectados en un período de tiempo más alto mientras confirma entradas con patrones envolventes en el período de tiempo de negociación. Una vez que una cuadrícula está activa, la estrategia agrega órdenes promedio cuando el precio se extiende en múltiplos del período de tiempo más alto ATR y sale cuando se alcanzan los objetivos de ganancias o reducción de la cartera.

## Cómo funciona
1. Se utilizan dos flujos de velas: el marco temporal de negociación seleccionado por el usuario y un marco temporal superior derivado automáticamente de él (M1→M5→M15→M30→H1→H4→D1).
2. En el marco de tiempo más alto, la estrategia calcula:
   - `ATR(20)` para dimensionar el espacio de la cuadrícula.
   - `SMA(SlowMaLength)` para filtrar la tendencia junto con RSI.
   - `DonchianChannels(20)` para niveles de soporte y resistencia.
3. En el período de negociación, rastrea las dos últimas velas completadas para detectar patrones envolventes alcistas o bajistas.
4. Una grilla larga comienza cuando la vela anterior toca la banda inferior Donchian, forma un patrón envolvente alcista y RSI confirma condiciones de sobreventa (`RSI < 35` mientras el precio está por encima del marco temporal superior SMA). Una cuadrícula corta refleja estas reglas en la banda superior con `RSI > 65`.
5. Después de la primera orden de mercado, la estrategia mantiene el precio inicial como ancla. Si el precio se mueve contra la posición en `2 * ATR` para el paso actual de la cuadrícula, agrega otra orden con el volumen multiplicado por `GridMultiplier`.
6. La grilla se cierra y todos los pedidos se cancelan cuando:
   - El PnL combinado (realizado + no realizado) supera `TakeProfitFactor * total grid volume`.
   - La reducción cae por debajo de `-MaxDrawdownFraction * initial portfolio value`.

## Parámetros
- **TakeProfitFactor**: múltiplo de beneficio del volumen total de la red requerido para cerrar la red (predeterminado `0.1`).
- **SlowMaLength**: período del período de tiempo más alto SMA utilizado para el filtrado (predeterminado `50`).
- **GridMultiplier**: factor geométrico aplicado a cada orden de promedio adicional (predeterminado `1.5`).
- **BaseOrderVolume**: volumen del primer pedido en la cuadrícula (predeterminado `0.1`).
- **MaxDrawdownFraction**: pérdida máxima relativa al valor inicial de la cartera antes de que se cierre forzosamente la red (predeterminado `0.8`).
- **CandleType** – plazo de negociación. El plazo más alto se deduce automáticamente.

## Notas
- Sólo se procesan velas cerradas para evitar repintar.
- La estrategia se basa en las cotizaciones de oferta y demanda disponibles para evaluar la PnL abierta; si sólo se proporcionan los últimos precios comerciales, la aproximación puede ser menos precisa.
- Cuando la información de la cartera no está disponible, se omite la protección de reducción, lo que permite que la red funcione hasta que se alcance el objetivo de ganancias o se cierre la posición manualmente.
