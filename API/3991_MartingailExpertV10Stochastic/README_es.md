# MartingailExpert v1.0 Stochastic Estrategia (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia **MartingailExpert v1.0 Stochastic** es una conversión directa del asesor experto MetaTrader 4
`MartingailExpert_v1_0_Stochastic.mq4`. La estrategia observa las líneas %K/%D del oscilador Stochastic
y abre una posición cuando la barra completada anteriormente produce una confirmación de impulso arriba (para largos)
o por debajo (para abreviaturas) de zonas de umbral configurables. Una vez que la primera operación está activa, el algoritmo crea un
Escalera martingala de órdenes de mercado adicionales cuyo volumen crece geométricamente y cuya toma de ganancias compartida
permanece anclado al precio de la última incorporación.

La conversión depende completamente del nivel alto API de StockSharp: suscripciones de velas, vinculación de indicadores y
ayudantes integrados `BuyMarket`/`SellMarket`. Todos los comentarios del código fueron reescritos en inglés y la implementación
sigue el estilo de sangría basado en pestañas requerido por las pautas del proyecto.

## Lógica de trading

### 1. Señal de entrada

1. El oscilador Stochastic (`Length = KPeriod`, `%K` suavizado = `Slowing`, `%D` suavizado = `DPeriod`) es
vinculado a la suscripción de vela principal. Sólo se procesan velas terminadas.
2. La estrategia imita la llamada MQL original `iStochastic(..., shift = 1)` almacenando los valores de la barra anterior.
de %K y %D. Se activa una entrada larga cuando `K_prev > D_prev` y `D_prev > ZoneBuy`. Una breve entrada es
se activa cuando `K_prev < D_prev` y `D_prev < ZoneSell`.
3. La primera operación utiliza `BuyVolume` o `SellVolume` y restablece cualquier estado de dirección opuesta para evitar
Mezclando escaleras largas y cortas.

### 2. Martingale promedio

1. Siempre que haya un cluster abierto (`_buyOrderCount` o `_sellOrderCount` mayor que cero) la estrategia
monitorea el mínimo (para largos) o máximo (para cortos) de la vela.
2. **Cálculo de pasos**
   * `StepMode = 0`: la próxima adición espera a que el precio se mueva exactamente `StepPoints × PointSize` en contra
el último pedido completado.
   * `StepMode = 1`: la distancia se convierte en `StepPoints + max(0, 2 × ordersCount − 2)` puntos, coincidiendo con el
MQL expresión `step + OrdersTotal*2 - 2`. La expresión se multiplica por el tamaño en puntos del instrumento.
(derivado de `Security.PriceStep` y ajustado para cotizaciones FX de 3/5 decimales).
3. Si la vela viola el nivel de activación, la estrategia envía una orden de mercado inmediata cuyo volumen es igual
`previousVolume × Multiplier`. Los volúmenes están normalizados al `VolumeStep` del instrumento, limitados por
`VolumeMax` (cuando esté disponible) y redondeado hacia abajo a cero si son inferiores a `VolumeMin`.
4. Después de cada adición, el precio objetivo compartido se actualiza a
`lastEntryPrice ± ProfitFactorPoints × PointSize × orderCount` dependiendo de la dirección.

### 3. Gestión de obtención de beneficios

1. El grupo se cierra una vez que la vela toca el precio objetivo compartido (`High >= target` para largos,
`Low <= target` para cortos). Una verificación adicional estima el beneficio precio-distancia utilizando el peso ponderado.
precio de entrada promedio para reflejar la protección original `OrderProfit()` de MQL.
2. Todas las órdenes abiertas se aplanan con un solo `SellMarket(Math.Abs(Position))` o
`BuyMarket(Math.Abs(Position))` llamada. Después de una salida exitosa, el estado interno de martingala se restablece.
3. Si el entorno externo cierra posiciones (intervención manual, stop-outs), la siguiente vela con
`Position == 0` borra automáticamente el estado de martingala en caché, manteniendo la estrategia coherente.

### 4. Notas de implementación adicionales

* El tamaño en puntos se deriva de `Security.PriceStep`. Para símbolos FX de 3 o 5 decimales, el valor se multiplica
por diez para emular el concepto MetaTrader de un pip (`Point`).
* `StartProtection()` se invoca una vez en `OnStarted` para que la plataforma pueda adjuntar comportamientos de protección comunes
(tiempos de espera, latidos del corazón, etc.).
* La estrategia dibuja velas, el indicador estocástico y operaciones propias en un área de gráfico dedicada para facilitar
Inspección visual durante las pruebas retrospectivas.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| ---- | ---- | ------- | ----------- |
| `StepPoints` | decimales | `25` | Distancia en puntos antes de realizar otra orden de martingala. |
| `StepMode` | entero | `0` | `0` – distancia fija, `1` – fijo más `2 × ordersCount − 2` puntos. |
| `ProfitFactorPoints` | decimales | `10` | Puntos agregados (o restados) por orden abierta para calcular la toma de ganancias del grupo. |
| `Multiplier` | decimales | `1.5` | Multiplicador aplicado al último volumen de pedido para la siguiente adición. |
| `BuyVolume` | decimales | `0.01` | Volumen de la orden larga inicial. |
| `SellVolume` | decimales | `0.01` | Volumen de la orden corta inicial. |
| `KPeriod` | entero | `200` | Período retrospectivo del oscilador estocástico. |
| `DPeriod` | entero | `20` | Período de suavizado para la línea de señal %D. |
| `Slowing` | entero | `20` | Suavizado adicional aplicado a %K (MetaTrader's `slowing`). |
| `ZoneBuy` | decimales | `50` | Valor mínimo de %D requerido para permitir entradas largas. |
| `ZoneSell` | decimales | `50` | Valor máximo de %D requerido para permitir entradas cortas. |
| `CandleType` | `DataType` | `5m time frame` | Tipo de vela utilizado para todos los cálculos del indicador. |

## Estructura de carpetas

```
API/3991/
├── CS/
│ └── MartingailExpertV10StochasticStrategy.cs
├── README.md
├── README_zh.md
└── README_ru.md
```

La implementación de Python se omite intencionalmente de acuerdo con los requisitos de la tarea.
