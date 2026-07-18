# Estrategia AMA Trader v2.1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia AMA Trader v2.1 es una conversión del asesor experto MetaTrader 4 **AMA_TRADER_v2_1.mq4** que combina las ráfagas de media móvil adaptativa (AMA) de Kaufman con un filtro Heiken Ashi de doble suavizado y comprobaciones de impulso RSI.

## Lógica principal

1. **Filtro de tendencia adaptativo**: un motor AMA personalizado reproduce el indicador original, incluidas las constantes rápidas/lentas, la relación de eficiencia y el parámetro de potencia. El algoritmo busca ráfagas de impulso en las que el valor de AMA salta más de `AmaThreshold` pasos de precio en comparación con la barra anterior.
2. **Confirmación de Heiken Ashi**: las velas de precios se suavizan dos veces: primero mediante un promedio móvil configurable en los precios brutos de OHLC, luego mediante un segundo promedio móvil en los buffers de Heiken Ashi. Una barra suavizada alcista (cerrada sobre abierta) permite operaciones largas, mientras que una barra bajista permite operaciones cortas.
3. **RSI Comprobación de impulso**: un RSI clásico con período configurable confirma el impulso: las posiciones largas requieren que el RSI retroceda desde un valor anterior mientras se mantiene por debajo de 70, las posiciones cortas requieren un rebote mientras el oscilador permanece por encima de 30.
4. **Gestión de posiciones**: la estrategia abre una única posición a la vez, aplica distancias opcionales de stop-loss y take-profit (en pasos de precio) y puede seguir el stop una vez que el precio se mueve en la dirección comercial. Cuando RSI cruza los extremos 70/30, se realiza un cierre parcial opcional antes de que se produzca una salida completa en el siguiente cruce.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | velas de 15 minutos | Plazo para todos los cálculos. |
| `TradeVolume` | 0.1 | Volumen de orden de mercado base. |
| `AmaLength` | 9 | Lookback utilizado por la media móvil adaptativa. |
| `AmaFastPeriod` | 2 | Constante rápida en barras para el suavizado AMA. |
| `AmaSlowPeriod` | 30 | Constante lenta en barras para el suavizado de AMA. |
| `AmaPower` | 2 | Exponente aplicado a la constante de suavizado (coincide con `G` en el código MQ4). |
| `AmaThreshold` | 2 pasos | Cambio mínimo de AMA (en pasos de precio) para activar una señal. |
| `FirstMaMethod` | alisado | Primer método de alisado para la construcción Heiken Ashi. |
| `FirstMaPeriod` | 6 | Longitud de la primera media móvil de suavizado. |
| `SecondMaMethod` | Lineal ponderado | Segundo método de suavizado aplicado a los buffers Heiken Ashi. |
| `SecondMaPeriod` | 2 | Longitud de la segunda media móvil de suavizado. |
| `RsiPeriod` | 14 | RSI período utilizado por el filtro de impulso. |
| `PartialClosePercent` | 70% | Parte de la posición activa que se cerrará cuando RSI cruce un extremo. Establezca en `0` para desactivar. |
| `StopLossSteps` | 50 | Distancia de stop-loss expresada en pasos de precio del instrumento. Establezca en `0` para desactivar. |
| `TakeProfitSteps` | 100 | Distancia de obtención de beneficios expresada en incrementos de precio. Establezca en `0` para desactivar. |
| `TrailingSteps` | 30 | Distancia del trailing stop en pasos de precio. Establezca en `0` para deshabilitar el seguimiento. |

## Reglas de trading

- **Entrada larga**: cuando el salto de AMA es positivo y supera `AmaThreshold`, la última vela Heiken Ashi suavizada es alcista y RSI retrocede (valor anterior mayor que el valor actual) mientras se mantiene en 70 o por debajo.
- **Entrada corta**: cuando el salto de AMA es negativo más allá de `AmaThreshold`, la vela Heiken Ashi suavizada es bajista y RSI aumenta (el valor anterior es menor que el actual) mientras se mantiene en 30 o por encima.
- **Cierre parcial**: si está habilitado, cierre `PartialClosePercent` de la posición cuando RSI cruce por encima de 70 (largos) o por debajo de 30 (cortos).
- **Salida completa**: cierre toda la posición en el extremo opuesto RSI, en stop-loss, take-profit o cuando se alcance el trailing stop.

La implementación utiliza el StockSharp API de alto nivel: una suscripción de vela alimenta la calculadora AMA personalizada, el canal de suavizado Heiken Ashi y el indicador RSI. Todos los comentarios en el código fuente están en inglés, lo que refleja los requisitos de las pautas de conversión.
