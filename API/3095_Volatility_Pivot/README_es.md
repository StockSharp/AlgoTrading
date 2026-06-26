# Estrategia de Pivote de Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Pivote de Volatilidad es un port de alto nivel de StockSharp del expert advisor original **Exp_VolatilityPivot.mq5**. Recrea el indicador personalizado Volatility Pivot proyectando dos líneas de stop adaptativas que siguen el precio usando volatilidad de Average True Range (ATR) o una desviación de precio fija. Cuando la tendencia cambia, el indicador emite flechas de rompimiento de una sola barra que desencadenan reversiones de posición. La estrategia puede seguir esas señales (`WithTrend`) u operar contra ellas (`CounterTrend`), proporcionando flexibilidad para estilos de rompimiento o reversión a la media.

A diferencia de la implementación MQL, esta versión se basa completamente en velas terminadas suministradas por `CandleType`. El modo ATR multiplica un ATR suavizado (EMA del ATR) por `AtrMultiplier`, mientras que el modo de precio usa el desplazamiento bruto `DeltaPrice`. Las líneas de pivote resultantes definen niveles de trailing alcista y bajista que gobiernan entradas y salidas.

## Datos de mercado e indicadores
- **Velas primarias (`CandleType`)** – todos los cálculos se realizan en este marco temporal. El valor predeterminado es una barra de 4 horas para coincidir con el expert advisor fuente.
- **ATR + suavizado EMA** – en modo `Atr` la estrategia procesa un `AverageTrueRange` con longitud `AtrPeriod` y luego lo suaviza por una `ExponentialMovingAverage` de longitud `SmoothingPeriod`.
- **Modo de desviación de precio** – en modo `PriceDeviation` el desplazamiento del trailing es la cantidad fija `DeltaPrice`, permitiendo distancias de stop deterministas cuando no se desea el suavizado de volatilidad.
- **Seguimiento del estado del pivote** – la estrategia mantiene los últimos valores de trail alcista/bajista y solo genera "señales" en la barra donde el trail cambia de un lado del precio al otro, reflejando los buffers del indicador de la versión MQL.

## Lógica de trading
1. **Cálculo del pivote** – para cada vela terminada la estrategia actualiza el precio del stop de trailing según las reglas de Volatility Pivot. Un trail alcista está activo cuando el precio cierra por encima del stop calculado; un trail bajista está activo cuando cierra por debajo.
2. **Detección de señales** – se dispara una nueva señal alcista (bajista) cuando el trail alcista (bajista) se activa después de estar inactivo en la barra anterior. El parámetro `SignalBar` retrasa la ejecución por el número solicitado de barras completadas, replicando la entrada `SignalBar` del script MQL.
3. **Filtro de dirección (`TradeDirection`)** – cuando se establece en `WithTrend` la estrategia compra en señales alcistas y vende en señales bajistas. Cuando se establece en `CounterTrend` la interpretación se invierte: las flechas alcistas cierran cortos y abren nuevos cortos, y viceversa.
4. **Permisos de entrada** – `EnableBuyEntries` y `EnableSellEntries` controlan si se pueden abrir nuevas posiciones largas o cortas.
5. **Permisos de salida** – `AllowLongExits` y `AllowShortExits` controlan si las posiciones existentes pueden cerrarse por señales directas o por el trail opuesto que permanece activo.
6. **Ajuste de posición** – la estrategia apunta a una posición neta de `+Volume` para largos, `-Volume` para cortos y `0` al aplanar. Las órdenes se dimensionan automáticamente para cerrar cualquier exposición opuesta antes de establecer la nueva dirección.
7. **Stops protectores** – las distancias opcionales de `StopLoss` y `TakeProfit` (expresadas en unidades de precio absolutas) monitorean cada vela terminada. Si el máximo/mínimo de la barra viola esos niveles, la estrategia sale inmediatamente de la posición.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `CandleType` | Serie de velas utilizada para el procesamiento de indicadores y la ejecución. | Velas de 4 horas |
| `AtrPeriod` | Longitud del componente ATR. | 100 |
| `SmoothingPeriod` | Longitud de suavizado EMA aplicada a los valores ATR. | 10 |
| `AtrMultiplier` | Multiplicador aplicado al ATR suavizado. | 3.0 |
| `DeltaPrice` | Desplazamiento de precio fijo usado cuando `PivotMode = PriceDeviation`. | 0.002 |
| `PivotMode` | Elige entre pivotes basados en ATR o desviación fija. | `Atr` |
| `TradeDirection` | Sigue (`WithTrend`) o desvanece (`CounterTrend`) los rompimientos de pivote. | `WithTrend` |
| `SignalBar` | Número de barras completadas a esperar antes de actuar sobre una señal. | 1 |
| `EnableBuyEntries` | Permitir abrir nuevas posiciones largas. | `true` |
| `EnableSellEntries` | Permitir abrir nuevas posiciones cortas. | `true` |
| `AllowLongExits` | Permitir cerrar posiciones largas existentes cuando persisten condiciones bajistas. | `true` |
| `AllowShortExits` | Permitir cerrar posiciones cortas existentes cuando persisten condiciones alcistas. | `true` |
| `StopLoss` | Distancia de stop-loss opcional (unidades de precio absolutas). Establecer en `0` para deshabilitar. | 0 |
| `TakeProfit` | Distancia de take-profit opcional (unidades de precio absolutas). Establecer en `0` para deshabilitar. | 0 |

> **Nota:** La propiedad `Strategy.Volume` de StockSharp define el tamaño de la posición. Configurarla antes de iniciar la estrategia para que coincida con el tamaño de contrato o acción del instrumento.

## Pautas de uso
1. Adjuntar la estrategia al `Security` y `Portfolio` deseados, y establecer `Volume` al tamaño de lote previsto.
2. Asegurarse de que la fuente de datos pueda suministrar el `CandleType` seleccionado. Sin un flujo continuo de velas terminadas, el suavizado ATR y la lógica de retraso de señal no pueden formarse.
3. Elegir `PivotMode` según el comportamiento del mercado: el modo ATR se adapta a la volatilidad, mientras que el modo de desviación de precio mantiene el trail fijo.
4. Ajustar `SignalBar` para reproducir el momento exacto del expert advisor original (retraso de 1 barra por defecto). Establecerlo en `0` ejecuta en la barra terminada más reciente.
5. Al usar `StopLoss`/`TakeProfit`, calibrar las distancias a la volatilidad del instrumento (son precios absolutos, no puntos ni porcentajes).
6. Monitorear los registros para mensajes informativos sobre entradas, salidas y stops protectores activados por cambios de pivote.

## Diferencias del expert advisor original
- Se eliminaron las opciones de gestión de dinero basadas en el saldo/margen libre de la cuenta. El tamaño de la posición se controla únicamente a través de `Strategy.Volume`.
- La "desviación" del precio de la orden y la sincronización manual de tiempo de la biblioteca auxiliar MQL son innecesarias porque StockSharp usa órdenes de mercado en velas terminadas.
- Se omiten las características de notificación, variables globales y carga manual de historial presentes en el script MQL.
- El manejo del stop protector y take-profit se simplifica a verificaciones basadas en velas; no hay colocación de órdenes intra-barra.

## Mejoras recomendadas
- Agregar filtros de sesión diaria o volatilidad para pausar el trading durante horas de baja liquidez.
- Extender la estrategia con gestión de trailing-stop que refleje las líneas de pivote, o exportar las líneas calculadas a un gráfico para visualización.
- Incorporar controles de riesgo a nivel de portafolio si múltiples instrumentos usan la misma instancia de estrategia.
