# Estrategia de Tendencia Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia reproduce el sistema clásico Alligator de Bill Williams del script original de MetaTrader (`Alligator.mq5`). Utiliza tres medias móviles suavizadas construidas sobre el precio mediano y desplazadas hacia adelante para visualizar la fase del mercado. Se abre una posición larga cuando la línea rápida Lips está por encima de Teeth, y Teeth está por encima de Jaw. Se abre una posición corta cuando la alineación está invertida. Solo puede estar activa una posición al mismo tiempo.

Una vez en una operación, la estrategia protege la posición con un stop-loss y take-profit expresados en pips. Cuando el mercado se mueve a favor de la operación una distancia de nivel cero configurable, el stop se mueve a punto de equilibrio. Un trailing stop sigue el máximo más alto (para largos) o el mínimo más bajo (para cortos) con un paso mínimo para evitar actualizaciones frecuentes del stop. Las posiciones se cierran cuando se alcanzan los niveles de stop-loss, trailing stop o take-profit.

La configuración predeterminada apunta a velas de 30 minutos y valores de pip estilo Forex, pero los parámetros se pueden optimizar para otros mercados. Dado que la versión MQL original usa el manejo de pips específico del bróker, la conversión depende del `PriceStep` del instrumento para traducir distancias en pips a precios absolutos.

## Reglas de Trading

### Entrada
- **Largo**: Sin posición abierta y `Lips > Teeth > Jaw` en la última vela completada.
- **Corto**: Sin posición abierta y `Lips < Teeth < Jaw` en la última vela completada.

### Salida y Gestión de Riesgos
- **Stop Inicial**: Colocado `StopLossPips` por debajo (largo) o por encima (corto) del precio de llenado.
- **Take Profit**: Colocado a `TakeProfitPips` del precio de llenado.
- **Nivel Cero**: Cuando el precio avanza `ZeroLevelPips`, el stop se mueve al precio de entrada.
- **Trailing Stop**: Después de la activación del nivel cero, el stop sigue el extremo con `TrailingStopPips`, actualizándose solo cuando la mejora supera `TrailingStepPips`.
- Las posiciones se aplanan inmediatamente cuando cualquier stop o el nivel de take-profit es tocado en los datos de la vela.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `CandleType` | Marco temporal de 30 minutos | Serie de velas utilizada para cálculos de indicadores y evaluación de señales. |
| `JawLength` | 13 | Período de media móvil suavizada para la línea de mandíbula azul. |
| `TeethLength` | 8 | Período de media móvil suavizada para la línea de dientes roja. |
| `LipsLength` | 5 | Período de media móvil suavizada para la línea de labios verde. |
| `JawShift` | 8 | Desplazamiento hacia adelante de la línea de mandíbula, expresado en barras. |
| `TeethShift` | 5 | Desplazamiento hacia adelante de la línea de dientes, expresado en barras. |
| `LipsShift` | 3 | Desplazamiento hacia adelante de la línea de labios, expresado en barras. |
| `EnableLong` | `true` | Permite o bloquea entradas largas. |
| `EnableShort` | `true` | Permite o bloquea entradas cortas. |
| `StopLossPips` | 45 | Distancia de stop-loss en pips desde el precio de llenado. |
| `TakeProfitPips` | 145 | Distancia de take-profit en pips desde el precio de llenado. |
| `ZeroLevelPips` | 30 | Distancia en pips requerida para mover el stop a punto de equilibrio. |
| `TrailingStopPips` | 50 | Distancia entre el extremo actual y el trailing stop. |
| `TrailingStepPips` | 10 | Mejora mínima en pips requerida antes de actualizar el trailing stop. |

## Notas

- El indicador Alligator se calcula sobre el precio mediano `(High + Low) / 2` para coincidir con la implementación de MetaTrader.
- Los valores de línea desplazados se emulan con búferes internos para que las comparaciones usen los mismos datos desplazados que el script original.
- La estrategia asume que una operación se ejecuta antes de que se procese una nueva señal en la misma barra, reflejando la ejecución barra a barra del EA fuente.
- Optimice las distancias en pips para que coincidan con el tamaño del tick y la volatilidad del instrumento operado.
