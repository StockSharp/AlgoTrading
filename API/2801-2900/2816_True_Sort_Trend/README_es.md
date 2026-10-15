# Estrategia True Sort Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el clásico template "True Sort" de MetaTrader esperando a que cinco medias móviles exponenciales se alineen en orden estricto. Cuando tanto la vela actual como la anterior completadas respetan el mismo ordenamiento alcista o bajista y el Índice de Dirección Promedio (ADX) confirma el momentum, la estrategia abre una posición en la dirección de la tendencia. El riesgo se controla mediante distancias opcionales absolutas de stop-loss y take-profit junto con un trailing stop que solo se activa después de que el precio se mueva lo suficiente a favor de la operación.

## Cómo funciona

1. Construir cinco EMAs (de rápida a lenta: predeterminado 10, 20, 50, 100, 200 períodos) en la serie de velas seleccionada.
2. Calcular el ADX con un período configurable (predeterminado 24) para calificar si la tendencia tiene suficiente fuerza (umbral predeterminado 20).
3. Solo en el momento en que se cierra una vela analizamos los indicadores. Las señales se ignoran para velas no terminadas para evitar decisiones prematuras.
4. Una configuración larga requiere lo siguiente para la vela completada **actual** y la **anterior**:
   - `EMA_rápida > EMA_2 > EMA_3 > EMA_4 > EMA_lenta` (alineación alcista perfecta).
   - `ADX > umbral` para asegurarse de que la pendiente es significativa.
5. Una configuración corta refleja lo anterior con todas las desigualdades invertidas.
6. Las posiciones se cierran cuando el orden se rompe, cuando se alcanzan los niveles protectores, o cuando el trailing stop devuelve una cantidad configurable de ganancia.

Esta lógica mantiene la estrategia estrictamente en mercados de fuerte tendencia y fuerza la alineación en dos barras para reducir el ruido.

## Reglas de trading

- **Entrada**
  - **Largo**: ADX mayor que el umbral y cinco EMAs ordenadas de más rápida a más lenta para tanto la vela actual como la anterior finalizada. Primero se cierra cualquier posición corta abierta, luego se abre un nuevo largo con el `Volume` configurado.
  - **Corto**: ADX mayor que el umbral y EMAs ordenadas en orden descendente durante dos velas consecutivas. Cualquier posición larga se aplana antes de enviar la entrada corta.
- **Salida**
  - Si el orden de la EMA pierde su ordenamiento estricto, la posición se cierra inmediatamente.
  - Salidas protectoras opcionales:
    - Distancia de stop-loss en unidades de precio absoluto por debajo (largo) o por encima (corto) del precio de entrada.
    - Distancia de take-profit en unidades de precio absoluto más allá del precio de entrada.
    - Trailing stop que se activa solo después de que el precio avance `TrailingStopDistance + TrailingStepDistance` y luego sigue al precio en `TrailingStopDistance`.
  - Los cierres manuales o las ejecuciones externas también restablecerán el estado interno.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `CandleType` | Tipo de datos de velas usadas para todos los cálculos. | Marco temporal de 1 hora |
| `FastEmaLength` | Período de la EMA más rápida (alineación de entrada). | 10 |
| `SecondEmaLength` | Período de la segunda EMA. | 20 |
| `ThirdEmaLength` | Período de la tercera EMA. | 50 |
| `FourthEmaLength` | Período de la cuarta EMA. | 100 |
| `SlowEmaLength` | Período de la EMA más lenta que representa la tendencia a largo plazo. | 200 |
| `AdxPeriod` | Longitud de promedio para el indicador ADX. | 24 |
| `AdxThreshold` | Valor mínimo de ADX requerido para permitir trades. | 20 |
| `StopLossDistance` | Distancia de precio absoluta del stop protector (0 deshabilita). | 0.005 |
| `TakeProfitDistance` | Distancia de precio absoluta del objetivo de ganancia (0 deshabilita). | 0.015 |
| `TrailingStopDistance` | Distancia entre el precio más alto/bajo y la salida por trailing. | 0.0005 |
| `TrailingStepDistance` | Avance extra necesario antes de que el trailing stop se active o mueva. | 0.0001 |

Todos los valores de distancia se expresan en unidades de precio. Para símbolos FX cotizados con cuatro o cinco decimales, valores como `0.005` corresponden aproximadamente a 50 pips. Ajuste los números para que coincidan con el tamaño del tick del instrumento negociado.

## Notas y consejos

- Funciona mejor en instrumentos con tendencia como los principales pares de FX o índices en marcos temporales intradía o swing. Aumente las longitudes de EMA para barras diarias o acórtelas para scalping.
- La confirmación de dos velas reduce drásticamente los whipsaws pero puede causar entradas tardías. Considere optimizar el umbral de ADX y las longitudes de EMA para su mercado.
- Los trailing stops permanecen inactivos hasta que el precio se mueve `TrailingStopDistance + TrailingStepDistance` desde la entrada. Establecer el paso en cero imita el comportamiento de MetaTrader donde el trailing comienza una vez que el precio recorre la distancia base.
- La estrategia depende de órdenes de mercado (`BuyMarket`, `SellMarket`). Configure la propiedad `Volume` de la instancia de estrategia para controlar el dimensionamiento de posición o integrar con la gestión de dinero del portafolio si es necesario.
- Combine con filtros de sesión o confirmación de marco temporal superior si necesita limitar las horas de trading.
