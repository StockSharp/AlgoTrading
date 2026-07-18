# Estrategia de Ruptura Fractal RSI Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el asesor experto de MetaTrader "RSI and Bollinger Bands" en StockSharp. Aplica Bandas de Bollinger al oscilador RSI, espera un nivel de ruptura fractal reciente y coloca órdenes stop más allá de ese nivel con desplazamientos configurables. Un filtro de trailing Parabolic SAR ajusta dinámicamente los stops una vez que una posición está abierta.

## Indicadores y señales
- **RSI** (por defecto 8 períodos) – el oscilador principal. Los umbrales de sobrecompra y sobreventa se usan para cancelar órdenes pendientes.
- **Bandas de Bollinger sobre RSI** (por defecto 14 períodos, desviación 1.0) – las entradas solo se activan cuando el RSI cierra fuera de la banda superior o inferior, coincidiendo con el comportamiento del script original donde Bollinger se alimenta de valores RSI.
- **Fractales de Bill Williams** – la estrategia escanea los últimos fractales confirmados de subida y bajada (patrón de 5 barras) y usa sus precios como niveles base de ruptura.
- **Parabolic SAR** (paso 0.003, máximo 0.2) – entrega una referencia de trailing stop una vez que una posición está activa.

## Lógica de entrada
1. El trabajo se realiza en velas finalizadas del marco temporal seleccionado (por defecto 4 horas).
2. Cuando aparece un **fractal alcista** y el RSI cierra por encima de la **banda Bollinger superior**, mientras el cierre anterior permanece por debajo del fractal, se coloca un **buy stop**:
   - Precio de entrada = máximo del fractal + indent (15 pips por defecto).
   - Stop loss opcional = entrada − StopLossPips.
   - Take profit opcional = entrada + TakeProfitPips.
3. Simétricamente, cuando se forma un **fractal bajista** y el RSI cierra por debajo de la **banda Bollinger inferior**, mientras el cierre anterior permanece por encima del fractal, se coloca un **sell stop** debajo del fractal.
4. El RSI revirtiendo dentro del canal cancela órdenes pendientes:
   - RSI < umbral inferior cancela buy stops.
   - RSI > umbral superior cancela sell stops.

## Salida y gestión de riesgo
- Las distancias fijas de stop loss y take profit (en pips) replican las entradas MQL. Establecer cualquier distancia en `0` deshabilita esa protección.
- La lógica de trailing Parabolic SAR requiere que el SAR esté al menos `SarTrailingPips` alejado del precio actual y solo mueve el stop en la dirección favorable.
- Cuando el trailing stop cruza el precio o el precio alcanza el take profit fijo, la posición se cierra con una orden de mercado.
- Abrir una posición automáticamente elimina la orden pendiente contraria y almacena los niveles de protección previstos.

## Parámetros
| Parámetro | Descripción | Predeterminado |
| --- | --- | --- |
| `RsiPeriod` | Longitud de suavizado del RSI. | 8 |
| `BandsPeriod` | Período de Bollinger sobre RSI. | 14 |
| `BandsDeviation` | Multiplicador de desviación estándar para Bollinger sobre RSI. | 1.0 |
| `SarStep` | Paso de aceleración Parabolic SAR. | 0.003 |
| `SarMax` | Aceleración máxima Parabolic SAR. | 0.2 |
| `TakeProfitPips` | Distancia take profit en pips. | 50 |
| `StopLossPips` | Distancia stop loss en pips. | 135 |
| `IndentPips` | Desplazamiento más allá de un fractal antes de colocar la orden stop. | 15 |
| `RsiUpper` | Umbral RSI que cancela sell stops. | 70 |
| `RsiLower` | Umbral RSI que cancela buy stops. | 30 |
| `SarTrailingPips` | Brecha mínima (en pips) entre el precio y el SAR antes del trailing. | 10 |
| `CandleType` | Tipo de datos / marco temporal para el procesamiento. | Velas de 4 horas |

## Notas
- La versión Python se omite intencionadamente, según se solicitó.
- Use `Volume` en la clase base para configurar el tamaño del lote (por defecto 1 si no se especifica).
- La estrategia debe ejecutarse en el mismo marco temporal que la configuración del EA original (EURUSD H4 según el archivo `.set` proporcionado).
