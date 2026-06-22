# Estrategia Polish Layer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Polish Layer** es una conversión del asesor experto de MetaTrader de `MQL/17484` a la API de alto nivel de StockSharp. Apunta a la continuación de tendencia a corto plazo en pares forex usando velas de 5 o 15 minutos. La dirección de la tendencia se define por la relación entre medias móviles exponenciales rápida y lenta y el momentum reciente del Índice de Fuerza Relativa (RSI). La confirmación de entrada requiere señales sincronizadas del Oscilador Estocástico, DeMarker y Williams %R.

## Indicadores
- **Media Móvil Exponencial (EMA)** – filtros de tendencia rápido (`ShortEmaPeriod`) y lento (`LongEmaPeriod`).
- **Índice de Fuerza Relativa (RSI)** – filtro de pendiente de momentum derivado de los valores de velas previas.
- **Oscilador Estocástico** – detecta reversiones de sobrecompra/sobreventa mediante cruces de umbral %K.
- **DeMarker** – confirma fases de acumulación/distribución.
- **Williams %R** – valida reversiones de momentum en niveles extremos.

## Parámetros
| Parámetro | Valor predeterminado | Descripción |
|-----------|---------|-------------|
| `ShortEmaPeriod` | 9 | Longitud del filtro de tendencia EMA rápida. |
| `LongEmaPeriod` | 45 | Longitud del filtro de tendencia EMA lenta. |
| `RsiPeriod` | 14 | Retroceso RSI usado para comparación de pendiente de momentum. |
| `StochasticKPeriod` | 5 | Retroceso de la línea %K. |
| `StochasticDPeriod` | 3 | Período de suavizado para %D. |
| `StochasticSlowing` | 3 | Factor de desaceleración final aplicado a %K. |
| `WilliamsRPeriod` | 14 | Ventana de retroceso de Williams %R. |
| `DeMarkerPeriod` | 14 | Ventana de retroceso de DeMarker. |
| `TakeProfitPoints` | 17 | Distancia al objetivo de beneficio en puntos de precio (usa `Security.PriceStep`). |
| `StopLossPoints` | 77 | Distancia al stop protector en puntos de precio. |
| `CandleType` | 5 minutos | Tipo de datos de vela procesado por la estrategia. |
| `Volume` | 1 | Tamaño de operación usado para entradas a mercado. |

## Lógica de trading
1. **Filtro de tendencia** – la vela anterior debe mostrar la EMA rápida por encima de la EMA lenta y el RSI subiendo (RSI anterior > RSI de dos barras atrás) para escenarios largos. La configuración inversa define escenarios cortos.
2. **Confirmación del oscilador** – las entradas solo se consideran cuando la estrategia está plana y se cumplen todas las condiciones siguientes:
   - **Estocástico %K** cruza por encima de 19 para largos o por debajo de 81 para cortos.
   - **DeMarker** cruza por encima de 0.35 para largos o por debajo de 0.63 para cortos.
   - **Williams %R** cruza por encima de -81 para largos o por debajo de -19 para cortos.
3. **Ejecución de órdenes** – la estrategia envía órdenes de mercado usando `BuyMarket(Volume)` o `SellMarket(Volume)` y se basa en `StartProtection` para adjuntar automáticamente los offsets de stop-loss y take-profit.

## Gestión de riesgo
- Las órdenes protectoras se crean vía `StartProtection`, transformando `TakeProfitPoints` y `StopLossPoints` en distancias absolutas de precio basadas en el instrumento `PriceStep`.
- El algoritmo permanece fuera del mercado hasta que las posiciones existentes son cerradas por las órdenes protectoras, reflejando el comportamiento del asesor experto original.

## Notas de uso
- Funciona mejor en pares forex líquidos con velas de 5 o 15 minutos.
- Asegurarse de que los metadatos del instrumento contengan un `PriceStep` válido; de lo contrario, ajustar `TakeProfitPoints` y `StopLossPoints` para que coincidan con el tamaño de tick del instrumento.
- Considerar pruebas hacia adelante antes del despliegue en vivo porque la secuencia de confirmación es sensible al suavizado de indicadores y los incrementos de precios del broker.
