# Estrategia del sistema de anillo EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia traslada al experto en cobertura de grid multidivisa "RingSystemEA" de MetaTrader 4 al StockSharp nivel alto API. Organiza una lista configurable de monedas en anillos triangulares (tres monedas generan tres pares correlacionados) y administra dos cestas cubiertas por anillo: una cesta **más** (larga/corta/larga) y una cesta **menos** (corta/larga/corta). La estrategia monitorea continuamente las ganancias flotantes en cada anillo, aplica un refuerzo estilo martingala basado en pasos cuando las pérdidas exceden los umbrales configurados y coordina las salidas globales o por lado cuando se alcanzan los objetivos de ganancias o pérdidas.

## Lógica de trading

* Cree todas las combinaciones únicas de tres monedas de la lista ordenada `CurrenciesTrade` (por ejemplo, EUR/GBP/AUD produce EURGBP, EURAUD y GBPAUD).
* Cada anillo mantiene dos cestas sincronizadas:
  * **Cesta Plus** abre COMPRAR en el primer par, VENDER en el segundo par, COMPRAR en el tercer par.
  * **Menos cesta** abre la secuencia reflejada de VENDER/COMPRAR/VENDER.
* Las cestas se abren automáticamente una vez que el anillo tiene datos de precios y el filtro de sesión permite operar. Ambos lados pueden funcionar simultáneamente, o solo un lado dependiendo de `SideOpenOrders`.
* Cuando una cesta activa supera el umbral `StepOpenNextOrders` (opcionalmente escalado geométrica o exponencialmente), se agrega una nueva capa de pedidos utilizando reglas de progresión de volumen (`LotOrdersProgress`).
* Las cestas se cierran cuando su PnL flotante satisface el modo de salida elegido:
  * `SingleTicket` cierra las cestas más y menos de forma independiente.
  * `BasketTicket` cierra ambas cestas juntas una vez que su beneficio combinado alcanza el objetivo.
  * `PairByPair` cierra pares individuales cuando su PnL excede el objetivo.
* Las salidas de protección reflejan la lógica MT4. Dependiendo de `TypeCloseInLoss`, la estrategia cierra cestas enteras, reduce a la mitad la exposición o deja que las cestas se recuperen sin salidas forzadas.
* La protección de sesión opcional replica el comportamiento de esperar después de la apertura del lunes y detenerse antes del cierre del viernes.
* Los parámetros coinciden estrechamente con el EA original. El tamaño de lote automático utiliza el valor actual de la cartera y `RiskFactor`, mientras que la opción "lote justo" compensa las diferencias de valor de tick dentro de un anillo.

## Parámetros clave

| Parámetro | Descripción |
| --- | --- |
| `CurrenciesTrade` | Lista de monedas ordenada que define cómo se generan los anillos. |
| `NoOfGroupToSkip` | Números de timbre separados por comas que se deben ignorar. |
| `SideOpenOrders` | Elija el lado positivo, el negativo o ambos. |
| `OpenOrdersInLoss` + `StepOpenNextOrders` | Controla cuando se añaden pedidos adicionales mientras se pierden cestas. |
| `StepOrdersProgress` | Multiplicador aplicado al umbral de pérdida para cada capa adicional. |
| `LotOrdersProgress` | Regla de escala para volúmenes de pedidos posteriores. |
| `TypeCloseInProfit` / `TargetCloseProfit` | Lógica y umbrales de toma de ganancias. |
| `TypeCloseInLoss` / `TargetCloseLoss` | Salidas protectoras en caso de pérdida. |
| `AutoLotSize`, `RiskFactor`, `ManualLotSize`, `UseFairLotSize` | Opciones de administración de dinero. |
| `ControlSession`, `WaitAfterOpen`, `StopBeforeClose` | Guardia de ventana de negociación semanal. |
| `MaxSpread`, `MaximumOrders`, `MaxSlippage` | Restricciones de riesgo. |

## Notas de comportamiento

* El puerto StockSharp mantiene el estado en estructuras administradas en lugar de matrices sin procesar, pero el flujo comercial refleja al experto en MT4: abrir cestas equilibradas, monitorear la canasta PnL, reforzar en los pasos de reducción y cerrar en eventos de ganancias o riesgos.
* Todos los indicadores son implícitos; la estrategia se basa únicamente en las suscripciones de precios y la cuenta PnL para tomar decisiones.
* Los pedidos están etiquetados con `StringOrdersEA` para que las herramientas externas de posprocesamiento puedan identificarlos.
* Las órdenes de mercado utilizan la cartera estratégica; conecte los instrumentos deseados antes de comenzar.

## Diferencias con el original EA

* El filtrado de propagación se simplifica: el puerto StockSharp valida el `MaxSpread` configurado a través de datos de velas en lugar de instantáneas de ticks.
* El modo de paso automático reutiliza el valor del paso manual porque los cálculos de margen específicos de MetaTrader no están disponibles dentro de StockSharp.
* Se omiten las funciones de dibujo de la interfaz de usuario y registro de archivos de la versión MT4. `SaveInformations` ahora escribe diagnósticos detallados en el registro en lugar del gráfico.
* El tamaño de la posición utiliza el valor actual de la cartera; ajuste `RiskFactor` para calibrar el volumen.

## Consejos de uso

1. Conecte y asigne todos los pares de divisas a los que hace referencia `CurrenciesTrade`. Los ayudantes de prefijo/sufijo admiten símbolos específicos del corredor.
2. Configure `SideOpenOrders` para controlar si la estrategia debe mantener ambas cestas u operar en una sola dirección.
3. Sintonice `StepOpenNextOrders`, `StepOrdersProgress` y `LotOrdersProgress` con cuidado; Estos parámetros dan forma a la progresión de la martingala y la exposición al riesgo.
4. Revise los mensajes de registro cuando `SaveInformations` esté habilitado para comprender cómo evolucionan los anillos y cuándo se agregan o cierran cestas.

Este puerto conserva el comportamiento central de la red cubierta del experto MT4 mientras lo adapta a la arquitectura basada en eventos y al sistema de parámetros de StockSharp.
