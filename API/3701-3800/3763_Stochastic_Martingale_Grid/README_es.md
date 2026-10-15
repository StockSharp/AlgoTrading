# Stochastic Martingale Estrategia de cuadrícula
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un StockSharp puerto del MetaTrader asesor experto `rmkp_9yj4qp1gn8fucubyqnvb`. Combina un filtro de entrada de oscilador estocástico con una cuadrícula de promedio estilo martingala. El algoritmo monitorea las velas terminadas, espera a que la línea de señal estocástica salga de las zonas predefinidas de sobrecompra o sobreventa y luego abre una posición en la dirección de la reversión. Cuando el precio se mueve en contra de la operación, agrega órdenes promedio con volumen duplicado a distancias de pips fijas. Cada tramo tiene su propio objetivo de obtención de beneficios y gestión de trailing stop, lo que permite que las posiciones se amplíen de forma independiente una vez que el precio se recupere.

## Lógica de trading
- **Detección de señal:**
  - Las líneas %K y %D de un oscilador estocástico configurable se evalúan en velas completadas.
  - Una configuración larga se activa cuando, en la vela anterior, %K estaba por encima de %D y %D estaba por debajo del umbral `ZoneBuy`.
  - Una configuración corta se activa cuando, en la vela anterior, %K estaba por debajo de %D y %D estaba por encima del umbral `ZoneSell`.
- **Ejecución inicial:**
  - Con una señal válida y mientras la cuenta está plana, la estrategia envía una orden de mercado con el `BaseVolume`.
  - El precio de entrada se almacena para gestionar los trailingstops y las órdenes promediadas posteriores.
- **Martingale promedio:**
  - Mientras una posición permanece abierta, el algoritmo detecta movimientos adversos del precio de `StepPips` con respecto a la última orden ejecutada.
  - Cada nueva orden promedio duplica el volumen del tramo anterior (progresión de martingala clásica) y solo se coloca si el número total de tramos abiertos es inferior a `MaxOrders` y el comercio permanece permitido.
- **Gestión de salidas:**
  - Cada tramo define un nivel de toma de ganancias individual ubicado a `TakeProfitPips` de su precio de entrada.
  - Los trailingstops se activan una vez que las ganancias no realizadas alcanzan `TrailingStopPips`; el ancla posterior se aprieta cada vez que las ganancias se extienden más.
  - Si el precio retrocede hasta el nivel final o alcanza el nivel de toma de ganancias, el tramo correspondiente se cierra mientras el resto del grupo permanece activo.
  - Cuando todos los tramos salen, la estrategia restablece su estado interno y espera la siguiente señal estocástica.

## Gestión del riesgo
- La expansión de martingala está limitada por `MaxOrders` y los límites de volumen de seguridad.
- Los volúmenes se normalizan según el `VolumeStep` del instrumento y se respetan las restricciones de volumen mínimo/máximo.
- Los trailingstops ayudan a proteger las ganancias flotantes de reversiones totales.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `CandleType` | Suscripción de vela utilizada para los cálculos de indicadores. | plazo de 15 minutos |
| `BaseVolume` | Volumen de pedido inicial realizado en la primera señal. | `0.1` |
| `TakeProfitPips` | Distancia de pip entre cada precio de entrada y su objetivo de obtención de beneficios. | `50` |
| `TrailingStopPips` | Distancia de pip utilizada para la activación y el seguimiento del trailing stop por tramo. | `20` |
| `MaxOrders` | Número máximo de tramos promediadores simultáneos (incluida la entrada inicial). | `7` |
| `StepPips` | Movimiento adverso mínimo, en pips, requerido antes de agregar otra orden promedio. | `7` |
| `KPeriod` | Longitud retrospectiva de la línea estocástica %K. | `5` |
| `DPeriod` | Longitud de suavizado para la línea estocástica %D. | `3` |
| `Slowing` | Suavizado adicional aplicado al cálculo de %K. | `3` |
| `ZoneBuy` | Límite superior que permite configuraciones largas cuando %K está por encima de %D. | `30` |
| `ZoneSell` | Límite inferior que permite configuraciones breves cuando %K está por debajo de %D. | `70` |

## Notas
- La estrategia utiliza StockSharp API de alto nivel con suscripciones de velas y vinculaciones de indicadores, manteniendo la implementación cerca de la lógica MetaTrader original mientras aprovecha las herramientas de visualización y riesgo de StockSharp.
- Debido a que las operaciones promedio duplican el volumen, asegúrese de que el volumen máximo permitido del instrumento pueda acomodar la escalera de martingala.
- Al igual que con cualquier sistema martingala, se recomienda encarecidamente una gestión adecuada del capital y restricciones de riesgo adicionales antes de implementarlo en una cuenta real.
