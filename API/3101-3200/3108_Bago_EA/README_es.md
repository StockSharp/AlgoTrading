# Estrategia Bago EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el asesor experto de MetaTrader "Bago EA". Opera con rupturas de seguimiento de tendencia confirmadas
por cruces de media móvil y RSI, mientras que el túnel Vegas (par de EMA 144/169) proporciona filtros espaciales y anclas
de trailing.

## Lógica de Trading

1. **Preparación de indicadores**
   - Dos EMAs (períodos `FastPeriod` y `SlowPeriod`, método `MaMethod`, precio `MaAppliedPrice`).
   - EMAs del túnel Vegas (períodos 144 y 169, mismo método/precio) para detectar el canal direccional.
   - RSI (`RsiPeriod`, `RsiAppliedPrice`) para confirmación.
   - Todas las conversiones precio-a-pip usan el `PriceStep` del instrumento con ajuste de 3/5 dígitos como el EA original.
2. **Máquina de estados de cruce**
   - El cruce de EMA arriba/abajo y el cruce de RSI por encima/debajo de 50 se rastrean con temporizadores. Cada estado
     permanece activo durante `CrossEffectiveBars` velas y se reinicia por el cruce opuesto o el tiempo de espera.
   - Los cruces del túnel marcan cuando el precio pasa de un lado del túnel Vegas al otro.
3. **Condiciones de entrada**
   - **Largo**: tanto el cruce de EMA como el de RSI están activos hacia arriba *y* el precio:
     - Cierra por encima del túnel al menos `TunnelBandWidthPips` pero no más de `TunnelSafeZonePips`, con cuerpo de vela
       alcista, o
     - Cierra por debajo del túnel en `TunnelBandWidthPips`, señalando un rebote desde abajo.
   - **Corto**: lógica espejo con cruces de EMA/RSI a la baja.
   - El trading solo se permite dentro de las sesiones habilitadas (Londres 07–16, Nueva York 12–21, Tokio 00–08, o cualquier
     barra que cierre después de las 23:00).
4. **Gestión de órdenes**
   - Las nuevas posiciones se abren con volumen `TradeVolume`. Las posiciones opuestas se cierran antes de revertir.
   - El stop inicial se establece en `StopLossPips` desde el precio de cierre. Los desplazamientos stop-a-túnel usan
     `StopLossToFiboPips`.
5. **Trailing y salidas parciales**
   - A medida que el precio avanza más allá de los niveles del túnel Vegas, el stop se mueve:
     - Dentro del túnel, el stop se sitúa en `tunnel ± (TrailingStepX + StopLossToFibo)`.
     - Fuera del túnel, se aplica un trailing fijo de `TrailingStopPips` detrás del precio.
   - Las salidas parciales cierran `PartialClose1Volume` en `TrailingStep1Pips` y `PartialClose2Volume` en `TrailingStep2Pips`
     una vez que el precio ha viajado lo suficiente desde la entrada.
   - Un cruce opuesto de EMA/RSI cierra la posición completa inmediatamente.
6. **Stops**
   - Las órdenes protectoras se mantienen como órdenes de stop de mercado. Se cancelan cada vez que se cierra la posición.

## Parámetros

| Parámetro | Tipo | Valor predeterminado | Descripción |
|-----------|------|----------------------|-------------|
| `TradeVolume` | decimal | 3 | Tamaño de orden en lotes. |
| `StopLossPips` | decimal | 30 | Distancia inicial de stop-loss. |
| `StopLossToFiboPips` | decimal | 20 | Buffer adicional al aparcar stops alrededor del túnel Vegas. |
| `TrailingStopPips` | decimal | 30 | Distancia del trailing stop cuando el precio sale del túnel. |
| `TrailingStep1Pips` | decimal | 55 | Primera capa de beneficio para salida parcial y reubicación de stop. |
| `TrailingStep2Pips` | decimal | 89 | Segunda capa de beneficio para salida parcial y trailing. |
| `TrailingStep3Pips` | decimal | 144 | Capa final antes de usar trailing puro. |
| `PartialClose1Volume` | decimal | 1 | Volumen cerrado en `TrailingStep1Pips`. |
| `PartialClose2Volume` | decimal | 1 | Volumen cerrado en `TrailingStep2Pips`. |
| `CrossEffectiveBars` | int | 2 | Número de barras durante las cuales los cruces de EMA/RSI permanecen válidos. |
| `TunnelBandWidthPips` | decimal | 5 | Zona neutral alrededor del túnel Vegas donde no se toman operaciones. |
| `TunnelSafeZonePips` | decimal | 120 | Distancia máxima por encima del túnel para entradas largas (y por debajo para cortos). |
| `EnableLondonSession` | bool | true | Permitir señales durante las horas de Londres. |
| `EnableNewYorkSession` | bool | true | Permitir señales durante las horas de Nueva York. |
| `EnableTokyoSession` | bool | false | Permitir señales durante las horas de Tokio. |
| `FastPeriod` | int | 5 | Longitud de EMA rápida. |
| `SlowPeriod` | int | 12 | Longitud de EMA lenta. |
| `MaShift` | int | 0 | Desplazamiento horizontal aplicado a todas las EMAs. |
| `MaMethod` | `MovingAverageType` | Exponential | Modo de suavizado de la media móvil. |
| `MaAppliedPrice` | `AppliedPriceType` | Close | Precio de vela enviado a las EMAs. |
| `RsiPeriod` | int | 21 | Longitud de promediado del RSI. |
| `RsiAppliedPrice` | `AppliedPriceType` | Close | Precio de vela enviado al RSI. |
| `CandleType` | `DataType` | Marco temporal H1 | Serie de velas para el cálculo. |

## Notas

- La estrategia mantiene los estados de indicadores incluso fuera del horario de trading, exactamente como en el EA original.
- Las órdenes de stop se gestionan mediante API de alto nivel (`SellStop`/`BuyStop`) para imitar las llamadas
  `PositionModify` de MetaTrader.
- Todos los comentarios y estructura siguen las directrices del repositorio (tabulaciones para sangría, comentarios en inglés).
