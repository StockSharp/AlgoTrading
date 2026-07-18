# Estrategia SMC Trader Camel CCI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una versión StockSharp del MetaTrader 4 asesor experto **"Steve Cartwright Trader Camel CCI MACD"**.
Reproduce la lógica comercial original basada en un canal de media móvil exponencial estilo camello,
un filtro de tendencias MACD y umbrales del índice de canales de productos básicos (CCI). Las operaciones se ejecutan al finalizar
velas para garantizar un comportamiento determinista y mantenerse cerca del flujo de trabajo barra por barra de la versión MQL.

## Lógica de trading

1. **Indicadores**
   - Se aplican dos medias móviles exponenciales (EMA) con el mismo período a los máximos y mínimos de las velas para formar el
canal de camellos. Una ruptura del cierre anterior más allá de estos límites indica fuerza del impulso.
   - Se utiliza un indicador MACD estándar (EMA rápido, EMA lento y línea de señal) para confirmar la dirección de la tendencia subyacente.
   - Un indicador CCI valida la fuerza del impulso utilizando niveles de sobrecompra/sobreventa en ±100 de forma predeterminada.
2. **Entradas largas**
   - El cierre de la vela anterior está por encima del máximo del camello EMA.
   - El valor principal anterior MACD está por encima de cero **y** por encima de la línea de señal.
   - El valor CCI anterior está por encima del umbral positivo.
   - No hay ninguna posición activa abierta y no se produjo ninguna salida dentro del período de tiempo de la vela actual (evita un reingreso rápido).
3. **Entradas cortas**
   - El cierre de la vela anterior está por debajo del mínimo del camello EMA.
   - El valor principal anterior MACD está por debajo de cero **y** por debajo de la línea de señal.
   - El valor CCI anterior está por debajo del umbral negativo.
   - Mismas condiciones de posición plana y enfriamiento que para configuraciones largas.
4. **Sale**
   - Las posiciones largas se cierran cuando el valor principal MACD anterior cruza por debajo de la línea de señal o cuando el valor principal CCI anterior
el valor cae por debajo del umbral positivo.
   - Las posiciones cortas se cierran cuando el valor principal MACD anterior cruza por encima de la línea de señal.
   - Después de cualquier salida, se aplica un tiempo de reutilización igual a la duración de una vela antes de nuevas entradas.

La estrategia se negocia una vez por barra como máximo porque cada decisión se basa en datos de la vela completada anteriormente.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Tipo de datos de vela/período de tiempo utilizado para todos los indicadores. | plazo de 1 hora |
| `CamelLength` | Longitud del canal alto/bajo EMA. | 34 |
| `CciPeriod` | Longitud del filtro CCI. | 20 |
| `MacdFastPeriod` | Longitud rápida de EMA para MACD. | 12 |
| `MacdSlowPeriod` | Longitud lenta de EMA para MACD. | 26 |
| `MacdSignalPeriod` | Período de suavizado de señal para MACD. | 9 |
| `CciThreshold` | Nivel absoluto CCI que debe superarse para las entradas (aplicado simétricamente). | 100 |

Todos los parámetros son optimizables a través del optimizador StockSharp gracias a las llamadas `SetOptimize`.

## Gestión del riesgo

- Las órdenes se envían a través de `BuyMarket` y `SellMarket`, heredando la propiedad de estrategia `Volume`.
- `StartProtection()` está habilitado para inicializar los asistentes de protección estándar StockSharp.
- En el algoritmo original no se define ningún stop-loss ni take-profit fijo; las salidas dependen únicamente de las señales de los indicadores.

## Trazar

La estrategia traza automáticamente los indicadores del canal camel EMA, MACD y CCI, junto con sus propias operaciones.
que replica las señales visuales utilizadas en la implementación de MT4.

## Notas

- El temporizador de enfriamiento utiliza la duración de la vela derivada de `CandleType.Arg`. Asegúrese de que `CandleType` contenga un
`TimeSpan` argumento cuando cambia el período de tiempo.
- Debido a que todas las decisiones se basan en los valores de la barra anterior, el orden de las operaciones refleja `iMACD`, `iCCI`
y `iMA` (con shift=1) llama en la fuente EA.
