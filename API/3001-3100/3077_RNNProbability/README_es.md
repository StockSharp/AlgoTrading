# Estrategia RNN Probability
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia RNN Probability es una conversión del expert de MetaTrader *RNN (barabashkakvn's edition)*. El robot original recopila tres instantáneas RSI separadas por el período RSI y las alimenta a una red de probabilidad artesanal que emula una red neuronal recurrente. El port de StockSharp replica este comportamiento con la API de suscripción de velas de alto nivel, convirtiendo automáticamente los lotes, pasos de precio y distancias de stop/objetivo de MetaTrader en conceptos de StockSharp.

Una vez que el valor RSI de la última vela terminada está disponible, la estrategia retrocede uno y dos períodos RSI para construir un historial de tres puntos. Estas lecturas normalizadas se combinan con los ocho pesos de MetaTrader (`Weight0` … `Weight7`) para producir una probabilidad de que el mercado deba caer. La probabilidad se remapea al rango `[-1; 1]`, y el signo determina si abrir una posición larga o corta. Solo se mantiene una posición a la vez, coincidiendo con el Expert Advisor original.

## Lógica de trading
1. Suscribirse a la serie de velas configurada y procesar manualmente el indicador `RelativeStrengthIndex` usando la entrada `AppliedPrice` seleccionada (apertura por defecto).
2. Almacenar los valores RSI terminados en un búfer continuo suficientemente grande para acceder a la lectura RSI de uno y dos períodos completos atrás.
3. Normalizar los tres valores RSI al rango `[0; 1]` y evaluar la red de probabilidad:
   - La primera rama (`Weight0`, `Weight1`, `Weight2`, `Weight3`) maneja el caso cuando el RSI actual está en la mitad inferior (por debajo de 50).
   - La segunda rama (`Weight4`, `Weight5`, `Weight6`, `Weight7`) maneja el caso cuando el RSI actual está en la mitad superior.
4. Transformar la probabilidad resultante en una señal de operación entre `-1` y `+1`.
5. Si no hay posición abierta y la señal es negativa, comprar `TradeVolume` lotes. Si la señal es no negativa, vender `TradeVolume` lotes en su lugar.
6. Opcionalmente armar niveles simétricos de stop-loss y take-profit expresados en pips. La estrategia convierte automáticamente la distancia en pips a un desplazamiento de precio absoluto, incluyendo el ajuste de dígito adicional usado por MetaTrader para símbolos forex de 3 y 5 dígitos.
7. Registrar cada decisión con las entradas RSI, probabilidad y señal resultante, reflejando el comportamiento informativo del experto de origen.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Marco temporal de 1 hora | Serie de velas principal usada para actualizaciones de indicadores y generación de señales. |
| `TradeVolume` | `decimal` | `1` | Tamaño de lote enviado con cada orden de mercado. |
| `RsiPeriod` | `int` | `9` | Longitud del indicador RSI. También define la distancia entre las muestras RSI históricas. |
| `AppliedPrice` | `AppliedPriceType` | `Open` | Componente de precio reenviado al RSI (Open, Close, High, Low, Median, Typical, Weighted). |
| `StopLossTakeProfitPips` | `decimal` | `100` | Distancia en pips para stop-loss y take-profit. Establecer en cero para deshabilitar órdenes protectoras. |
| `Weight0` … `Weight7` | `decimal` | `6, 96, 90, 35, 64, 83, 66, 50` | Pesos de probabilidad aplicados a las ocho ramas de la red. Cada valor representa un porcentaje entre 0 y 100. |

## Diferencias respecto al expert original de MetaTrader
- Las notificaciones por email fueron eliminadas. Los registros de StockSharp proporcionan la misma información sin depender de un servidor SMTP.
- El dimensionamiento de posición está fijo en un único `TradeVolume`. Los cierres parciales o el escalado incremental se omiten intencionalmente para coincidir con el diseño de una posición del código fuente.
- Los datos del indicador se entregan a través de la suscripción de velas de alto nivel de StockSharp, eliminando las llamadas manuales a `CopyBuffer` y la aritmética de punteros.
- La conversión de pips usa el `PriceStep` del instrumento y compensa automáticamente los símbolos forex de 3/5 dígitos en lugar de depender de tamaños de tick codificados.

## Consejos de uso
- Alinear `TradeVolume` con el paso mínimo de lote del instrumento antes de lanzar la estrategia; el constructor también refleja el valor en `Strategy.Volume`.
- Ajustar los ocho pesos durante la optimización para adaptar la red de probabilidad a diferentes mercados. Todos los pesos se exponen como parámetros de optimización.
- Decrementar `StopLossTakeProfitPips` o establecerlo en cero cuando se opere con símbolos de spreads amplios o cuando se usen salidas discrecionales.
- Agregar la estrategia a un gráfico para visualizar velas, RSI y operaciones ejecutadas para facilitar la validación de la salida de la red neuronal.

## Indicadores
- Un `RelativeStrengthIndex` calculado desde el precio aplicado elegido.
