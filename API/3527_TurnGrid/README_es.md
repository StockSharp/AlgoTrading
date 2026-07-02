# Estrategia TurnGrid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia TurnGrid** replica el comportamiento del MQL5 Asesor Experto `TurnGrid.mq5` original. Construye una cuadrícula de precios simétrica alrededor del precio de mercado actual y alterna entre órdenes largas y cortas cada vez que el precio migra de una celda de la cuadrícula a otra. La estrategia reequilibra continuamente las órdenes abiertas para mantener una exposición tanto alcista como bajista hasta que se alcance el objetivo de renta variable configurado.

La conversión utiliza el nivel alto API de StockSharp: las suscripciones de velas impulsan las actualizaciones de la red, las órdenes de mercado manejan las entradas y salidas, y la gestión de riesgos se expresa a través de parámetros estratégicos. Todos los comentarios han sido traducidos al inglés y los nombres siguen las convenciones StockSharp.

## Lógica de trading

1. Cuando comienza la estrategia, captura el último cierre de vela y crea una cuadrícula que contiene `4 * GridShares` niveles. El nivel central se establece en el precio actual, los niveles superiores escalan en `1 + GridDistance` y los niveles inferiores escalan en `1 - GridDistance`.
2. Se coloca una orden de compra de mercado inicial en el centro de la cuadrícula. Su volumen se calcula a partir de la parte del presupuesto disponible (`Balance / GridShares`) y una fórmula de apuesta incremental heredada de la versión MQL.
3. Cada vela terminada actualiza el índice de la cuadrícula actual según el precio de cierre. Si el índice cambia:
   - Las posiciones vinculadas a billetes a dos niveles del nuevo índice se cierran (los billetes comprados por debajo del precio se venden, los billetes vendidos por encima se recompran).
   - Se abren nuevas posiciones para mantener activos los anclajes tanto largos como cortos. Si ninguna de las partes está presente, la estrategia abre la parte con menos posiciones activas para equilibrar la exposición.
4. Las tarifas se aproximan mediante el parámetro `FeeRate`. Cada pedido ejecutado contribuye a una tarifa corriente total que se utiliza al evaluar el rendimiento.
5. Cuando el capital de la cuenta (después de restar la estimación de la tarifa acumulada) excede el saldo inicial en `EquityTakeProfit`, la estrategia cierra la posición neta y reconstruye la cuadrícula en torno al último precio.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `GridDistance` | Distancia relativa entre niveles de cuadrícula adyacentes. | `0.01` |
| `GridShares` | Número máximo de posiciones de grilla simultáneas que pueden estar activas. | `50` |
| `EquityTakeProfit` | Ganancia porcentual sobre el saldo inicial requerido para restablecer la red. | `0.02` |
| `FeeRate` | Tarifa de transacción estimada por operación, aplicada al volumen ejecutado. | `0.0008` |
| `CandleType` | Serie de velas utilizada para impulsar la estrategia. | `1` período de tiempo de minutos |

## Notas de implementación

- La suscripción a velas se maneja a través de `SubscribeCandles(CandleType)` y la estrategia reacciona solo a las velas terminadas, coincidiendo con la lógica basada en ticks del EA original mientras mantiene la compatibilidad con StockSharp.
- El estado de la cuadrícula se almacena en una matriz liviana de estructuras `GridLevel` que contienen anclajes de precios, indicadores booleanos y volúmenes de tickets para cierres diferidos.
- Los tamaños de las órdenes siguen la fórmula de asignación de capital incremental original, con una normalización adicional a través de las configuraciones `VolumeStep`, `VolumeMin` y `VolumeMax` del valor.
- Los reinicios basados en acciones esperan a que se cierre la posición neta actual antes de reconstruir la red, lo que garantiza transiciones limpias entre los ciclos comerciales.

## Archivos

- `CS/TurnGridStrategy.cs`: implementación en C# de la estrategia utilizando StockSharp construcciones de alto nivel.
- `README.md` – Documentación en inglés (este archivo).
- `README_zh.md` – Documentación en chino simplificado.
- `README_ru.md` – Documentación rusa.
