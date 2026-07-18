# Estrategia de Gann Line
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica las ideas centrales del asesor experto MetaTrader 4 "Gann Line" (ID de origen 24877) usando la API de alto nivel de StockSharp. Mantiene los mismos filtros de tendencia, Momentum y MACD a largo plazo mientras expresa todas las herramientas de gestión monetaria en **pasos de precio**, lo que hace que la lógica sea independiente del broker.

## Lógica de trading

1. **Filtro de tendencia (temporalidad principal)**
   - Se aplican dos medias móviles ponderadas linealmente (LWMA) al precio típico de la vela (high + low + close) / 3.
   - Un sesgo largo requiere que la LWMA rápida cierre por encima de la LWMA lenta; un sesgo corto requiere lo contrario.
2. **Confirmación de Momentum (temporalidad superior)**
   - Un oscilador Momentum calculado en una temporalidad superior configurable verifica cuánto se desvía el oscilador del nivel de equilibrio (100).
   - Al menos uno de los últimos tres valores de Momentum terminados debe superar el umbral de desviación configurado antes de que se permita cualquier trade.
3. **Filtro MACD lento (temporalidad muy alta)**
   - Un filtro MACD calculado en una temporalidad lenta (mensual por defecto) debe confirmar la dirección: la línea principal MACD por encima de la señal para largos, por debajo para cortos.
4. **Gestión de posiciones**
   - Los objetivos fijos de stop-loss y take-profit se convierten de pasos de precio a precios absolutos cuando se abre un trade.
   - La lógica de punto de equilibrio opcional mueve el stop al precio de entrada más un offset una vez que el trade ha avanzado una cantidad determinada de pasos en beneficio.
   - La lógica de trailing opcional desplaza el stop detrás del máximo más alto (para largos) o del mínimo más bajo (para cortos) una vez que el precio ha recorrido un número configurable de pasos.

## Gestión de riesgos

- Todas las distancias (stop-loss, take-profit, punto de equilibrio y trailing) se ingresan en **pasos** de precio. El helper los convierte a precios usando el `PriceStep` del instrumento.
- La estrategia trabaja con la propiedad base `Volume`. Si es cero, se usa un contrato/lote por defecto.
- Solo se gestiona una posición neta única. Las señales opuestas cierran el trade actual antes de abrir uno nuevo.

## Diferencias respecto a la versión MQL4

- El asesor experto original dependía de una línea de tendencia Gann dibujada manualmente. StockSharp no expone objetos del gráfico, por lo que el puerto reemplaza esa verificación con la confirmación de pendiente LWMA.
- El trailing basado en dinero, los cierres parciales y las verificaciones de capital de toda la cuenta del script se simplifican en cálculos deterministas basados en pasos.
- Las notificaciones (alertas, correos electrónicos, pushes móviles) no se generan porque las estrategias de StockSharp típicamente registran en la salida de la plataforma.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `Fast LWMA` | Longitud de la LWMA rápida utilizada para el filtro de tendencia. |
| `Slow LWMA` | Longitud de la LWMA lenta utilizada para el filtro de tendencia. |
| `Momentum Period` | Retrospectiva del oscilador Momentum en la temporalidad secundaria. |
| `Momentum Threshold` | Desviación mínima de 100 requerida por cualquiera de los últimos tres valores de Momentum. |
| `MACD Fast / Slow / Signal` | Longitudes EMA del filtro MACD lento. |
| `Take Profit (steps)` | Distancia de take-profit en pasos de precio. |
| `Stop Loss (steps)` | Distancia de stop-loss en pasos de precio. |
| `Use Trailing`, `Trail Activation`, `Trail Distance` | Habilitar trailing, beneficio necesario antes de que comience el trailing, y distancia entre el extremo de precio y el stop de trailing. |
| `Use BreakEven`, `BreakEven Activation`, `BreakEven Offset` | Habilitar punto de equilibrio, beneficio requerido antes de mover el stop, y beneficio adicional bloqueado después. |
| `Primary Timeframe` | Tipo de vela utilizado por el cruce LWMA. |
| `Momentum Timeframe` | Tipo de vela enviado al oscilador Momentum. |
| `MACD Timeframe` | Tipo de vela enviado al filtro MACD lento. |

## Consejos de uso

1. Seleccione un instrumento y establezca el `Primary Timeframe` deseado. Las otras temporalidades tienen por defecto 1 hora (Momentum) y 30 días (MACD), pero se pueden personalizar para reproducir el mapeo de coeficientes original.
2. Configure `Volume` y los parámetros de riesgo basados en pasos para que coincidan con las especificaciones del contrato de su broker.
3. Ejecute la estrategia en `Designer` o a través de código. Monitoree el registro para verificar que los filtros, los movimientos de punto de equilibrio y los ajustes de trailing aparezcan como se espera.
4. Optimice los umbrales de Momentum y MACD para adaptar la lógica portada a diferentes mercados o temporalidades.

## Mejoras adicionales

- Integrar un stop global basado en capital similar al script original.
- Reemplazar el filtro de pendiente LWMA con una línea de tendencia personalizada dibujada en el gráfico una vez que StockSharp exponga eventos de objetos.
- Agregar toma de beneficios parcial para imitar el comportamiento de múltiples objetivos de la versión MQL4.
