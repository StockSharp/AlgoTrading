# Estrategia Eugene de Rompimiento Interno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Eugene de Rompimiento Interno es un puerto directo del asesor experto original de MetaTrader por barabashkakvn. Se centra en la acción del precio pura: una secuencia de velas internas seguida de un rompimiento de rango. Los niveles de confirmación derivados del cuerpo de la vela anterior aseguran que el rompimiento desarrolle momentum antes de que la estrategia tome una posición.

## Descripción general

La estrategia vigila un nuevo máximo o mínimo en relación con la vela anterior. Las configuraciones largas requieren que la vela anterior tenga un mínimo por debajo del máximo de la vela anterior a ella, destacando la compresión antes del rompimiento. Las configuraciones cortas se niegan a operar si la vela anterior es una barra interna, reflejando las salvaguardas en la lógica MQL fuente. Las órdenes siempre se ejecutan al mercado con un volumen fijo.

## Lógica de mercado

- Enfatiza los rompimientos del máximo/mínimo más reciente para captar movimientos direccionales temprano.
- Usa el cuerpo de la vela anterior para calcular dos niveles de retroceso de un tercio (`zigLevelBuy` y `zigLevelSell`). El precio debe tocar estos niveles, o la sesión debe estar pasada la hora de activación configurada, antes de que se permita una entrada.
- Previene nuevas posiciones cuando un rompimiento coincide con una vela interna contra la dirección de la operación.
- Cierra posiciones abiertas siempre que se confirme la señal de rompimiento opuesta, asegurando que la estrategia siempre esté plana o alineada con la última señal.

## Reglas de entrada

### Largo

1. El máximo de la vela actual es mayor que el máximo de la vela anterior.
2. La confirmación se recibe cuando el mínimo actual perfora el retroceso de un tercio del cuerpo de la vela anterior, o la hora actual está más allá del parámetro de hora de activación.
3. El mínimo actual debe mantenerse por encima del mínimo anterior mientras el mínimo anterior se sitúa por debajo del máximo de hace dos velas.
4. No hay ninguna posición abierta.

### Corto

1. El mínimo de la vela actual es menor que el mínimo de la vela anterior.
2. La confirmación se recibe cuando el máximo actual prueba el retroceso superior de un tercio del cuerpo de la vela anterior, o la hora actual está más allá del parámetro de hora de activación.
3. La vela anterior no debe ser una barra interna.
4. El máximo actual debe estar por debajo del máximo anterior.
5. No hay ninguna posición abierta.

## Reglas de salida

- Cerrar posiciones largas cuando se forma un rompimiento corto validado (condiciones 1–3 de la lógica de entrada corta).
- Cerrar posiciones cortas cuando se forma un rompimiento largo validado (condiciones 1–3 de la lógica de entrada larga).

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `CandleType` | Marco temporal de las velas procesadas por la estrategia. | Velas de 1 hora |
| `Volume` | Tamaño de la orden enviada con cada orden de mercado. | 0.1 |
| `ActivationHour` | Hora del día después de la cual las confirmaciones se aceptan automáticamente, replicando el filtro `TimeCurrent()` del código MQL. | 8 |

## Notas

- Las verificaciones de confirmación etiquetadas "white bird" y "black bird" en el script original siempre se evalúan como falsas debido a las condiciones fuente; se conservan para paridad pero no afectan las decisiones de trading.
- No se utilizan indicadores adicionales ni trailing stops—el enfoque es puramente basado en precios y voltea posiciones en cada rompimiento opuesto.
