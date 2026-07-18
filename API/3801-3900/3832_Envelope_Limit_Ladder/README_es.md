# Estrategia de escalera de límite envolvente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de escalera de límite de sobre** es una adaptación de C# del asesor experto MetaTrader `E_2_12_5min.mq4` (ID 7671). Reconstruye la escalera original de órdenes límite alrededor de un sobre EMA en velas de 5 minutos mientras mantiene el modelo de gestión de seguimiento y múltiples objetivos del robot heredado.

## Concepto

1. **Filtro de envolvente**: una envolvente de media móvil (predeterminada EMA 144 con una desviación del 0,05 %) calculada en el período de tiempo configurable `EnvelopeCandleType` proporciona la línea media y las bandas superior/inferior.
2. **Vela de señal**: las señales comerciales se evalúan en la suscripción `CandleType` (5 minutos predeterminados). Cuando la vela anterior cierra entre la línea media y la banda más cercana, los brazos estratégicos limitan las órdenes en la línea media.
3. **Escalera de órdenes**: se colocan hasta tres límites de compra y tres límites de venta simultáneamente:
   - Precio de entrada: valor de línea media alineado.
   - Stop-loss: banda envolvente opuesta.
   - Take-profit: banda ± compensaciones definidas por el usuario (8, 13 y 21 puntos por defecto).
4. **Ventana de negociación**: las órdenes pendientes se crean solo cuando `TradingStartHour < Hour < TradingEndHour`. Todos los límites restantes se cancelan una vez que llega la hora de apertura `TradingEndHour`.
5. **Gestión de posición**: cada orden límite ejecutada coloca inmediatamente su propia orden de parada y toma de ganancias. Un modo de seguimiento opcional mueve el stop a la media móvil (o lo mantiene en la banda opuesta) cuando el precio supera el sobre.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | 5 minutos | Tipo vela para detección de señal. |
| `EnvelopeCandleType` | 5 minutos | Tipo de vela utilizado para calcular la envolvente. Utilice un período de tiempo más alto para imitar la entrada MT4 `EnvTimeFrame`. |
| `EnvelopePeriod` | 144 | Longitud media móvil de la envolvente. |
| `MaMethod` | EMA | Método de media móvil (`SMA`, `EMA`, `SMMA`, `LWMA`). |
| `EnvelopeDeviation` | 0,05 | Ancho del sobre en porcentaje (0,05 = 0,05%). |
| `TradingStartHour` | 0 | Primera hora en la que pueden aparecer órdenes pendientes (comprobación exclusiva, coincide con el comportamiento de MT4). |
| `TradingEndHour` | 17 | Hora en la que se eliminan todas las órdenes pendientes (límite superior exclusivo). |
| `FirstTakeProfitPoints` | 8 | Desplazamiento en puntos agregados más allá del envolvente del primer peldaño de la escalera. |
| `SecondTakeProfitPoints` | 13 | Desplazamiento en puntos para el segundo peldaño. |
| `ThirdTakeProfitPoints` | 21 | Desplazamiento en puntos para el tercer peldaño. |
| `UseOppositeEnvelopeTrailing` | `true` | Mantiene el stop en la banda opuesta (`true`) o lo sigue hasta la media móvil (`false`). Refleja la bandera MT4 `MaElineTSL`. |
| `OrderVolume` | 0.1 | Volumen por orden pendiente (reemplaza el tamaño de lote adaptable de MT4). |

## Notas de comportamiento

- La estrategia mantiene un par stop/take separado para cada orden límite ejecutada. Las salidas no interfieren con los peldaños restantes de la escalera.
- El seguimiento solo se activa después de una ruptura más allá del sobre y solo aprieta el stop en la dirección rentable.
- Cuando `EnvelopeCandleType` difiere de `CandleType`, los valores de envolvente más recientes de la suscripción secundaria se reutilizan para velas de señal, coincidiendo estrechamente con la búsqueda de envolvente de marco de tiempo superior MT4.
- La rutina de administración de dinero MT4 original (`LotsOptimized`) se reemplaza por el parámetro explícito `OrderVolume` para mantener el puerto determinista dentro de StockSharp.

## Consejos de uso

- Haga coincidir el período de tiempo del sobre con las entradas de MT4 para reproducir el comportamiento original (por ejemplo, mantenga `EnvelopeCandleType` en 5 minutos o cambie a 1 hora/4 horas según sea necesario).
- Establezca `UseOppositeEnvelopeTrailing` en `false` si desea que el trailing stop salte a la media móvil en lugar de a la banda opuesta una vez que el precio salga del sobre.
- Optimice las compensaciones de toma de ganancias y la desviación envolvente juntas; las distancias de la escalera dependen de la volatilidad capturada por la envolvente.
