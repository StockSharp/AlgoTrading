# Estrategia experta en martingala
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Martingail Expert es una estrategia de martingala que sigue tendencias y se basa en el oscilador Stochastic para cronometrar nuevas secuencias de operaciones. Una vez que el indicador genera una dirección, la estrategia inicia una escalera de órdenes de mercado y gestiona la exposición utilizando un objetivo de ganancias dinámico y un esquema de tamaño de posición geométrico.

## Lógica de trading
- Calcule un oscilador Stochastic en la serie de velas configurada. Los valores finales más recientes de %K y %D se almacenan en caché para la toma de decisiones.
- Inicie una nueva secuencia larga cuando `%K (previous) > %D (previous)` y `%D (previous)` estén por encima del umbral `BuyLevel`.
- Inicie una nueva secuencia corta cuando `%K (previous) < %D (previous)` y `%D (previous)` estén por debajo del umbral `SellLevel`.
- Después de ingresar una secuencia, cada movimiento de precio favorable igual a `ProfitFactor × openOrders` pips agrega una nueva posición con el volumen base.
- Cada movimiento adverso de `StepPoints` pips multiplica el último volumen llenado por `Multiplier` y envía una orden promedio en la misma dirección.

## Reglas de salida
- Cierre toda la posición tan pronto como el último precio de ejecución alcance un objetivo de beneficio dinámico de `ProfitFactor × openOrders` pips en la dirección favorable.
- Restablezca el estado de martingala cada vez que el tamaño de la posición agregada vuelva a cero.

## Gestión del riesgo
La progresión de martingala aumenta la exposición rápidamente cuando el precio se mueve en contra de la posición. Ajuste `Multiplier`, `StepPoints` y `ProfitFactor` cuidadosamente para que coincidan con el tamaño de la cuenta y la volatilidad del instrumento.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Volume` | Volumen de orden de mercado base utilizado para la primera operación y cada complemento favorable. |
| `Multiplier` | Factor aplicado al último volumen ejecutado al promediar durante movimientos adversos. |
| `StepPoints` | Distancia en puntos que desencadena un orden de promediación de martingala. |
| `ProfitFactor` | Objetivo de beneficio por orden abierta expresado en puntos. La distancia real es `ProfitFactor × number_of_orders`. |
| `KPeriod` | Longitud retrospectiva para la línea %K. |
| `DPeriod` | Longitud de suavizado para la línea %D. |
| `Slowing` | Suavizado adicional aplicado a %K antes de comparar con %D. |
| `BuyLevel` | Valor mínimo de %D requerido para permitir una nueva secuencia larga. |
| `SellLevel` | Valor máximo de %D requerido para permitir una nueva secuencia corta. |
| `CandleType` | Serie de velas utilizadas para los cálculos (predeterminado: período de 5 minutos). |

## Notas
- Funciona mejor en pares de divisas líquidos donde el tamaño del pip y el paso de volumen permiten un escalado granular.
- Requiere margen suficiente para soportar varios pasos de martingala.
