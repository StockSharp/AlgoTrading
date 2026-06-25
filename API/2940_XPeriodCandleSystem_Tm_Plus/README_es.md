# Estrategia XPeriod Candle System TM Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port de StockSharp del asesor experto de MetaTrader `Exp_XPeriodCandleSystem_Tm_Plus`. El robot original se basa en el indicador personalizado *XPeriod Candle System*, que suaviza los datos de velas y colorea las barras según las rupturas de las Bandas de Bollinger. La versión traducida reproduce este comportamiento aplicando suavizado exponencial a las series OHLC, mapeando los mismos modos de precio aplicado, y ejecutando operaciones a partir de los estados de color resultantes. Una salida basada en tiempo y órdenes de protección configurables complementan la lógica de ruptura.

## Lógica de trading

1. **Velas suavizadas** – Las medias móviles exponenciales con longitud configurable construyen valores sintéticos de apertura, máximo, mínimo y cierre que aproximan el indicador fuente.
2. **Precio aplicado** – El usuario puede seleccionar cualquiera de las doce fórmulas de precio (cierre, apertura, mediana, variaciones de seguimiento de tendencia, Demark, etc.) antes de alimentar los datos en las Bandas de Bollinger.
3. **Análisis de bandas** – Un indicador de Bandas de Bollinger (longitud y desviación configurables) procesa la serie de precios suavizados. Se requieren bandas finalizadas antes de evaluar las señales.
4. **Estados de color** –
   - Barra alcista por encima de la banda superior → color `0` (ruptura al alza).
   - Barra bajista por debajo de la banda inferior → color `4` (ruptura a la baja).
   - Otras barras alcistas → color `1`; otras barras bajistas → color `3`.
   - Un desplazamiento de ruptura configurable (convertido a unidades de precio usando el tamaño de tick del símbolo cuando es posible) evita falsos disparadores.
5. **Entradas** – La estrategia analiza la vela definida por `SignalBar` y su predecesora:
   - Abrir largo cuando la barra anterior fue una ruptura alcista (`0`) y la barra de señal no lo es.
   - Abrir corto cuando la barra anterior fue una ruptura bajista (`4`) y la barra de señal no lo es.
6. **Salidas** –
   - Cerrar largos cuando la barra de referencia es bajista (`> 2`).
   - Cerrar cortos cuando la barra de referencia es alcista (`< 2`).
   - Un temporizador de mantenimiento opcional (`TimeTrade` y `HoldingMinutes`) cierra posiciones tras los minutos especificados.
7. **Riesgo** – `StartProtection` despliega distancias absolutas opcionales de take-profit y stop-loss para cada operación.

## Parámetros

| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|----------------------|
| `OrderVolume` | Tamaño de orden base utilizado para entradas de mercado. | 0.1 |
| `BuyPosOpen` / `SellPosOpen` | Habilitar/deshabilitar entradas largas o cortas. | `true` |
| `BuyPosClose` / `SellPosClose` | Permitir salidas de posiciones largas o cortas. | `true` |
| `TimeTrade` | Activa el filtro de salida basado en tiempo. | `true` |
| `HoldingMinutes` | Tiempo máximo de mantenimiento antes de que el filtro de tiempo cierre una posición. | 960 |
| `CandleType` | Tipo de datos de velas (marco temporal) solicitado del mercado. | 4 horas |
| `Period` | Longitud de las medias móviles exponenciales de suavizado. | 5 |
| `BollingerLength` | Número de barras suavizadas dentro de la ventana de cálculo de Bollinger. | 20 |
| `BandsDeviation` | Multiplicador del ancho de banda. | 1.001 |
| `AppliedPriceMode` | Transformación de precio usada antes del indicador Bollinger (cierre, apertura, mediana, seguimiento de tendencia, Demark, etc.). | Close |
| `SignalBar` | Índice de la barra usada para evaluar señales (1 = última barra cerrada). | 1 |
| `StopLoss` / `TakeProfit` | Distancias absolutas (en unidades de precio) utilizadas por el motor de protección. | 1000 / 2000 |
| `Deviation` | Desplazamiento de ruptura extra añadido por encima/debajo de las Bandas de Bollinger. | 10 |

## Notas de uso

- El paso de suavizado usa medias móviles exponenciales para replicar el cálculo propietario de XPeriod. Períodos más pequeños mantienen las velas sintéticas más cercanas a los precios de mercado, mientras que períodos más grandes enfatizan la estructura de tendencia.
- `SignalBar` debe permanecer dentro del historial almacenado (hasta 14 posiciones después de la barra actual). Valores mayores al historial disponible omitirán automáticamente el trading.
- El desplazamiento de ruptura se multiplica por `PriceStep` cuando el valor expone un tamaño de tick. Esto mantiene el comportamiento similar a la versión MetaTrader donde `Deviation` se define en puntos.
- `StopLoss` y `TakeProfit` se especifican en unidades absolutas de precio. Establézcalos en cero para deshabilitar las órdenes de protección manteniendo activa la infraestructura de gestión.
- Aún no se proporciona traducción en Python; esta carpeta contiene únicamente la implementación en C#.
