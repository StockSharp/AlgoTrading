# Estrategia de intercambio (API 3751)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Swaper** replica el asesor experto de MetaTrader "Swaper 1.1" utilizando la estrategia de alto nivel de StockSharp API. el
El sistema original acumula ganancias de swap reequilibrando constantemente una cartera sintética entre exposición larga y corta. esto
La conversión preserva la lógica del flujo de dinero al reconstruir el saldo virtual del experto, calculando un valor razonable para el
instrumento subyacente y alinear la posición abierta con ese valor objetivo.

## Lógica principal

1. **Reconstrucción de capital sintético.** La estrategia recrea el acumulador MetaTrader `money` combinando el acumulador inicial
saldo (`BaseUnits * BeginPrice`), beneficio realizado de órdenes ejecutadas y la parte no realizada de la posición actual
escalado por `ContractMultiplier`.
2. **Denominador del valor razonable.** El experto MQL mantiene una variable `com` que crece o disminuye con el volumen activo. El StockSharp
El puerto refleja este comportamiento a través de `BaseUnits + ContractMultiplier * Position`.
3. **Cálculo del volumen objetivo.** El algoritmo evalúa el máximo de los dos últimos máximos de la vela (ajustado por el diferencial del mercado)
y el mínimo de los dos últimos mínimos para reproducir la barandilla MetaTrader. A `Experts / (Experts + 1)` factor controls how
agresivamente la estrategia avanza hacia el valor razonable.
4. **Ajustes de posición.** Dependiendo del valor `dt` calculado, la estrategia
   - cierra posiciones cuando el ajuste calculado es inferior a una décima parte de un lote, o
   - sells additional volume when `dt < 0`, or
   - compra volumen adicional cuando `dt >= 0`.
5. **Tamaño de lote teniendo en cuenta el margen.** El método auxiliar `GetTradableVolume` aproxima las comprobaciones de `AccountFreeMargin()` comparando el
configurado `MarginPerLot` con el capital de cartera disponible. Si el tamaño solicitado excede el margen disponible, el lote
la cantidad se reduce a la décima más cercana.

Todo el ciclo se ejecuta en velas terminadas, reemplazando la función original basada en ticks manteniendo la lógica económica.
intacto.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `Experts` | `1` | Ponderación aplicada al ajuste sintético del valor razonable. |
| `BeginPrice` | `1.8014` | Precio inicial utilizado para reconstruir el saldo virtual. |
| `MagicNumber` | `777` | Identificador conservado para compatibilidad con la versión MetaTrader (pedidos registrados si es necesario). |
| `BaseUnits` | `1000` | Unidades de capital iniciales utilizadas por el denominador de la ecuación del valor razonable. |
| `ContractMultiplier` | `10` | Multiplicador que convierte las diferencias de precios en moneda de cuenta. |
| `MarginPerLot` | `1000` | Capital aproximado requerido para respaldar un lote; gobierna la lógica de reducción de lotes. |
| `FallbackSpreadSteps` | `1` | Spread en pasos de precios cuando faltan cotizaciones de nivel uno. |
| `CandleType` | `1 Hour` | Plazo principal que alimenta el ciclo de reequilibrio. |

## Flujo de trabajo comercial

1. Suscríbase a la serie de velas configuradas y a los datos de nivel uno.
2. Realice un seguimiento de las mejores cotizaciones de oferta y demanda para obtener un diferencial preciso. Si la transmisión está en silencio, recurra a
`FallbackSpreadSteps * PriceStep`.
3. Vuelva a calcular el capital sintético y el denominador de cada vela terminada.
4. Compute `dt` using the high price path. Cuando `dt < 0`, cambie a la rama de precio bajo para emular la protección original
lógica.
5. Utilice `AdjustShort` o `AdjustLong` para reducir o expandir la posición. Cuando el tamaño objetivo es menor que una décima parte de un lote,
cierre la posición por completo para copiar el comportamiento `closeby` de MetaTrader.
6. Actualice el PnL realizado dentro de `OnOwnTradeReceived` para que las iteraciones posteriores utilicen el saldo más reciente.

## Diferencias frente a la versión MQL4

- El bucle `start()` impulsado por ticks se reemplaza por el procesamiento de velas, lo que evita esperas ocupadas y al mismo tiempo preserva la estrategia
intención.
- El historial de órdenes y el escaneo de operaciones abiertas se aproximan a través del propio flujo comercial de la estrategia en lugar de `OrdersHistoryTotal()`
y `OrdersTotal()`.
- Las comprobaciones de margen utilizan `Portfolio.CurrentValue` con una constante configurable `MarginPerLot` porque el margen específico del corredor
Las funciones no están disponibles en StockSharp.
- El cierre de pares vía `OrderCloseBy` se emula simplemente aplanando la posición neta, consistente con el modelo de compensación de la mayoría
StockSharp conectores.

## Notas de uso

- Configure `MarginPerLot` según las especificaciones del contrato del conector para evitar que la estrategia solicite una
Volumen inviable.
- La estrategia espera que los datos de las velas proporcionen máximos y mínimos confiables; utilizar un período de tiempo que coincida con el feed del corredor utilizado por el
MetaTrader versión si desea un comportamiento idéntico.
- Debido a que las cotizaciones de nivel uno pueden llegar de forma asincrónica, la estrategia almacena el último diferencial. Asegúrese de que tanto las velas como el nivel
Las suscripciones One están habilitadas para una replicación precisa.
