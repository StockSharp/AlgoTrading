# Estrategia de Trend Line
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
La estrategia Trend Line replica la lógica central de gestión de trades del experto MetaTrader original combinando una media móvil ponderada linealmente rápida y lenta, un filtro de Momentum y una confirmación MACD. La conversión se centra en componentes StockSharp de alto nivel y mantiene el mismo enfoque sistemático que espera impulsos de Momentum en la dirección de la tendencia antes de entrar. Los stops de protección, los objetivos de beneficio y un stop de seguimiento opcional en pasos de precio proporcionan una gestión de riesgos similar al experto original.

## Lógica de trading
1. Suscribirse a la serie de velas configurada y calcular los siguientes indicadores:
   - Media móvil ponderada linealmente (LWMA) rápida con el `FastMaPeriod` configurable.
   - LWMA lenta con el `SlowMaPeriod` configurable.
   - Indicador Momentum con período `MomentumPeriod`. Las tres lecturas de Momentum más recientes se rastrean para emular la verificación de Momentum de múltiples barras presente en la versión MQL.
   - Indicador de Convergencia/Divergencia de Medias Móviles (MACD) con longitudes estándar de rápida/lenta/señal. La estrategia almacena los valores de MACD y señal para uso posterior.
2. Entrar largo cuando:
   - La LWMA rápida está por encima de la LWMA lenta.
   - Al menos uno de los últimos tres valores de Momentum es mayor o igual a `MomentumBuyThreshold`.
   - La línea principal MACD está por encima de la línea de señal MACD.
   - No existe ninguna posición corta abierta (la exposición corta se aplana antes de abrir una posición larga).
3. Entrar corto cuando:
   - La LWMA rápida está por debajo de la LWMA lenta.
   - Al menos uno de los últimos tres valores de Momentum es menor o igual a `MomentumSellThreshold` (el umbral debe ser negativo para detectar aceleración descendente).
   - La línea principal MACD está por debajo de la línea de señal MACD.
   - No existe ninguna posición larga abierta (la exposición larga se aplana antes de abrir una posición corta).
4. Después de cada entrada, la estrategia coloca órdenes protectoras de stop-loss y take-profit por distancia en pasos de precio. Ambas órdenes se recalculan cada vez que cambia la posición.
5. Se puede activar un stop de seguimiento con `TrailingStopSteps` y `TrailingTriggerSteps`. Una vez que la posición abierta gana al menos la distancia de disparo, el stop-loss se mueve a `TrailingStopSteps` del precio de cierre actual de la vela procesada.

## Parámetros
- `CandleType` – tipo de datos para la serie de velas utilizada por cada indicador (temporalidad de 1 hora por defecto).
- `FastMaPeriod` – período de la LWMA rápida (por defecto 6).
- `SlowMaPeriod` – período de la LWMA lenta (por defecto 85).
- `MomentumPeriod` – número de velas para el cálculo de Momentum (por defecto 14).
- `MomentumBuyThreshold` – Momentum positivo mínimo necesario para permitir nuevas posiciones largas (por defecto 0.3).
- `MomentumSellThreshold` – Momentum máximo (negativo) permitido antes de abrir nuevas posiciones cortas (por defecto -0.3).
- `MacdFastLength` – longitud EMA rápida del MACD (por defecto 12).
- `MacdSlowLength` – longitud EMA lenta del MACD (por defecto 26).
- `MacdSignalLength` – longitud EMA de señal del MACD (por defecto 9).
- `StopLossSteps` – distancia de stop de protección expresada en pasos del instrumento (por defecto 20).
- `TakeProfitSteps` – distancia del objetivo de beneficio de protección en pasos (por defecto 50).
- `TrailingStopSteps` – distancia del stop de seguimiento en pasos (por defecto 40, desactivado cuando es cero).
- `TrailingTriggerSteps` – beneficio en pasos requerido antes de que el stop de seguimiento se active (por defecto 40).

## Notas
- Los vínculos de indicadores se basan solo en velas terminadas; los datos no terminados se ignoran para evitar señales prematuras.
- `SetStopLoss` y `SetTakeProfit` trabajan con distancias en pasos de precio, lo que mantiene el comportamiento consistente en instrumentos con diferentes tamaños de tick.
- Cuando `MomentumSellThreshold` se mantiene positivo, la comparación predeterminada (`<= threshold`) espera que ese valor sea negativo. Ajuste el signo al optimizar la estrategia.
- El stop de seguimiento funciona en modo de fin de barra porque se actualiza cuando se procesa cada vela terminada, reflejando el script original que recalculaba los stops en nuevas barras.
- La conversión omite intencionalmente el dibujo manual de líneas de tendencia y las reglas de liquidación basadas en capital porque dependían de características interactivas de la terminal no disponibles en StockSharp. Se preservan todas las demás reglas centrales de entrada y riesgo.
