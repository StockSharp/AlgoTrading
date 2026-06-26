# Estrategia Ichimoku Momentum MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- **Tipo**: Seguimiento de tendencia con confirmación de Momentum.
- **Marco temporal**: Configurable (velas de 15 minutos por defecto).
- **Indicadores**: Ichimoku (Tenkan/Kijun), Medias Móviles Ponderadas Linealmente, Momentum, MACD.
- **Stops**: Take-profit y stop-loss fijos opcionales en puntos de precio a través de `StartProtection`.

## Descripción de la estrategia
Esta estrategia recrea el flujo de decisión del experto de MetaTrader "Ichimoku" (carpeta `MQL/23469`). Evalúa la vela cerrada anterior y abre nuevos trades al inicio de la siguiente barra cuando las cuatro confirmaciones están de acuerdo:

1. **Alineación Ichimoku** – Tenkan (línea de conversión) debe estar por encima de Kijun (línea base) para trades largos y por debajo para cortos.
2. **Filtro de tendencia LWMA** – Una media móvil ponderada lineal rápida debe mantenerse por encima de la LWMA lenta para largos y por debajo para cortos. Ambas medias se calculan en el mismo marco temporal que las velas suscritas.
3. **Fuerza del Momentum** – La distancia absoluta del oscilador de momentum desde el nivel neutro 100 debe ser mayor que un umbral configurable en al menos una de las últimas tres velas cerradas.
4. **Confirmación MACD** – El histograma MACD debe coincidir con la dirección (línea MACD posicionada más allá de la línea de señal con el mismo signo).

Cuando las cuatro condiciones se alinean alcistamente y la estrategia no está actualmente larga, compra el volumen configurado más las unidades necesarias para aplanar una posición corta existente. Cuando las condiciones cambian a bajistas, refleja el proceso en el lado de la venta. Las señales opuestas siempre cierran posiciones abiertas, proporcionando una salida determinista incluso sin órdenes protectoras.

La gestión de riesgos se maneja a través del `StartProtection` de StockSharp, permitiendo distancias fijas de take-profit y stop-loss expresadas en puntos del instrumento. Establecer cualquier parámetro en cero deshabilita el tramo de protección correspondiente.

## Descripción de parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `FastMaPeriod` | Longitud de la media móvil ponderada lineal rápida usada para el filtro de tendencia. |
| `SlowMaPeriod` | Longitud de la media móvil ponderada lineal lenta. |
| `MomentumPeriod` | Período de lookback del oscilador de momentum. |
| `MomentumThreshold` | Distancia mínima desde 100 que el momentum debe alcanzar en al menos una de las últimas tres velas. |
| `MacdFastPeriod` | Longitud EMA rápida del filtro MACD. |
| `MacdSlowPeriod` | Longitud EMA lenta del filtro MACD. |
| `MacdSignalPeriod` | Longitud EMA de señal del filtro MACD. |
| `TenkanPeriod` | Longitud del Ichimoku Tenkan-sen. |
| `KijunPeriod` | Longitud del Ichimoku Kijun-sen. |
| `SenkouSpanBPeriod` | Longitud del Ichimoku Senkou Span B. |
| `TakeProfitPoints` | Distancia de take-profit opcional en puntos de precio (0 deshabilita). |
| `StopLossPoints` | Distancia de stop-loss opcional en puntos de precio (0 deshabilita). |
| `CandleType` | Marco temporal usado para todos los cálculos de indicadores. |

## Notas de uso
- La estrategia lee solo velas finalizadas y almacena los valores de indicadores de la barra anterior, coincidiendo con la lógica `shift=1` del EA de MetaTrader.
- Ajustar `MomentumThreshold` al cambiar a mercados con diferente escala de momentum (p. ej., crypto vs. pares forex).
- Las órdenes protectoras se gestionan internamente; no se envían órdenes de corchete a nivel de exchange.
- Los gráficos, si están disponibles, mostrarán velas de precio, ambas LWMAs, la nube Ichimoku y los trades ejecutados.
