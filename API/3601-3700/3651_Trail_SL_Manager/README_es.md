# Estrategia de gestión de Trail SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen

Trail SL Manager es una estrategia de utilidad que reproduce el comportamiento del experto original MetaTrader `trailSL`.
No abre operaciones por sí solo. En cambio, supervisa las posiciones existentes y ajusta dinámicamente sus niveles de parada de protección.
La lógica refleja el guión original: primero se presiona el stop para alcanzar el punto de equilibrio una vez que el precio avanza en una cantidad configurable, luego un algoritmo de seguimiento incremental sigue bloqueando las ganancias a medida que continúa la tendencia.

## como funciona

1. Se suscribe al flujo de velas configurado para monitorear las barras terminadas.
2. Realiza un seguimiento del precio de entrada promedio y la dirección de la posición actual.
3. Cuando el precio se mueve a favor de la operación en `BreakEvenTriggerPoints`, el stop se empuja al precio de entrada más una compensación opcional.
4. Después de la activación del punto de equilibrio, o inmediatamente si se permite, la estrategia incrementa el stop en `TrailOffsetPoints` cada `TrailStepPoints` hasta que el precio se revierta y cierre la posición en el mercado.

Las reglas finales se calculan con la misma aritmética basada en puntos que la versión MetaTrader, por lo que el comportamiento sigue siendo familiar para los operadores que migran a StockSharp.

## Parámetros

| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `EnableBreakEven` | Permite mover el stop para alcanzar el punto de equilibrio una vez que la operación se vuelve rentable. | `true` |
| `BreakEvenTriggerPoints` | Distancia de beneficio en puntos necesaria para activar el movimiento de equilibrio. | `20` |
| `BreakEvenOffsetPoints` | Se agregan puntos adicionales al precio de entrada cuando se ejecuta el punto de equilibrio. | `10` |
| `EnableTrailing` | Alterna la lógica del trailing stop. | `true` |
| `TrailAfterBreakEven` | Si es `true`, el seguimiento comienza solo después del ajuste del punto de equilibrio. | `true` |
| `TrailStartPoints` | Se permite un beneficio mínimo en puntos antes del seguimiento. | `40` |
| `TrailStepPoints` | Paso de beneficio entre recálculos finales. | `10` |
| `TrailOffsetPoints` | Puntos agregados a la parada en cada paso final. | `10` |
| `InitialStopPoints` | Distancia del tope de protección inicial cuando aparece una nueva posición. | `200` |
| `CandleType` | Suscripción de velas utilizada para monitorear los cambios de precios. | `1 Minute` |

## Uso

1. Adjunte la estrategia a un entorno donde las entradas se generen mediante otra estrategia o manualmente.
2. Configure los umbrales basados en puntos para que coincidan con la volatilidad del símbolo y los requisitos del corredor.
3. Inicie la estrategia para que pueda monitorear las velas terminadas y ajustar las paradas automáticamente.
4. Supervise los dibujos del gráfico para ver cómo evolucionan los niveles de parada con cada paso final.

> **Nota:** La estrategia cierra posiciones con órdenes de mercado cuando se supera el trailing stop simulado. Agregue protección específica del intercambio (como órdenes stop reales) si su flujo de trabajo lo requiere.
