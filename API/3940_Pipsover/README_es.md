# Estrategia Pipsover 8167
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **Pipsover 8167** es una versión StockSharp del MetaTrader 4 asesor experto `Pipsover.mq4` distribuido con la compilación 8167. El experto busca picos fuertes del oscilador Chaikin que aparecen inmediatamente después de un retroceso al promedio móvil simple de 20 períodos en la vela anterior. Cuando ocurre esa combinación, el script abre una posición en la dirección del impulso y la protege con distancias fijas de stop-loss y take-profit (70 y 140 puntos respectivamente en el código original MQL). Esta versión de C# reconstruye exactamente la misma lógica utilizando componentes StockSharp de alto nivel para que no se requiera acceso directo al búfer.

La implementación utiliza el indicador Línea de acumulación/distribución (ADL) y dos promedios móviles exponenciales para reconstruir los valores del oscilador Chaikin producidos por `iCustom("Chaikin", ...)` en MetaTrader. Todas las decisiones comerciales se retrasan hasta que la vela se cierra por completo, replicando las comprobaciones `OrdersTotal()` y `Close[1]`/`Open[1]` del script fuente.

## Indicadores y Señales
- **Promedio móvil simple (SMA 20)** – aplicado a los cierres de velas. La vela anterior debe perforar el SMA (mínimo por debajo para largos, alto por encima para cortos) mientras mantiene un cuerpo en la dirección de la configuración.
- **Oscilador Chaikin (EMA 3 – EMA 10 de ADL)** – reconstruido internamente a partir de la secuencia ADL para reflejar las lecturas de `iCustom("Chaikin", 0, 0, 1)`. Los umbrales de entrada y salida se expresan en unidades absolutas del oscilador.
- **Filtro de acción del precio**: la estrategia verifica la dirección del cuerpo de la vela anterior: los cuerpos alcistas permiten operaciones largas mientras que los bajistas permiten operaciones cortas.

## Reglas de trading
### Entrada larga
1. La vela anterior cierra alcista (`Close[1] > Open[1]`).
2. El mínimo anterior se rompe por debajo del valor SMA20 de esa vela.
3. El valor de Chaikin anterior está por debajo de `-OpenLevel` (predeterminado 55).
4. Actualmente no hay ninguna posición abierta.

### Entrada corta
1. La vela anterior cierra bajista (`Close[1] < Open[1]`).
2. El máximo anterior está por encima del valor SMA20 de esa vela.
3. El valor de Chaikin anterior está por encima de `OpenLevel`.
4. Actualmente no hay ninguna posición abierta.

### Condiciones de salida
- **Las posiciones largas** se cierran cuando se satisface la siguiente vela: cuerpo bajista, muy por encima de SMA20 y Chaikin por encima de `CloseLevel` (predeterminado 90).
- **Las posiciones cortas** se cierran cuando la siguiente vela tiene un cuerpo alcista, un mínimo por debajo de SMA20 y Chaikin por debajo de `-CloseLevel`.
- Además, cada operación conlleva un stop de protección en `StopLossPoints` y una toma de ganancias en `TakeProfitPoints`, ambos expresados en incrementos de precio del instrumento seleccionado.

## Gestión del riesgo
- Distancia de stop-loss: `StopLossPoints × PriceStep` (el valor predeterminado es 70 puntos).
- Distancia de obtención de beneficios: `TakeProfitPoints × PriceStep` (el valor predeterminado es 140 puntos).
- Tamaño de posición: configurable a través de `TradeVolume`, asignado directamente a la propiedad `Volume` de la estrategia StockSharp y utilizado para todas las órdenes de mercado.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `TradeVolume` | 0.1 | Volumen de órdenes de mercado (lotes o contratos, según el valor). |
| `MaLength` | 20 | Período del SMA utilizado para la verificación de retroceso. |
| `StopLossPoints` | 70 | Distancia de stop-loss medida en pasos de precio. |
| `TakeProfitPoints` | 140 | Distancia de obtención de beneficios medida en incrementos de precios. |
| `OpenLevel` | 55 | Umbral absoluto del oscilador Chaikin que desbloquea nuevas entradas. |
| `CloseLevel` | 90 | Umbral absoluto del oscilador Chaikin que fuerza las salidas. |
| `ChaikinFastLength` | 3 | Longitud rápida EMA en la reconstrucción de Chaikin. |
| `ChaikinSlowLength` | 10 | Longitud lenta EMA en la reconstrucción de Chaikin. |
| `CandleType` | H1 | Marco de tiempo utilizado para suscribirse a velas y calcular indicadores. |

## Notas de implementación
- Las velas y los indicadores están conectados a través de `SubscribeCandles().Bind(...)`, por lo que la estrategia se mantiene dentro de las pautas de alto nivel API.
- Los valores de Chaikin se calculan en la memoria alimentando lecturas de ADL en dos objetos EMA, evitando llamadas prohibidas como `GetValue()` en los buffers del indicador.
- La información de la vela anterior se almacena en caché dentro del estado de la estrategia para reproducir el patrón de acceso MQL `Close[1]`, `Low[1]`, `High[1]` y `iCustom(...,1)`.
- Los niveles de stop-loss y take-profit se rastrean manualmente porque el experto original envió órdenes de mercado simples con compensaciones estáticas en lugar de órdenes de protección del lado del servidor.
