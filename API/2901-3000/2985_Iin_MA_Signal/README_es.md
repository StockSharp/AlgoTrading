# Estrategia Iin MA Signal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia reproduce el comportamiento del clásico asesor experto MQL5 **Iin MA Signal**. Observa un cruce entre una media móvil rápida y una lenta y reacciona en la barra definida por el parámetro `SignalBar`, igual que la plantilla original que sondeaba los búferes del indicador. Los cruces alcistas abren posiciones largas y opcionalmente cierran cortos existentes, mientras que los cruces bajistas abren cortos y opcionalmente cierran largos. Los stops y objetivos pueden adjuntarse automáticamente a través de la protección de posición de StockSharp.

## Lógica de trading
1. Suscribirse a una única serie de velas especificada por `CandleType` (predeterminado: velas de 1 hora).
2. Construir dos medias móviles usando los tipos y longitudes definidos por `FastMaType`/`FastPeriod` y `SlowMaType`/`SlowPeriod`. SMA, EMA, SMMA (RMA) y LWMA son compatibles para cubrir las combinaciones disponibles en el código fuente MQL.
3. Almacenar una ventana deslizante de valores de media móvil para que el cruce pueda evaluarse en el índice de vela dado por `SignalBar`. Esto imita las solicitudes `CopyBuffer` del asesor experto original.
4. Detectar un cruce alcista cuando la MA rápida estaba por debajo de la MA lenta en la barra anterior de la ventana y sube por encima de ella en la barra de señal mientras que la tendencia anterior no era ya alcista. Un cruce bajista se detecta de forma simétrica.
5. Actualizar el indicador de tendencia interno después de cada cruce confirmado para evitar entradas duplicadas y replicar la variable guarda `trend` del indicador MQL.
6. Cuando se permite el trading (`IsFormedAndOnlineAndAllowTrading()` devuelve verdadero), enviar las órdenes de mercado definidas por los indicadores de entrada/salida.

## Reglas de entrada
- **Entrada larga**: se activa en un cruce alcista si `AllowLongEntries` está habilitado y la posición actual es plana o corta. Cualquier corto abierto puede cerrarse primero cuando `CloseShortOnSignal` es verdadero.
- **Entrada corta**: se activa en un cruce bajista si `AllowShortEntries` está habilitado y la posición actual es plana o larga. Cualquier largo abierto puede cerrarse primero cuando `CloseLongOnSignal` es verdadero.

## Reglas de salida
- Las señales opuestas pueden cerrar posiciones según los interruptores `CloseLongOnSignal` y `CloseShortOnSignal`.
- Los niveles de salida de protección opcionales usan distancias de precio absolutas: `StopLossPoints` y `TakeProfitPoints`. Cuando alguno de los valores es mayor que cero, la estrategia llama a `StartProtection` para armar el stop-loss y/o el take-profit usando órdenes de mercado.

## Parámetros
| Parámetro | Descripción | Valor predeterminado |
| --- | --- | --- |
| `CandleType` | Tipo de datos que describe la serie de velas utilizada para los cálculos. | Marco temporal de 1 hora |
| `FastPeriod` | Período de la media móvil rápida. | 10 |
| `FastMaType` | Tipo de la media móvil rápida (`Sma`, `Ema`, `Smma`, `Lwma`). | `Ema` |
| `SlowPeriod` | Período de la media móvil lenta. | 22 |
| `SlowMaType` | Tipo de la media móvil lenta (`Sma`, `Ema`, `Smma`, `Lwma`). | `Sma` |
| `SignalBar` | Número de barras cerradas atrás que deben contener el cruce (1 reproduce el predeterminado de MQL). | 1 |
| `AllowLongEntries` | Habilitar o deshabilitar entradas largas. | `true` |
| `AllowShortEntries` | Habilitar o deshabilitar entradas cortas. | `true` |
| `CloseLongOnSignal` | Cerrar posiciones largas cuando aparece una señal bajista. | `true` |
| `CloseShortOnSignal` | Cerrar posiciones cortas cuando aparece una señal alcista. | `true` |
| `StopLossPoints` | Distancia absoluta de stop-loss en unidades de precio (0 deshabilita). | 1000 |
| `TakeProfitPoints` | Distancia absoluta de take-profit en unidades de precio (0 deshabilita). | 2000 |

## Notas de implementación
- Se utilizan APIs de StockSharp de alto nivel en todo momento: `SubscribeCandles` solicita datos de mercado y `Bind` transmite los valores de MA directamente a la estrategia sin manejo manual del historial.
- La fábrica de medias móviles (`CreateMa`) mapea los valores de enumeración a los indicadores de StockSharp, evitando cálculos personalizados.
- Un búfer compacto en memoria mantiene solo `SignalBar + 2` muestras, lo suficiente para evaluar el cruce en la barra solicitada y la anterior.
- Las órdenes de protección son opcionales y se inicializan solo si se configuran distancias distintas de cero, replicando el módulo MM opcional de la versión MQL.
- Todos los comentarios del código están escritos en inglés según las reglas del repositorio.

## Uso
1. Compilar la solución (`dotnet build AlgoTrading.sln`) para compilar la nueva estrategia.
2. Instanciar `IinMaSignalStrategy` en su aplicación, configurar los parámetros deseados y asignar un conector/instrumento/portafolio antes de iniciarlo.
3. Opcionalmente adjuntar la estrategia a un gráfico para visualizar las medias móviles rápida y lenta junto con las operaciones ejecutadas.
4. Optimizar los períodos de MA, la barra de señal y la configuración de riesgo para adaptar la plantilla a diferentes mercados.

## Diferencias respecto al asesor experto MQL original
- La versión de StockSharp utiliza suscripción de alto nivel y vinculación de indicadores en lugar de consultas manuales de búfer.
- Los auxiliares de gestión del dinero de `TradeAlgorithms.mqh` son reemplazados por `StartProtection`, que ofrece automatización equivalente de stop y objetivo.
- La gestión de posiciones es plana por defecto: la estrategia evita el hedging no abriendo una nueva posición mientras el lado opuesto sigue activo, a menos que el indicador de cierre esté deshabilitado.
- El renderizado del gráfico aprovecha los métodos auxiliares de StockSharp y no intenta replicar los búferes de flecha originales.
