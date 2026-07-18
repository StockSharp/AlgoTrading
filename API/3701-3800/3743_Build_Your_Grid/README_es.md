# Construya su estrategia de red
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Build Your Grid** es una conversión directa del asesor experto MetaTrader "BuildYourGridEA". Mantiene dos independientes
Escalas de posiciones de mercado en el lado largo y corto, agrega nuevas capas cuando el precio avanza en un número configurable de pips.
sy opcionalmente aumenta el volumen negociado de forma geométrica o exponencial. La cesta se puede cerrar cuando se alcanza un objetivo de beneficio combinado.
et se alcanza, cuando se excede una pérdida máxima medida en pips, o emitiendo órdenes de cobertura siempre que se rompa la reducción flotante.
Es un porcentaje del saldo de la cuenta.

## como funciona

1. **Entradas iniciales.** Dependiendo de la *Colocación de órdenes*, la estrategia abre la primera orden de compra, venta o ambas órdenes de mercado tan pronto como la condición del diferencial lo permita.
2. **Expansión de la red.** Los pedidos adicionales se activan con la tendencia o en contra de ella. La distancia hasta la siguiente capa se mide en pips, opcionalmente se multiplica por el número de órdenes ya abiertas o por una potencia de dos.
3. **Progresión de volumen.** El tamaño del pedido sigue la regla de progresión del lote seleccionado (estático, geométrico o exponencial) y puede limitarse mediante el *Multiplicador máximo* en relación con la primera entrada.
4. **Recogida de beneficios.** La cesta completa se cierra una vez que el PnL flotante agregado supera el objetivo expresado en pips o en la moneda de la cuenta.
5. **Protección contra pérdidas.** Cuando la pérdida acumulada cruza el umbral de pips configurado, la estrategia cierra el ticket más antiguo de cada lado o toda la cesta dependiendo del modo *Manejo de pérdidas*.
6. **Cobertura.** Si la reducción flotante alcanza el *Umbral de cobertura (%)*, se envía una orden de equilibrio dimensionada por la diferencia de volumen y el *Multiplicador de cobertura* para congelar la exposición.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `Order Placement` | Qué direcciones están permitidas para abrir nuevas capas (ambas, solo largas, solo cortas). |
| `Grid Direction` | Si los pedidos adicionales siguen la tendencia o desvanecen el movimiento. |
| `Grid Step (pips)` | Distancia base en pips hasta la siguiente capa antes de aplicar los multiplicadores. |
| `Step Progression` | Distancia estática, crecimiento geométrico (× recuento) o crecimiento exponencial (× 2^(n-1)). |
| `Close Target` | Tipo de objetivo de ganancias (pips o moneda de la cuenta). |
| `Target (pips)` / `Target (currency)` | Umbral que se debe superar para cerrar la cesta en beneficio. |
| `Loss Handling` | Acción cuando se alcanza el límite de reducción de pips (no hacer nada, cerrar los primeros tickets o cerrar todos). |
| `Loss (pips)` | Pérdida combinada máxima tolerada antes de que se active la protección. |
| `Use Hedge` | Permite que las órdenes de cobertura equilibren la exposición neta durante reducciones profundas. |
| `Hedge Threshold (%)` | Porcentaje del saldo de la cuenta utilizado como desencadenante de la cobertura. |
| `Hedge Multiplier` | Multiplicador aplicado a la diferencia de volumen al emitir la orden de cobertura. |
| `Auto Volume` / `Risk Factor` | Dimensionamiento de posiciones impulsado por equilibrio. Volumen = Saldo × Factor de Riesgo / 100000. |
| `Manual Volume` | Tamaño de lote fijo cuando el tamaño automático está deshabilitado. |
| `Lot Progression` | Escalado estático, geométrico o exponencial para órdenes consecutivas. |
| `Max Multiplier` | Limita el tamaño del lote a `firstLot × MaxMultiplier`. |
| `Max Orders` | Número máximo de posiciones abiertas simultáneas (0 = ilimitado). |
| `Max Spread` | Bloquea nuevas operaciones mientras el diferencial en pips esté por encima del umbral (0 = ignorar). |
| `Use Completed Bar` / `Candle Type` | Evalúe las señales solo una vez por vela completa del tipo seleccionado. |

## Notas de uso

- La estrategia se basa en las mejores actualizaciones de oferta y demanda. Configure su fuente de datos para proporcionar cotizaciones de nivel 1 con diferenciales precisos.
- Las órdenes de cobertura dependen del valor de la cartera. Cuando se ejecuta en StockSharp Designer o Tester, asegúrese de que la cartera conectada informe un saldo significativo.
- Las estrategias grid acumulan riesgos rápidamente. Comience con volúmenes conservadores y pruebe la configuración en simulación antes de aplicarla al comercio real.
- Cuando `Use Completed Bar` está habilitado, la lógica comercial se evalúa solo una vez por vela terminada, lo que imita la opción "Usar barra completada" del asesor original.
