# Estrategia de BladeRunner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia BladeRunner es una traducción del asesor experto de MetaTrader que combina rupturas de fractales con confirmación de tendencia y momentum. El port de StockSharp mantiene la estructura multi-timeframe del script original analizando tres fuentes de velas diferentes: una serie primaria para la ejecución de operaciones, una serie de marco temporal superior para el filtro de momentum, y una serie lenta para el filtro de tendencia MACD. Las órdenes se abren con escala configurable, stop-loss y distancias de take-profit expresadas en pasos de precio.

## Lógica de trading
1. **Filtro de ruptura de fractal** – la estrategia escanea velas completadas en busca de patrones de fractal de Bill Williams. Se acepta un fractal alcista (superior) cuando la vela formada dos barras antes hace un nuevo máximo de swing y la barra de confirmación abre por debajo del precio del fractal y la LWMA de 20 períodos del precio típico. Los fractales bajistas aplican las reglas simétricas.
2. **Confirmación de tendencia** – las medias móviles ponderadas linealmente (LWMA) rápida y lenta calculadas en la serie de velas primaria definen la tendencia subyacente. Los longs requieren que la LWMA rápida esté por encima de la lenta, mientras que los shorts requieren la alineación opuesta.
3. **Filtro de momentum** – un oscilador de momentum calculado en la corriente de velas de marco temporal superior debe desviarse de 100 al menos por el umbral configurado en cualquiera de las tres últimas observaciones. Esto reproduce las verificaciones de spike de momentum de la versión MQL.
4. **Filtro MACD** – un MACD calculado en el marco temporal lento debe tener su línea principal por encima (long) o por debajo (short) de la línea de señal, reflejando el filtro mensual utilizado por el asesor experto.
5. **Confirmación de ruptura** – el cierre de la vela primaria más reciente debe romper más allá del nivel fractal almacenado antes de que se envíe la orden.

Cuando todos los filtros se alinean, la estrategia abre una posición de mercado usando el tamaño de lote configurado. La exposición existente en la dirección opuesta se cierra antes de revertir. Se permiten entradas adicionales hasta que se alcanza el número máximo de operaciones de escala.

## Detalles de implementación
- Tres suscripciones de velas se crean a través de la API de alto nivel. Cada fuente se vincula directamente a los indicadores requeridos sin añadirlos a la colección global de indicadores.
- Las LWMAs operan sobre el precio típico (HLC/3) para coincidir con la implementación MQL. El MACD también consume precios típicos.
- La detección de fractales almacena una ventana deslizante de velas completadas y valores de filtro asociados. Solo se mantiene la dirección de fractal validada más reciente, lo que evita señales duplicadas en la misma estructura.
- El historial de momentum se mantiene como un array de tamaño fijo, evitando asignaciones dinámicas mientras se reproduce el look-back del EA original.
- El dimensionamiento de órdenes respeta las restricciones de intercambio a través de ajustes de paso de volumen, volumen mínimo y máximo.
- El helper integrado `StartProtection` aplica distancias de stop-loss y take-profit expresadas en pasos de precio, coincidiendo con los valores de pip fijos de MetaTrader.

## Parámetros
| Nombre | Descripción | Por defecto |
| --- | --- | --- |
| `CandleType` | Serie de velas primaria utilizada para la generación de señales. | Velas de 15 minutos |
| `MomentumCandleType` | Serie de marco temporal superior para el filtro de momentum. | Velas de 1 hora |
| `MacdCandleType` | Serie de velas utilizada por el filtro de tendencia MACD. | Velas diarias |
| `FastMaPeriod` | Longitud de la LWMA rápida. | 6 |
| `SlowMaPeriod` | Longitud de la LWMA lenta. | 85 |
| `FilterMaPeriod` | LWMA utilizada para validar las rupturas de fractal. | 20 |
| `MomentumPeriod` | Período de promediado del indicador de momentum. | 14 |
| `MomentumThreshold` | Desviación absoluta mínima del momentum desde 100. | 0.3 |
| `FractalLookback` | Número de velas retenidas para el análisis de fractales. | 200 |
| `MaxTrades` | Número máximo de órdenes de escala por dirección. | 3 |
| `OrderVolume` | Volumen base para cada orden de mercado. | 1 contrato |
| `TakeProfitSteps` | Distancia de take-profit expresada en pasos de precio. | 50 |
| `StopLossSteps` | Distancia de stop-loss expresada en pasos de precio. | 20 |

## Gestión de riesgos
- Los niveles de stop-loss y take-profit se adjuntan automáticamente a cada posición a través de `StartProtection`.
- La estrategia siempre cierra la exposición opuesta antes de abrir operaciones en la nueva dirección para evitar situaciones de cobertura.
- El volumen se ajusta a las restricciones del instrumento antes de colocar órdenes. El límite `MaxTrades` limita los pasos de escala totales por dirección.

## Diferencias del EA original
- Las utilidades de stop de equity, trailing stop y break-even de MetaTrader no están implementadas. El control de riesgo de StockSharp se puede agregar externamente si es necesario.
- La lógica de trailing basada en dinero y las notificaciones push se omiten porque StockSharp proporciona flujos de trabajo de notificación alternativos.
- El filtro MACD usa velas diarias por defecto en lugar de barras mensuales. Ajuste `MacdCandleType` a un marco temporal mensual cuando sea compatible con la fuente de datos conectada.
- La validación de fractales depende de la última vela de confirmación almacenada en la ventana deslizante. Esto produce el mismo efecto práctico que el bucle en el script MQL mientras evita escaneos repetidos.

## Notas de uso
1. Configure los tipos de velas para que coincidan con los instrumentos y marcos temporales compatibles con su fuente de datos.
2. Alinee `OrderVolume`, `TakeProfitSteps` y `StopLossSteps` con el tamaño de tick y el paso de volumen del instrumento.
3. Ajuste `MomentumThreshold` y las longitudes de LWMA durante pruebas walk-forward para adaptar la sensibilidad de ruptura a diferentes mercados.
4. Habilite el dibujo en el gráfico para visualizar las tres LWMAs y verificar que las rupturas de fractal se alinean con los filtros de tendencia antes de operar en vivo.
