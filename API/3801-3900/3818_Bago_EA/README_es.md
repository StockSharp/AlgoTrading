# Bago EA Estrategia clásica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una fiel StockSharp versión del MetaTrader experto de `MQL/7656/Bago_ea.mq4`. Mantiene la filosofía original de seguimiento de tendencias: las entradas se activan solo cuando los promedios móviles exponenciales y RSI rompen la zona neutral en la misma dirección, mientras que el túnel de Las Vegas actúa como un filtro espacial y como ancla para el seguimiento paso a paso.

## Lógica comercial en detalle

1. **Pila de indicadores**
   - EMA rápidas y lentas (`FastPeriod`/`SlowPeriod`, método compartido `MaMethod`, precio aplicado `MaAppliedPrice`).
   - EMA del túnel Vegas con períodos fijos 144 y 169 que utilizan la misma configuración para emular las envolventes del túnel.
   - RSI (`RsiPeriod`, `RsiAppliedPrice`) con el clásico nivel 50 utilizado como filtro de confirmación.
   - Los datos de las velas provienen de `CandleType`; la misma vela alimenta todos los indicadores a través del proceso de alto nivel `Bind`.
2. **Máquina entre estados**
   - Los cruces EMA y RSI por encima o por debajo de sus umbrales establecen indicadores booleanos y contadores de barra. Cada estado expira después de que `CrossEffectiveBars` velas completadas o cuando aparece la cruz opuesta, exactamente como los temporizadores de la versión MQL.
   - Banderas de túnel adicionales rastrean cuando el precio de cierre salta de un lado al otro del túnel de Las Vegas para que la lógica de seguimiento sepa qué régimen aplicar.
3. **Puerta de sesión**
   - Solo se permite operar durante sesiones de mercado seleccionadas: Londres (07–16), Nueva York (12–21) y Tokio (00–08 más la barra de las 23:00). Estas ventanas replican los interruptores `extern bool` en el EA original.
4. **Filtros de entrada**
   - **Largo**: requiere banderas EMA-arriba y RSI-arriba y un cierre alcista por encima del túnel de al menos `TunnelBandWidthPips` pero no más de `TunnelSafeZonePips`, o un cierre de retroceso por debajo del túnel por `TunnelBandWidthPips` que indica un rebote.
   - **Breve**: condiciones reflejadas usando controles de túnel simétricos y EMA-abajo/RSI-abajo.
   - Cuando se abre una posición inversa, la estrategia la cierra en el mercado antes de entrar en la nueva dirección, imitando `OrderClose` de MetaTrader.
5. **Gestión de posiciones y salidas**
   - El stop-loss inicial se coloca a `StopLossPips` de la entrada. Cada vez que el precio se estaciona alrededor del túnel de Las Vegas, la parada se reubica utilizando un colchón adicional `StopLossToFiboPips` para igualar las compensaciones "fibo" del experto.
   - Los pasos finales corresponden a los niveles de TP del EA. A medida que el precio se aleja del túnel, la estrategia primero estaciona la parada cerca del túnel ±(`TrailingStepX` + `StopLossToFiboPips`) y gradualmente cambia a un seguimiento puro del precio de `TrailingStopPips`.
   - Las salidas parciales (`PartialClose1Volume`, `PartialClose2Volume`) se ejecutan una vez que el movimiento llega a `TrailingStep1Pips` y `TrailingStep2Pips`. El volumen restante lo gestiona el trailing stop hasta que se alcanza el tercer paso (`TrailingStep3Pips`).
   - Cualquier cruce opuesto EMA/RSI cierra inmediatamente la posición completa.
6. **Manejo de pedidos**
   - Las órdenes de suspensión se mantienen explícitamente mediante llamadas `SellStop`/`BuyStop`. Cada vez que es necesario mover la parada, se cancela la orden anterior y se envía una nueva; esto refleja las repetidas llamadas `OrderModify` del código MQL.
   - Todos los cálculos de pips se basan en el instrumento `PriceStep` y se ajustan automáticamente a cotizaciones de 3 o 5 dígitos multiplicando el paso por diez, al igual que la conversión de puntos de MetaTrader.

## Parámetros

| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|---------|-------------|
| `TradeVolume` | decimales | 3 | Volumen total abierto con una nueva señal. |
| `StopLossPips` | decimales | 30 | Distancia de parada de protección inicial en pips. |
| `StopLossToFiboPips` | decimales | 20 | Amortiguador adicional al moverse se detiene alrededor de las bandas del túnel de Las Vegas. |
| `TrailingStopPips` | decimales | 30 | Distancia de la parada final cuando el precio sale del túnel. |
| `TrailingStep1Pips` | decimales | 55 | Primera capa de ganancias derivada del nivel TP1 de EA. |
| `TrailingStep2Pips` | decimales | 89 | Segunda capa de beneficio (TP2). |
| `TrailingStep3Pips` | decimales | 144 | Tercera capa de beneficios (TP3) antes de pasar al trailing puro. |
| `PartialClose1Volume` | decimales | 1 | Volumen para cerrar cuando se alcance `TrailingStep1Pips`. |
| `PartialClose2Volume` | decimales | 1 | Volumen para cerrar cuando se alcance `TrailingStep2Pips`. |
| `CrossEffectiveBars` | entero | 2 | Número de velas completadas mientras las banderas cruzadas siguen siendo válidas. |
| `TunnelBandWidthPips` | decimales | 5 | Zona neutral alrededor del túnel donde se evitan nuevos intercambios. |
| `TunnelSafeZonePips` | decimales | 120 | Distancia máxima desde el túnel que aún permite una entrada de fuga. |
| `EnableLondonSession` | booleano | cierto | Habilite el comercio entre las 07:00 y las 16:00, hora de cambio. |
| `EnableNewYorkSession` | booleano | cierto | Habilite el comercio entre las 12:00 y las 21:00 hora de cambio. |
| `EnableTokyoSession` | booleano | falso | Habilite el comercio entre las 00:00 y las 08:00 y en la vela de las 23:00. |
| `FastPeriod` | entero | 5 | Longitud rápida EMA. |
| `SlowPeriod` | entero | 12 | Longitud lenta de EMA. |
| `MaShift` | entero | 0 | Desplazamiento horizontal de las medias móviles. |
| `MaMethod` | `MovingAverageType` | exponencial | Modo de cálculo EMA (se mantiene configurable para experimentación). |
| `MaAppliedPrice` | `AppliedPriceType` | Cerrar | Precio de vela enviado a las EMA. |
| `RsiPeriod` | entero | 21 | RSI período promedio. |
| `RsiAppliedPrice` | `AppliedPriceType` | Cerrar | Precio de vela reenviado al RSI. |
| `CandleType` | `DataType` | Periodo H1 | Serie de velas que impulsa la estrategia. |

## Notas de implementación

- La estrategia se ejecuta completamente en la suscripción de vela de alto nivel API y mantiene los valores del indicador en listas móviles para imitar la indexación de barras (`Close[1]`, `Close[2]`) del script original.
- Los temporizadores y las banderas de túnel reproducen la máquina de estado finito que determina si un cruce todavía está "fresco".
- Se llama a `StartProtection()` `OnStarted` para que los controles de riesgo integrados de StockSharp supervisen la posición abierta al igual que el stop-loss estricto de MetaTrader.
- Los comentarios en línea están escritos en inglés y describen cada paso de la conversión para facilitar el mantenimiento.
