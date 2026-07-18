# Estrategia de Cobertura de Doble Lote por Pasos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

La **Estrategia de Cobertura de Doble Lote por Pasos** es un port en C# de los asesores expertos de MetaTrader 5 *"x1 lot from high to low"* y *"x1 lot from low to high"* (carpeta `MQL/19543`). Los robots originales abren inmediatamente una cesta cubierta de posiciones de compra y venta, ciclan el volumen de la orden después de cada nueva entrada, y cierran toda la cesta una vez que se alcanza un objetivo de beneficio fijo. Esta implementación reproduce ese comportamiento sobre la API de alto nivel de StockSharp mientras expone parámetros limpios y gestión detallada del estado.

Están disponibles dos modos de operación:

- **HighToLow** – comienza con el multiplicador de lote máximo, abre la primera cesta cubierta con el volumen más grande, y luego disminuye al siguiente paso de lote después de las primeras entradas.
- **LowToHigh** – comienza con el paso de lote mínimo, aumenta el tamaño del lote después de cada nueva entrada hasta que se alcanza el multiplicador configurado, y luego continúa operando a ese tamaño.

La estrategia mantiene ambas patas de compra y venta activas simultáneamente, gestiona los niveles de stop-loss y take-profit por pata, y monitorea el capital del portafolio para imponer un objetivo de beneficio a nivel de cesta.

## Lógica de Trading

1. Cuando no existen posiciones, la estrategia abre **ambas** una orden de mercado larga y corta usando el tamaño de lote actual.
2. Si exactamente una pata está activa (por ejemplo, el lado opuesto fue detenido), la pata faltante se reabre en mercado con el tamaño de lote actual.
3. Después de cada entrada exitosa, el tamaño de lote se actualiza dependiendo del modo seleccionado (`HighToLow` o `LowToHigh`).
4. Las salidas de protección por pata se evalúan en cada tick de operación entrante:
   - Una pata larga se cierra si el precio alcanza su stop-loss (`StopLossPips` por debajo de la entrada larga promedio) o su take-profit (`TakeProfitPips` por encima de la entrada promedio).
   - Una pata corta se cierra si el precio alcanza su stop-loss (`StopLossPips` por encima de la entrada corta promedio) o su take-profit (`TakeProfitPips` por debajo de la entrada promedio).
5. Una vez que la ganancia de capital del portafolio supera `MinProfit`, la estrategia cierra todas las posiciones restantes y restablece el estado del lote al tamaño de inicio del modo.
6. La lógica de seguridad cierra la cesta y restablece todo si se detecta inesperadamente más de una posición de compra o venta.

Todas las órdenes se envían a través de los helpers de alto nivel `BuyMarket` y `SellMarket`. La estrategia rastrea los rellenos con `OnOwnTradeReceived`, mantiene la exposición agregada por pata, y previene órdenes duplicadas mientras las entradas o salidas aún están pendientes.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `LotMultiplier` | Multiplicador de lote máximo expresado en pasos de volumen mínimos (por defecto `10`). |
| `StopLossPips` | Distancia de stop-loss en pips para cada pata (por defecto `50`). Establecer en `0` para deshabilitar. |
| `TakeProfitPips` | Distancia de take-profit en pips para cada pata (por defecto `150`). Establecer en `0` para deshabilitar. |
| `MinProfit` | Objetivo de beneficio de la cesta en moneda de la cuenta. Una vez que la ganancia de capital supera este valor, todas las posiciones se cierran (por defecto `27`). |
| `ScalingMode` | Comportamiento del paso de lote. `HighToLow` refleja el EA "x1 lot from high to low", `LowToHigh` refleja "x1 lot from low to high". |

La estrategia deriva automáticamente el paso de volumen mínimo de `Security.VolumeStep` y calcula el valor de pip usando el paso de precio del instrumento (con el ajuste forex tradicional de 4/5 dígitos).

## Reinicio y Ciclo de Volumen

- **HighToLow** – abre la primera cesta con el volumen más alto (`VolumeStep * LotMultiplier`). Después de cualquier entrada, el volumen interno se reduce en un paso. Cuando se alcanza el objetivo de beneficio de la cesta, el volumen se restablece a `0` para que el siguiente ciclo comience desde el máximo nuevamente.
- **LowToHigh** – comienza desde el paso de lote mínimo. Después de cada entrada, el lote se aumenta en un paso hasta que se alcanza el techo del multiplicador. Cuando se alcanza el objetivo de beneficio de la cesta, el volumen se restablece al paso mínimo.

## Notas de Uso

- La estrategia se suscribe a operaciones de tick (`DataType.Ticks`) porque los bots MetaTrader originales se ejecutan en eventos de tick. Configure el proveedor de historia o el conector en vivo en consecuencia.
- Las verificaciones de stop-loss y take-profit ocurren dentro del algoritmo, por lo que no se registran órdenes de protección adicionales en el exchange.
- Dado que ambas patas se abren en mercado, la estrategia funciona mejor en brokers que soportan posiciones cubiertas y spreads pequeños. En venues de netting, seguirá funcionando pero las patas se compensarán efectivamente hasta que una de ellas sea cerrada por la lógica interna.
- Los parámetros predeterminados copian la configuración MQL original. Ajústelos cuidadosamente: cubrir altos volúmenes puede generar drawdowns significativos antes de que se alcance el objetivo de beneficio de la cesta.

## Correspondencia con la Lógica MQL Original

| Variable MetaTrader | Propiedad C# / Comportamiento |
|---------------------|-------------------------------|
| `InpLots` | `LotMultiplier` con manejo automático de paso de volumen. |
| `InpStopLoss` & `InpTakeProfit` | `StopLossPips` y `TakeProfitPips` con conversión de pip basada en `PriceStep`. |
| `InpMinProfit` | `MinProfit` y la verificación de capital del portafolio. |
| `LotCheck` | Helper `LotCheck` que aplica el paso mínimo y el volumen máximo. |
| `CalculatePositions` | Seguimiento interno de exposición larga/corta a través de `OnOwnTradeReceived`. |
| `CloseAllPositions()` | Método `CloseAllPositions` con coordinación de órdenes pendientes y reinicio de estado. |

## Consideraciones de Gestión de Riesgo

La estrategia mantiene intencionalmente posiciones largas y cortas abiertas, lo que causa exposición continua a costos de spread y tasas de swap. Antes de ejecutar con capital real:

- Validar el comportamiento en el emulador de StockSharp o en trading en papel.
- Asegurarse de que su broker soporte la cobertura; de lo contrario, las patas larga/corta serán neteadas inmediatamente.
- Ajustar los valores de stop-loss, take-profit y objetivo de beneficio a la volatilidad del instrumento.
- Monitorear el uso del margen, porque las patas largas/cortas simultáneas duplican la exposición nominal.

## Archivos

- `CS/DualLotStepHedgeStrategy.cs` – implementación de estrategia StockSharp con comentarios en línea extensos.
- `README_ru.md` – traducción rusa con instrucciones detalladas.
- `README_zh.md` – traducción china con instrucciones detalladas.
