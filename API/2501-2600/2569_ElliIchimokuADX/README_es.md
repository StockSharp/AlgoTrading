# Estrategia Elli Ichimoku ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia es un port en C# del experto MetaTrader 5 "Elli" (edición de barabashkakvn). Combina la estructura de Ichimoku Kinko Hyo con un filtro de ruptura del Average Directional Index (+DI). Las operaciones se abren solo cuando un fuerte impulso direccional es confirmado simultáneamente por la alineación de líneas de Ichimoku y un repentino aumento en el índice de dirección positiva.

La implementación de StockSharp mantiene el comportamiento original de trabajar con dos flujos de velas: el análisis de Ichimoku se realiza en un marco temporal superior (predeterminado 1 hora) mientras que ADX se evalúa en una serie más rápida (predeterminado 1 minuto). Las órdenes se ingresan con un stop protector fijo y objetivo medidos en pasos de precio, idénticos al asesor experto original.

## Indicadores y datos
- **Ichimoku** (Tenkan 19, Kijun 60, Senkou Span B 120 por defecto).
- **Average Directional Index (ADX)**, solo se usa la línea +DI como en el código fuente.
- Las áreas de gráfico opcionales muestran la serie de velas, la nube de Ichimoku y la línea ADX.

Se crean dos suscripciones de velas independientes:
1. `IchimokuCandleType` (predeterminado 1 hora) – impulsa los cálculos de Ichimoku y genera decisiones de trading.
2. `AdxCandleType` (predeterminado 1 minuto) – alimenta el indicador ADX y suministra valores +DI actuales/anteriores.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `TakeProfitPoints` | 60 | Distancia de take profit en pasos de precio. Establecer en 0 para deshabilitar. |
| `StopLossPoints` | 30 | Distancia de stop loss en pasos de precio. Establecer en 0 para deshabilitar. |
| `TenkanPeriod` | 19 | Longitud de la línea Tenkan-sen (línea de conversión) de Ichimoku. |
| `KijunPeriod` | 60 | Longitud de la línea Kijun-sen (línea base) de Ichimoku. |
| `SenkouSpanBPeriod` | 120 | Longitud de la línea Senkou Span B de Ichimoku. |
| `AdxPeriod` | 10 | Período para el indicador ADX. |
| `PlusDiHighThreshold` | 13 | Umbral que el valor actual +DI debe superar. |
| `PlusDiLowThreshold` | 6 | Umbral que el valor anterior +DI debe permanecer por debajo. |
| `BaselineDistanceThreshold` | 20 | Separación mínima entre Tenkan/Kijun (en pasos de precio) requerida para confirmar momentum. |
| `IchimokuCandleType` | velas de 1 hora | Serie de velas usada para la evaluación de Ichimoku. |
| `AdxCandleType` | velas de 1 minuto | Serie de velas usada para el cálculo de ADX. |

## Lógica de trading
1. Esperar una vela de Ichimoku terminada.
2. Asegurarse de que ADX tenga al menos dos valores terminados y la última lectura produjo una ruptura +DI (`+DI anterior < PlusDiLowThreshold` y `+DI actual > PlusDiHighThreshold`).
3. Convertir la separación Tenkan/Kijun en pasos de precio y verificar que supere `BaselineDistanceThreshold`.
4. Todas las órdenes se bloquean si ya existe una posición abierta.
5. **Comprar** cuando:
   - Tenkan > Kijun.
   - Kijun > Senkou Span A.
   - Senkou Span A > Senkou Span B (nube alcista).
   - Precio de cierre > Kijun.
6. **Vender** cuando se observa la alineación inversa (Tenkan < Kijun < Senkou Span A < Senkou Span B y el cierre está por debajo de Kijun).
7. Las salidas de posición dependen del stop protector y el objetivo configurados mediante `StartProtection`. No se activa ninguna salida discrecional; esto refleja el EA original que esperaba stops/objetivos o intervención manual.

## Gestión de riesgos
`StartProtection` se llama una vez al inicio. Si el stop o el objetivo es cero, se omite la protección respectiva. Las órdenes se envían con ejecución de mercado (`BuyMarket`/`SellMarket`), coincidiendo con la implementación MQL que usaba órdenes de mercado con SL/TP adjuntos.

## Notas de implementación
- Solo se usa el índice de dirección positiva para señales largas y cortas, replicando la lógica del código MQL5 (el autor original comentó la rama -DI).
- La estrategia no rastrea la línea Chikou explícitamente; en cambio, la alineación de la nube se valida comparando Senkou Span A y B.
- Los campos internos almacenan los últimos dos valores +DI sin llamar a `GetValue`, de acuerdo con las pautas de la API de alto nivel.
- Si ambos parámetros de vela son idénticos, se reutiliza una única suscripción para Ichimoku y ADX para reducir la sobrecarga.

## Consejos de uso
- Mantener `AdxCandleType` más rápido que `IchimokuCandleType` para emular la versión MT5 (p.ej., ADX M1 vs. Ichimoku H1).
- Aumentar `BaselineDistanceThreshold` en instrumentos de alta volatilidad para exigir una mayor separación Tenkan/Kijun.
- Dado que el experto abre solo una posición a la vez, combinar la estrategia con controles de riesgo a nivel de portafolio cuando se operan múltiples símbolos.
