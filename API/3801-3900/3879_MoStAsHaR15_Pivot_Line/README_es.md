# Estrategia de línea pivote MoStAsHaR15
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el experto "MoStAsHaR15 ForReX - Pivot Line" MetaTrader 4 utilizando la estrategia de alto nivel de StockSharp API. Mantiene el mapa de pivote de piso diario original combinado con filtros de impulso de ADX, EMA diferenciales y el histograma MACD (OsMA). La lógica intradiaria opera con un flujo de velas cada hora, mientras que una segunda suscripción consume la vela diaria completada anteriormente para reconstruir la escalera de pivote antes de cada decisión.

## Lógica de trading
- **Cálculo del pivote**: el máximo, el mínimo y el cierre de ayer de la serie diaria generan el pivote clásico (P), tres niveles de resistencia (R1–R3), tres niveles de soporte (S1–S3) y seis puntos medios (M0–M5). El cierre actual de la vela se compara con esta escalera para detectar el rango circundante. Se conserva el mapeo inusual del EA original que vincula la región entre M5 y R3 con el segmento S3/M0.
- **Filtro de distancia**: las operaciones solo se activan cuando la distancia hasta el límite de obtención de ganancias que limita el rango actual es mayor que `MinimumDistancePips` (14 pips de forma predeterminada), lo que refleja las comprobaciones originales `dif1`/`dif2`.
- **Las entradas largas** requieren todos los siguientes filtros:
  - La línea principal ADX está por encima de `AdxThreshold` (20) y el componente +DI está aumentando y es más fuerte que −DI.
  - El EMA de cierre está al menos `EmaSpreadPips` (5 pips) por encima del EMA de apertura, y la vela anterior ya tenía el mismo orden alcista.
  - El histograma MACD aumentó en comparación con la vela anterior (OsMA en aumento).
- **Las entradas cortas** reflejan la rama larga con fuerza −DI, diferencial bajista EMA y un histograma MACD descendente.
- Sólo se permite una posición neta en cualquier momento. Las órdenes se envían con ejecución de mercado utilizando `BuyMarket()` y `SellMarket()`.

## Gestión de Puestos
- **Stop-loss** – opcional, ubicado `StopLossPips` debajo/arriba del precio de entrada. Establezca en `0` para desactivar como en el EA original.
- **Take-profit**: fijado en el límite de pivote (soporte o resistencia) que delimita el rango de precios actual cuando se abre la operación.
- **Parada dinámica**: una vez que el precio avanza más de `TrailingStopPips + TrailingStepPips` más allá de la entrada, la parada se sigue para mantener una distancia de `TrailingStopPips`. El valor del paso debe permanecer positivo siempre que esté habilitado el seguimiento.
- Si el stop-loss, el trailing stop o el take-profit se tocan dentro de una vela, la posición se cierra según la evaluación de esa barra.

## Parámetros de estrategia
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `HourlyCandleType` | Serie de velas intradiarias que alimentan la lógica de ejecución. | 1 hora |
| `DailyCandleType` | Flujo de velas diario utilizado para calcular los niveles de pivote. | 1 dia |
| `StopLossPips` | Distancia inicial de stop-loss en pips. `0` lo desactiva. | 20 |
| `TrailingStopPips` | Distancia del trailing stop en pips. | 10 |
| `TrailingStepPips` | Movimiento mínimo (en pips) antes de que se actualice el trailing stop. Debe ser > 0 cuando el seguimiento está habilitado. | 5 |
| `MinimumDistancePips` | Distancia mínima de pips hasta el límite de pivote cercano antes de ingresar a una operación. | 14 |
| `EmaSpreadPips` | Spread requerido entre el cierre EMA y la apertura EMA. | 5 |
| `AdxThreshold` | Lectura mínima de ADX que activa la señal. | 20 |
| `AdxPeriod` | ADX período del indicador. | 14 |
| `EmaClosePeriod` | EMA longitud aplicada a los cierres de velas. | 5 |
| `EmaOpenPeriod` | EMA longitud aplicada a las aperturas de velas. | 8 |
| `MacdFastPeriod` | Período EMA rápida para MACD (numerador OsMA). | 12 |
| `MacdSlowPeriod` | Período lento de EMA durante MACD. | 26 |
| `MacdSignalPeriod` | Periodo de señal EMA durante MACD. | 9 |

## Notas de conversión
- Los valores del indicador se evalúan solo en velas terminadas y no se almacenan colecciones continuas; el estado se administra a través de campos escalares según las pautas del repositorio.
- Los pips se derivan de la precisión decimal y `PriceStep` del valor. Los símbolos citados con 3 o 5 decimales utilizan la convención "mini pip" al igual que MetaTrader.
- El mapeo de obtención de ganancias para la región M5→R3 recurre intencionalmente al par S3/M0 para permanecer fiel al código fuente.
- Todos los comentarios dentro de la estrategia permanecen en inglés como lo exigen las instrucciones del proyecto.

## Consejos de uso
- Ajuste los tipos de velas para que coincidan con la sesión de negociación de su instrumento, especialmente para mercados con rollovers diarios no estándar.
- Debido a que la lógica evalúa paradas y objetivos en velas cerradas, en mercados rápidos puede ocurrir un deslizamiento adicional en comparación con la ejecución del nivel de tick MetaTrader.
- Considere ajustar `MinimumDistancePips` y `EmaSpreadPips` cuando aplique la estrategia a activos con diferentes regímenes de volatilidad.
