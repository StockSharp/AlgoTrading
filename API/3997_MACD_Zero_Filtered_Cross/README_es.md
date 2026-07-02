# MACD Cruz filtrada cero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
MACD Zero Filtered Cross es un puerto C# del MetaTrader 4 asesor experto `Robot_MACD_12.26.9`. El robot original vigila
cruza entre la línea MACD y su línea de señal, pero filtra nuevas operaciones para que las entradas largas solo ocurran mientras ambas líneas
permanecen por debajo del eje cero y las entradas cortas sólo se producen mientras ambas líneas permanecen por encima del eje. La versión StockSharp mantiene el
misma lógica cruzada, agrega controles de riesgo que se integran con el marco (filtrado del saldo de la cartera y toma de ganancias unificada
gestión), y expone cada valor configurable a través de parámetros de estrategia que apoyan la optimización.

La estrategia se basa en velas terminadas durante un período de tiempo configurable. Los valores del indicador son proporcionados por el incorporado
indicador `MovingAverageConvergenceDivergenceSignal`, lo que garantiza que la estrategia siga siendo compatible con el nivel alto API y
respeta las pautas de uso de `BindEx`.

## Lógica estratégica
### Cálculo del indicador
* **MACD línea** – diferencia entre una media móvil exponencial rápida y lenta (longitudes predeterminadas: 12 y 26).
* **Línea de señal**: media móvil exponencial aplicada a la línea MACD (longitud predeterminada: 9).
* **Filtro cero**: el signo de ambas líneas en relación con cero determina si un cruce puede desencadenar una entrada de posición.

### Reglas de entrada
* **Configuración larga**
  * La línea MACD debe cruzar por encima de la línea de señal (`MACD[t-1] < Signal[t-1]` y `MACD[t] > Signal[t]`).
  * Tanto la línea MACD como la línea de señal deben estar por debajo de cero después del cruce.
  * La posición neta actual debe ser plana o corta; Los cortos existentes se cierran inmediatamente antes de intentar un largo.
  * Un filtro de saldo opcional requiere que el valor de la cartera supere un mínimo configurable antes de enviar un nuevo pedido.
* **Configuración corta**
  * La línea MACD debe cruzar por debajo de la línea de señal (`MACD[t-1] > Signal[t-1]` y `MACD[t] < Signal[t]`).
  * Ambas líneas del indicador deben estar por encima de cero después del cruce.
  * La posición neta actual debe ser plana o larga; Los largos existentes se aplanan antes de enviar un nuevo corto.
  * El filtro de saldo se aplica simétricamente a las entradas cortas.

### reglas de salida
* **Salida cruzada**: cuando la línea MACD vuelve a cruzar la línea de señal contra la posición actual, la estrategia se cierra
el comercio abierto en el mercado. Esto refleja el EA original, que siempre aplanó la posición en un cruce opuesto antes
buscando nuevas oportunidades.
* **Toma de ganancias fija**: una toma de ganancias basada en unidades (expresada en puntos de precio) se aplica a través de `StartProtection`. El nivel coincide
el parámetro MQL `TakeProfit` y utiliza el valor de puntos del instrumento.

### Gestión de riesgos y capital
* **Manejo de volumen**: el parámetro `LotVolume` refleja el tamaño del lote MT4. La estrategia envía ese volumen exacto para cada entrada.
* **Filtro de saldo**: el parámetro `MinimumBalancePerVolume` multiplica el volumen solicitado para determinar la cartera mínima
valor requerido antes de que se permitan nuevas entradas. Si la verificación del saldo falla, la estrategia registra un mensaje y omite la operación,
igualando la salvaguardia original del margen libre.
* **Integridad de los datos**: las señales se procesan solo en velas terminadas y después de que `IsFormedAndOnlineAndAllowTrading()` confirme que
Tanto la conexión como los indicadores están listos.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `FastPeriod` | EMA longitud del componente rápido MACD. |
| `SlowPeriod` | EMA longitud del componente lento MACD. |
| `SignalPeriod` | EMA longitud de la línea de señal MACD. |
| `TakeProfitPoints` | Distancia a la toma de ganancias protectora en puntos de precio. Establezca en `0` para desactivar. |
| `LotVolume` | Volumen base de pedidos, equivalente a la entrada de “Lotes” de la versión MT4. |
| `MinimumBalancePerVolume` | Valor mínimo de cartera requerido por unidad de volumen negociado antes de abrir una posición. Establezca en `0` para omitir el filtro. |
| `CandleType` | Marco de tiempo utilizado para construir velas y alimentar la cadena del indicador. |

## Notas adicionales
* La estrategia utiliza la sobrecarga `BindEx` para que el indicador MACD pueda suministrar tanto el valor MACD como el de señal en un solo
devolución de llamada sin llamadas manuales a `GetValue`.
* Todos los comentarios dentro del código C# están escritos en inglés, de acuerdo con las pautas del proyecto.
* No existe una traducción de Python para esta estrategia; solo la implementación de C# se proporciona en el paquete API.
* Para replicar el comportamiento original de MT4 lo más fielmente posible, seleccione un período de tiempo de vela que coincida con el gráfico donde solía ejecutarse EA
y mantener el parámetro de volumen consistente con el tamaño del lote negociado anteriormente.
