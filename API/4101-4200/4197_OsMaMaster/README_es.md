# Estrategia OsMaMaster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia OsMaMaster reproduce el comportamiento del experto original **OsMaSter_V0** MetaTrader 4 basándose en el histograma MACD (OsMA) para detectar inversiones de impulso. La estrategia se suscribe a una única serie de velas y evalúa el punto de inflexión de OsMA más reciente una vez que se cierra una vela, lo que se alinea con la directriz del repositorio de trabajar únicamente con barras terminadas.

## Lógica de trading
- **Pila de indicadores**: se procesa un indicador `MovingAverageConvergenceDivergence` en cada vela terminada. Los períodos rápido, lento y de señal reflejan los parámetros de entrada MQL y el valor predeterminado es 26/09/5 respectivamente.
- **Precio aplicado**: el parámetro `AppliedPrice` asigna las constantes clásicas MetaTrader `PRICE_*` (0 = cierre, 1 = apertura, 2 = alto, 3 = bajo, 4 = mediana, 5 = típico, 6 = ponderado). El precio seleccionado se introduce directamente en el indicador MACD.
- **Detección de señal**: se comparan cuatro lecturas de OsMA según las compensaciones `Shift1`–`Shift4` suministradas. La configuración predeterminada (0,1,2,3) busca un mínimo o máximo local del histograma:
  - Configuración larga: `OsMA[shift4] > OsMA[shift3]`, `OsMA[shift3] < OsMA[shift2]`, `OsMA[shift2] < OsMA[shift1]`.
  - Configuración breve: `OsMA[shift4] < OsMA[shift3]`, `OsMA[shift3] > OsMA[shift2]`, `OsMA[shift2] > OsMA[shift1]`.
- **Política de posición única**: se envía una nueva operación solo cuando no hay ninguna posición abierta actualmente que coincida con el EA original que verificó las órdenes existentes a través de `ExistPositions`.

## Gestión de Puestos
- **Stop-loss** – `StopLossPips` define la distancia opcional (en pips) entre el precio de cumplimiento y el stop de protección. Un valor de `0` desactiva la parada.
- **Take-profit**: `TakeProfitPips` refleja el parámetro de toma de ganancias de EA. Cuando se establece en `0`, no se utiliza ningún objetivo fijo.
- **Modelo de ejecución**: tanto el stop como el objetivo se evalúan frente a los extremos de las velas (`HighPrice`/`LowPrice`). Si se supera un umbral dentro de una vela, la posición se cierra al cierre de la vela utilizando órdenes de mercado.
- **Reinicio de estado**: cada vez que se cierra la posición, todas las referencias de parada/objetivo pendientes se borran para que la siguiente entrada pueda configurarlas de nuevo.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Marco temporal de la serie de velas utilizado para todos los cálculos. | 1 hora |
| `FastEmaPeriod` | Longitud rápida EMA dentro del indicador MACD. | 9 |
| `SlowEmaPeriod` | Longitud lenta de EMA dentro del indicador MACD. | 26 |
| `SignalPeriod` | Longitud de la señal EMA utilizada para construir el histograma. | 5 |
| `AppliedPrice` | MetaTrader `PRICE_*` código que define qué precio de vela alimenta el MACD. | 0 (cerca) |
| `Shift1` | Primer cambio de OsMA (normalmente la barra actual). | 0 |
| `Shift2` | Segundo turno de OsMA. | 1 |
| `Shift3` | Tercer turno de OsMA. | 2 |
| `Shift4` | Cuarto turno de OsMA. | 3 |
| `StopLossPips` | Distancia de parada de protección en pips. | 50 |
| `TakeProfitPips` | Distancia objetivo de ganancias en pips. | 50 |

## Notas de conversión
- La implementación StockSharp mantiene un búfer de anillo compacto de los valores recientes de OsMA en lugar de solicitar repetidamente el historial del indicador, lo que garantiza el cumplimiento de la regla del repositorio sobre evitar recopilaciones de datos personalizadas.
- Todas las decisiones comerciales utilizan velas terminadas para evitar trabajar con valores de indicadores incompletos.
- La lógica de stop-loss y take-profit emulan la colocación de órdenes MQL monitoreando los máximos y mínimos de las velas y cerrando posiciones con órdenes de mercado.
- El volumen de estrategia predeterminado se establece en **0,01**, lo que refleja el tamaño de lote predeterminado de EA.

## Consejos de uso
- Ajuste los períodos `CandleType` y MACD para que coincidan con la volatilidad del instrumento. Los mercados más rápidos pueden beneficiarse de longitudes EMA más cortas.
- Considere deshabilitar la toma de ganancias configurando `TakeProfitPips` en `0` si desea aprovechar las tendencias extendidas y administrar las salidas manualmente.
- Cuando experimente con diferentes valores de `Shift`, asegúrese de que el cambio más grande no sea excesivamente grande; la estrategia mantiene solo tantos valores de histograma como requiere el cambio máximo.
- Debido a que las salidas se evalúan en función de los datos de las velas, el uso de marcos de tiempo más cortos reduce el retraso entre el incumplimiento del umbral real y la ejecución de la salida.
