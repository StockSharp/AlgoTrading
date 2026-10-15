# Estrategia fácil de robot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Easy Robot es un asesor experto que sigue el impulso y opera una vez por cada vela horaria completada. Cuando la vela anterior cierra alcista, la estrategia abre una nueva posición larga; cuando cierra bajista abre en corto. Sólo una posición puede estar activa en cualquier momento, reflejando completamente la lógica original MetaTrader 4.

## Reglas comerciales
1. Suscríbase al tipo de vela horaria seleccionado por el parámetro **CandleType** (el valor predeterminado es H1).
2. Una vez que se termina una vela, compara su cierre con su apertura:
   - Cerrar > Abrir: envía una orden de compra de mercado si no hay ninguna posición abierta.
   - Cerrar < Abrir: enviar una orden de venta de mercado si está plano.
3. El tamaño de la posición utiliza la propiedad de la estrategia `Volume`, exactamente igual que la versión MQL que se basó en `CheckVolumeValue` con un valor predeterminado de 0,01 lotes.
4. Los niveles de stop-loss y take-profit se basan en un indicador **Average True Range** con período **AtrPeriod** (predeterminado 14):
   - Distancia de parada = `ATR * StopFactor`.
   - Tomar distancia = `ATR * TakeFactor`.
   - Ambas distancias están normalizadas por la distancia mínima tick/pip, por lo que las órdenes de protección nunca se colocan más cerca de lo que permite el corredor.
5. Las órdenes de protección se registran inmediatamente después de la orden de mercado a través de `SetStopLoss` y `SetTakeProfit`, proporcionando el mismo comportamiento que `OrderSend` con los parámetros `sl` y `tp`.
6. El seguimiento opcional se activa cuando **UseTrailingStop** es verdadero. Después de que la operación acumula ganancias de **TrailingStartPips** (MetaTrader pips, es decir, puntos ajustados por cotizaciones de 3/5 decimales), la parada se acerca con **TrailingStepPips** y se empuja más solo cuando se alcanzan nuevos extremos de ganancias. El seguimiento respeta la distancia mínima de parada del corredor para evitar modificaciones no válidas.
7. Las cotizaciones para los cálculos de stop utilizan la mejor oferta/demanda cuando esté disponible, retrocediendo al último precio o cierre de vela, coincidiendo con las referencias originales `Bid`/`Ask`.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `TakeFactor` | 4.2 | multiplicador ATR para la distancia de obtención de beneficios (se asigna a la entrada `TakeFactor` en MQL). |
| `StopFactor` | 4.9 | multiplicador ATR para la distancia de stop-loss (se asigna a `StopFactor`). |
| `UseTrailingStop` | cierto | Habilita el seguimiento estilo MetaTrader (`UseTstop`). |
| `TrailingStartPips` | 40 | Beneficio en pips antes de que pueda comenzar el seguimiento (`Tstart`). |
| `TrailingStepPips` | 19 | Paso de pip aplicado al rastrear actualizaciones (`Tstep`). |
| `AtrPeriod` | 14 | periodo ATR de cálculo para el dimensionamiento de la volatilidad. |
| `CandleType` | H1 | Serie de velas utilizada para señales y entrada ATR. |

## Notas
- La estrategia restablece los precios de entrada y parada almacenados cada vez que la posición vuelve a cero, asegurando un estado limpio para la siguiente señal.
- La distancia mínima de parada se estima mediante el tamaño del pip del instrumento (o el paso del precio cuando el tamaño del pip no está disponible). Esto reproduce el ayudante `SC` del archivo de inclusión MQL.
- `StartProtection()` se llama una vez al inicio para que la plataforma pueda gestionar las salidas de emergencia si es necesario.
