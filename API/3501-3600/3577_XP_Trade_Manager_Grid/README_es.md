# Cuadrícula de XP Trade Manager (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia **XP Trade Manager Grid** es una adaptación directa del MetaTrader 4 asesores expertos `XP Trade Manager Grid.mq4`. Automatiza una cuadrícula simétrica que agrega continuamente nuevas posiciones cada vez que el mercado se aleja un número configurable de puntos del último tramo completado. El experto original gestionó las ganancias con niveles parciales de toma de ganancias para los primeros tres pedidos, un grupo de equilibrio cuando la escalera crece y una protección de riesgo global basada en el porcentaje de la cuenta. La implementación de StockSharp mantiene las mismas ideas al tiempo que aprovecha las primitivas API de alto nivel (órdenes de mercado, suscripciones de velas y parámetros de estrategia).

## Lógica de trading

1. **Entrada inicial**: la estrategia abre inmediatamente la primera orden de mercado en la dirección seleccionada por el usuario (vender de forma predeterminada). Todas las operaciones posteriores se agrupan en la red jerárquica.
2. **Expansión de la red**: cada vez que el precio de cierre se desvía un `StepPoints` * paso de precio más allá del tramo más reciente en un lado, se coloca una nueva orden de mercado en esa dirección siempre que el número total de tramos simultáneos sea inferior a `MaxOrders`.
3. **TP dedicado para las tres primeras etapas**: las tres primeras órdenes de cada lado heredan sus compensaciones de obtención de beneficios únicas (`TakeProfit1Partitive`, `TakeProfit2`, `TakeProfit3`). Una vez que los máximos y mínimos de la vela tocan esos niveles, la pierna se aplana.
4. **Clúster de equilibrio**: cuando la cantidad total de tramos abiertos llega a cuatro o más, la estrategia calcula el precio de equilibrio ponderado de toda la escalera. Dependiendo de qué lado tenga más tramos, compensa ese punto de equilibrio con el objetivo total correspondiente (`TakeProfit4Total`... `TakeProfit15Total`) dividido entre las órdenes activas. Si el precio toca el objetivo calculado, se cierra toda exposición.
5. **Renovación del ciclo**: si se cierra la primera orden de un ciclo pero el beneficio recaudado en puntos aún está por debajo de `TakeProfit1Total`, la lógica espera a que el mercado se mueva `TakeProfit1Offset` puntos más allá de la última salida y luego vuelve a abrir la orden inicial.
6. **Control de riesgos**: la ganancia flotante en la moneda de la cuenta (realizada + no realizada) se compara constantemente con el `RiskPercent` por ciento del saldo inicial de la cartera. Si se sobrepasa el umbral de pérdidas, toda la escalera se aplana inmediatamente.

El puerto C# realiza un seguimiento interno de cada tramo lleno. Se admiten los rellenos parciales y las estructuras cubiertas (compras y ventas simultáneas) se resuelven exactamente como en el experto MQL: los rellenos opuestos primero cancelan los tramos pendientes antes de que se registre una nueva exposición.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `CandleType` | Tipo de datos utilizado para impulsar la estrategia (predeterminado: velas de 1 minuto). |
| `OrderVolume` | Volumen de cada orden/tramo de mercado. |
| `MaxOrders` | Máximos tramos simultáneos en ambas direcciones. |
| `StepPoints` | Distancia en puntos entre órdenes de cuadrícula consecutivos. |
| `RiskPercent` | Pérdida flotante máxima tolerable como % del saldo inicial de la cartera. |
| `TakeProfit1Total` | Objetivo de puntos total acumulado por los ciclos del pedido n.º 1 antes de que no se produzca ninguna renovación automática. |
| `TakeProfit1Partitive` | Distancia de toma de ganancias (puntos) para el partido de ida. |
| `TakeProfit1Offset` | Distancia mínima de retroceso requerida antes de recrear la primera orden. |
| `TakeProfit2` / `TakeProfit3` | Compensaciones de TP individuales (puntos) para los tramos 2 y 3. |
| `TakeProfit4Total` … `TakeProfit15Total` | Totales de TP de equilibrio utilizados una vez que el tamaño de la escalera alcanza el número correspondiente de pedidos. |
| `InitialSide` | Dirección de la primera orden (Compra o Venta). |

> **Nota:** Todas las entradas basadas en puntos son escaladas automáticamente por la seguridad `PriceStep`, coincidiendo con la lógica `Point()` original de MetaTrader.

## Comportamiento comparado con la versión MetaTrader

* La variante StockSharp cierra los primeros tres tramos mediante órdenes de mercado en lugar de modificar los valores de toma de ganancias individuales, porque el nivel alto API no expone la modificación directa de la orden.
* Los cálculos de ganancias flotantes se basan en el paso del instrumento y el precio del paso. Los corredores con especificaciones contractuales exóticas pueden requerir ajustes si no exponen esos campos.
* Las etiquetas a nivel de plataforma que se muestran en MT4 ("Pips de ganancias" / "Divisa de ganancias") no se reproducen. En cambio, las estadísticas del ciclo interno se utilizan para decidir cuándo reabrir la primera orden.

## Requisitos

* Adjunte la estrategia a un valor que exponga tanto `PriceStep` como `StepPrice`.
* Asegúrese de que el conector comercial admita órdenes de mercado inmediatas o canceladas. Todos los tramos de la cuadrícula se ejecutan mediante métodos auxiliares `BuyMarket`/`SellMarket`.

## Consejos de uso

1. Comience con valores pequeños de `OrderVolume` cuando realice pruebas para evaluar cómo se comporta la cuadrícula en su feed.
2. Ajuste cuidadosamente `StepPoints` para la volatilidad del símbolo. Los escalones más grandes reducen el número de patas abiertas y, por tanto, la reducción.
3. Aumente `TakeProfit1Offset` cuando opere con instrumentos con diferenciales más amplios para evitar reingresos prematuros.
4. Combine la estrategia con la llamada `StartProtection()` incorporada, que monitorea las desconexiones inesperadas y se vuelve a conectar sin problemas.
