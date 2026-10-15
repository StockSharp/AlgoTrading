# Estrategia de la barra siguiente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Nextbar** es una traducción directa del MetaTrader 4 asesores expertos `nextbar.mq4`. El EA original evalúa la distancia entre la última vela completada y una vela que tiene varias barras más antigua. Cuando el precio viaja lo suficientemente lejos en una dirección, sigue el impulso o cotiza en contra de él, dependiendo de la bandera de dirección configurada. Luego, las posiciones se protegen con niveles simétricos de toma de ganancias/stop-loss y se cierran forzosamente después de un número fijo de barras.

Esta versión de StockSharp mantiene el mismo comportamiento mientras utiliza la estrategia de alto nivel API. Procesa únicamente velas completadas, asegurando que todos los cálculos coincidan con la lógica de cierre de barra del script MT4.

## Lógica original MQL
* **Distancia de impulso**: compare `Close[1]` con `Close[bars2check+1]`. Si la diferencia es al menos `minbar * Point`, trátela como una señal válida.
* **Indicador de dirección**: la entrada MQL `direction` es igual a `1` para el seguimiento de tendencias (comprar después de un repunte, vender después de una caída) y `2` para operaciones contrarias (comprar después de una caída, vender después de un repunte).
* **Restricción de entrada**: solo se puede abrir un pedido a la vez. Se envía una nueva operación al inicio de la barra después de la señal.
* **Reglas de salida**: cierre una posición larga si el último cierre alcanza la distancia de ganancias por encima de la entrada o la distancia de pérdidas por debajo de ella; lo contrario se aplica a los pantalones cortos. Si no se alcanza ninguno de los niveles, cierre la operación después de que `bars2hold` velas completadas.

## StockSharp aspectos destacados de la implementación
* Utiliza `SubscribeCandles()` y `Bind` para recibir velas completadas en el período de tiempo configurado.
* Almacena un breve historial continuo de precios de cierre para hacer referencia a la vela que coincide con la compensación MQL `bars2check + 1`.
* Convierte todos los parámetros basados en puntos con `Security.PriceStep`, imitando la constante MetaTrader `Point`.
* Coloca órdenes de mercado con la estrategia `Volume` y admite entradas que siguen el impulso o contrarias a través del parámetro `Direction`.
* Implementa salidas de ganancias, pérdidas y períodos de tenencia exactamente una vez por vela terminada para mantenerse alineado con el flujo de trabajo original.

## Parámetros
| Parámetro | Descripción | Predeterminado | Notas |
|-----------|-------------|---------|-------|
| `CandleType` | Plazo utilizado para la evaluación de la señal. | plazo de 1 hora | Adjunte la estrategia a un valor que pueda proporcionar este tipo de vela. |
| `BarsToCheck` | Número de velas completadas entre el cierre de referencia y el último cierre. | 8 | Coincide con `bars2check` del EA. |
| `BarsToHold` | Número máximo de velas completadas para mantener una posición abierta. | 10 | Coincide con `bars2hold`. La posición se cierra en la barra donde el contador alcanza este número. |
| `MinMovePoints` | Distancia mínima (en MetaTrader puntos) entre los dos cierres comparados. | 77 | Corresponde a `minbar`. Convertido usando `Security.PriceStep`. |
| `TakeProfitPoints` | Distancia objetivo de ganancias en MetaTrader puntos. | 115 | Equivalente a la entrada `profit`. Establezca en cero para desactivarlo si lo desea. |
| `StopLossPoints` | Distancia de stop-loss en MetaTrader puntos. | 115 | Equivalente a la entrada `loss`. Establezca en cero para desactivarlo si lo desea. |
| `Direction` | Modo de negociación: `Follow` (tendencia) o `Reverse` (contrario). | `Follow` | Refleja la entrada `direction` (`1` = seguir, `2` = invertir). |
| `Volume` | Volumen comercial utilizado para órdenes de mercado. | Volumen de estrategia | Configure a través de la propiedad estándar `Strategy.Volume`. |

## Flujo de trabajo comercial
1. Espere una vela terminada y guarde en caché su precio de cierre.
2. Obtenga el cierre de hace `BarsToCheck` velas y calcule la diferencia.
3. Si el movimiento absoluto está por debajo de `MinMovePoints * PriceStep`, no hagas nada.
4. De lo contrario:
   * En el modo **Seguir**, compre si el precio subió y venda si el precio bajó.
   * En el modo **Inverso**, compre si el precio bajó y venda si el precio subió.
5. En cada vela finalizada posterior mientras la posición esté abierta:
   * Cierre largo cuando el cierre esté `TakeProfitPoints` por encima o `StopLossPoints` por debajo del precio de entrada almacenado.
   * Cierre los cortos cuando el cierre esté `TakeProfitPoints` por debajo o `StopLossPoints` por encima de la entrada.
   * Forzar el cierre una vez que hayan transcurrido `BarsToHold` velas desde la entrada.

## Notas de uso
* La conversión de puntos a precio absoluto requiere `Security.PriceStep`. Proporcione los metadatos correctos del instrumento (escalón de precio, precio de escalón, reglas de volumen) antes de ejecutar la estrategia.
* La estrategia no gestiona múltiples posiciones simultáneas; asegúrese de que `Volume` corresponda al tamaño que espera para un solo pedido MT4.
* Debido a que las decisiones se evalúan únicamente sobre velas completadas, la estrategia debe ejecutarse con datos históricos y en tiempo real que proporcionen barras terminadas.
