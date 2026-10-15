# Estrategia TugbaGold
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

TugbaGold es un asesor experto en promedios basado en cuadrículas que se origina en MetaTrader 5. La estrategia convertida recrea su lógica de gestión de canasta y tamaño de posición de martingala utilizando API de alto nivel de StockSharp. El sistema coloca nuevas órdenes cada vez que la vela anterior cierra con impulso direccional y construye progresivamente una cuadrícula de posiciones espaciadas por una distancia configurable. Las salidas promedio se ejecutan bloqueando ganancias en las posiciones extremas o cerrando parcialmente la cesta según el modo seleccionado.

## como funciona

1. La estrategia evalúa las velas completadas desde el parámetro `CandleType`. Las señales utilizan la vela *anterior*, que coincide con la lógica MT5 original.
2. Una vela alcista permite la colocación de una nueva orden de compra. Una vela bajista permite una nueva orden de venta.
3. Las órdenes se agregan solo si la distancia desde el mejor precio existente en esa dirección excede `PointOrderStepPips`.
4. El primer pedido utiliza `StartVolume`. Las entradas posteriores duplican el volumen de la posición más favorable respetando `MaxVolume` y los límites del corredor.
5. Una vez que existen al menos dos posiciones, la estrategia calcula los precios objetivo que incluyen el buffer `MinimalProfitPips`. El cálculo difiere según el modo de salida:
   - **Promedio** – promedio ponderado de las posiciones extremas más el colchón de ganancias.
   - **Parcial**: combinación de los peores y mejores tickets, donde el peor ticket usa `StartVolume` y el mejor usa su tamaño real.
6. Cuando se alcanzan los objetivos, la estrategia cierra las órdenes correspondientes:
   - **Modo promedio**: cierra ambas entradas extremas por completo.
   - **Modo parcial**: cierra completamente la peor entrada y reduce la mejor entrada en `StartVolume`.
7. Las posiciones independientes individuales utilizan `TakeProfitPips` para salir una vez que el precio alcanza la distancia configurada.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPips` | Distancia de toma de ganancias que se aplica cuando solo hay una posición abierta. Establezca en `0` para desactivar. |
| `StartVolume` | Volumen inicial para el primer pedido en una secuencia de cuadrícula. |
| `MaxVolume` | Volumen máximo de pedido. `0` mantiene la secuencia de duplicación ilimitada. |
| `CloseMode` | Modo de salida: `Average` (cierre ambos extremos) o `Partial` (cierre parcial + total). |
| `PointOrderStepPips` | Distancia mínima en pips antes de que se pueda agregar una nueva orden promedio. |
| `MinimalProfitPips` | Se agregó un colchón de ganancias adicional a los objetivos promedio. |
| `CandleType` | Serie de velas utilizadas para la evaluación de señales. |

## Gestión de posiciones

- Los incrementos de precios se derivan de `Security.PriceStep`. Si no está disponible, se utiliza un valor predeterminado de `0.0001`.
- Los volúmenes se normalizan automáticamente según las restricciones mínimas, máximas y de pasos del corredor.
- La estrategia rastrea las posiciones ocupadas internamente y emite órdenes de mercado (`BuyMarket` / `SellMarket`) al cerrar partes de la cesta.
- La protección se habilita automáticamente a través de `StartProtection()` una vez que comienza la estrategia.

## Notas y limitaciones

- La implementación supone ejecución inmediata de órdenes de mercado, similar al entorno MT5.
- Las señales promedio se basan en las mejores cotizaciones actuales de oferta y demanda; Asegúrese de que los datos de Nivel 1 estén disponibles para una ejecución precisa.
- Debido a que las salidas están impulsadas por la lógica estratégica, los niveles de stop-loss del experto original no se recrean.
- Utilice una gestión de riesgos cautelosa: el tamaño de la martingala puede generar una gran exposición si las tendencias persisten.

## Detalles de conversión

- Las fórmulas promedio y los ajustes de la canasta reflejan el código fuente original.
- La selección de posición (mejores/peores boletos) se reproduce mediante el seguimiento de los precios de apertura más altos y más bajos dentro de cada dirección.
- Toda la lógica se ejecuta dentro de la suscripción de vela utilizando el nivel alto API de StockSharp sin recurrir al acceso a datos de bajo nivel.
