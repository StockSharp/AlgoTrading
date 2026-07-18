# Estrategia de FiboChannel Line
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia FiboChannel Line** es una conversión del asesor experto MetaTrader "FIBOCHANNEL". El robot original dependía de la dirección de un canal de Fibonacci dibujado manualmente, oscilaciones de Momentum en una temporalidad superior, y una combinación de medias móviles ponderadas linealmente y señales MACD. El puerto de StockSharp mantiene el mismo espíritu aprovechando vínculos de indicadores de alto nivel y gestión de riesgos integrada.

Ideas clave:

- Seguir la tendencia dominante usando un par de medias móviles ponderadas linealmente (LWMA).
- Confirmar picos de Momentum alrededor del nivel neutro del oscilador Momentum.
- Filtrar trades con la relación línea MACD vs. línea de señal.
- Verificar la pendiente de un canal de regresión lineal en lugar de leer objetos del gráfico.
- Gestionar posiciones mediante protección porcentual automática.

La estrategia funciona en cualquier instrumento que admita la agregación de velas. La temporalidad predeterminada son velas de 30 minutos, lo que proporciona un equilibrio entre la capacidad de respuesta y la estabilidad del indicador.

## Lógica de trading
1. **Filtro de tendencia** – cuando la LWMA rápida cierra por encima de la LWMA lenta, el mercado se considera alcista y solo se evalúan trades largos. Cuando está por debajo, solo se consideran cortos.
2. **Requisito de Momentum** – una ventana deslizante de las tres lecturas de Momentum más recientes debe mostrar que al menos un valor se desvió del nivel neutro 100 por el umbral configurado. Esto replica las verificaciones de fortaleza de Momentum de múltiples barras de la versión MQL.
3. **Filtro MACD** – los largos requieren que la línea MACD esté por encima de la línea de señal; los cortos requieren lo contrario.
4. **Dirección del canal** – la pendiente de regresión lineal debe ser positiva (para largos) o negativa (para cortos) más allá del `Slope Threshold`. Esto imita la validación del canal ascendente/descendente del experto original que comparaba puntos de anclaje de un objeto de canal de Fibonacci.
5. **Entradas y reversiones** – si todas las condiciones se alinean y no hay ninguna posición existente en esa dirección, la estrategia cancela las órdenes activas y envía una orden de mercado con tamaño `Volume + |Position|`. Esto permite reversiones suaves.
6. **Salidas** – si la dirección del canal o el filtro MACD deja de apoyar el trade abierto, la posición se cierra después de cancelar las órdenes pendientes. Adicionalmente, las reglas de stop-loss protector, take-profit y drawdown máximo se configuran a través de `StartProtection`.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `Candle Type` | Agregación de velas utilizada para todos los indicadores. | Marco temporal de 30 minutos |
| `Fast LWMA` | Longitud de la media móvil ponderada linealmente rápida. | 6 |
| `Slow LWMA` | Longitud de la media móvil ponderada linealmente lenta. | 85 |
| `Momentum Period` | Número de barras para el indicador Momentum. | 14 |
| `Momentum Threshold` | Desviación absoluta mínima de 100 requerida dentro del buffer de Momentum. | 0.3 |
| `Channel Length` | Barras utilizadas para calcular la pendiente de regresión lineal. | 50 |
| `Slope Threshold` | Valor mínimo de pendiente absoluta para confirmar la dirección de tendencia. | 0.0 |
| `MACD Fast` | Período EMA rápida dentro del cálculo MACD. | 12 |
| `MACD Slow` | Período EMA lenta dentro del cálculo MACD. | 26 |
| `MACD Signal` | Período de línea de señal del MACD. | 9 |
| `Take Profit %` | Distancia del take-profit protector en porcentaje. | 2 |
| `Stop Loss %` | Distancia del stop-loss protector en porcentaje. | 1 |
| `Equity Risk %` | Drawdown máximo de capital de cuenta permitido antes de aplanar todas las posiciones. | 3 |

Todos los parámetros numéricos exponen sugerencias de optimización que reflejan los rangos típicos de las entradas MQL.

## Gestión de riesgos
`StartProtection` está configurado para aplicar:

- Stop-loss y take-profit basados en porcentaje relativos al precio de entrada.
- Guardia de drawdown de capital que aplana la estrategia si la pérdida supera el porcentaje configurado.

Estas protecciones sustituyen las numerosas rutinas de balance, trailing y punto de equilibrio del experto original, proporcionando un comportamiento más claro y seguro dentro de StockSharp.

## Diferencias respecto a la versión MetaTrader
- Las lecturas de objetos del gráfico fueron reemplazadas por un filtro de pendiente de regresión porque las estrategias de StockSharp no interactúan con canales de Fibonacci manuales.
- En lugar de una mezcla de lógica de trailing basada en dinero, la estrategia depende de `StartProtection`.
- El conjunto de indicadores sigue siendo el mismo (LWMA, Momentum, MACD), pero se implementa usando vínculos de alto nivel y sin sondeo directo de valores de indicadores.
- Las alertas, correos electrónicos y notificaciones push fueron eliminados ya que el entorno StockSharp ya proporciona registro consolidado.

## Notas de uso
1. Adjunte la estrategia a una cartera y un valor, configure el tamaño del lote a través de la propiedad `Volume` y ajuste los parámetros según sea necesario.
2. Asegúrese de que los datos históricos estén disponibles para el tipo de vela seleccionado para que el buffer de Momentum y el indicador de pendiente puedan formarse correctamente.
3. Ejecute primero en trading de papel para ajustar el umbral de Momentum y los parámetros de riesgo según la volatilidad del instrumento operado.
