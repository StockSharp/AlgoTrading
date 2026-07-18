# Estrategia OpenPendingorderAfterPositionGetStopLoss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **OpenPendingorderAfterPositionGetStopLoss** transfiere los MetaTrader 5 asesores expertos del mismo nombre al API de alto nivel de StockSharp. Evalúa continuamente la pendiente de la línea Stochastic %K en el período de tiempo seleccionado. Cuando %K baja, coloca una orden stop de venta por debajo del mercado, y cuando %K sube, coloca una orden stop de compra por encima del mercado. Cada entrada completa recibe inmediatamente una orden protectora de limitación de pérdidas y toma de ganancias. Si un stop-loss cierra la posición, la estrategia reinstala automáticamente la orden pendiente correspondiente para que la red de operaciones de ruptura se restablezca sin esperar a la siguiente vela.

## Reglas comerciales
- Suscríbase a las velas terminadas del período de tiempo configurado y calcule un oscilador Stochastic clásico (`KPeriod`, `DPeriod`, `Slowing`).
- Compare el valor %K actual con el valor de hace dos barras:
  - `%K(current) < %K(two bars ago)` → envíe una parada de venta por debajo de la mejor oferta.
  - `%K(current) > %K(two bars ago)` → envíe una parada de compra por encima de la mejor demanda.
- Las órdenes pendientes se compensan del mercado mediante el diferencial actual más el buffer `MinStopDistancePoints` definido por el usuario, que coincide con la lógica MQL original.
- Una vez que se completa una orden pendiente, la estrategia envía un stop-loss protector (orden stop) y una toma de ganancias opcional (orden limitada).
- Cuando se activa el stop-loss protector, la orden pendiente correspondiente se recrea inmediatamente utilizando los últimos precios del mercado.
- Las órdenes de protección se cancelan automáticamente cuando la posición se cierra mediante la toma de ganancias o cuando la estrategia se detiene.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `OrderVolume` | Volumen comercial en lotes para cada orden pendiente. |
| `StopLossPoints` | Distancia de stop-loss en puntos de símbolo. Establezca en 0 para desactivar. |
| `TakeProfitPoints` | Distancia de toma de ganancias en puntos de símbolo. Establezca en 0 para desactivar. |
| `MinStopDistancePoints` | Colchón de precio mínimo (en puntos) agregado al diferencial antes de realizar una orden pendiente. |
| `MaxPositions` | Número máximo de posiciones simultáneas por dirección (las cuentas de compensación utilizan efectivamente 0 o 1). |
| `KPeriod` | Número de barras utilizadas para el cálculo de %K. |
| `DPeriod` | Longitud de la línea %D de suavizado. |
| `Slowing` | Factor de suavizado adicional aplicado a %K antes de la comparación. |
| `PendingExpiry` | Vida útil opcional de las órdenes stop pendientes. Las órdenes vencidas se cancelan en la siguiente vela. |
| `CandleType` | Marco de tiempo utilizado para la suscripción de velas y los cálculos de indicadores. |

## Notas de implementación
- Toda la gestión de pedidos depende de ayudantes de alto nivel como `BuyStop`, `SellStop`, `SellLimit` y `BuyLimit` según lo requiere `AGENTS.md`.
- Los valores del indicador se consumen directamente dentro de la devolución de llamada `SubscribeCandles().BindEx(...)`, evitando cualquier llamada a `GetValue`.
- La estrategia monitorea eventos `MyTrade` para instalar y eliminar órdenes de protección, emulando la lógica `OnTradeTransaction` del Asesor Experto original.
- Los comentarios dentro del código están escritos en inglés y la sangría se realiza con tabulaciones, de conformidad con las pautas del repositorio.
