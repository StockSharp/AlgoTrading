# Estrategia de ruptura de soporte y resistencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el experto "SupportResistTrade" MetaTrader combinando una ruptura de soporte y resistencia recientes con un filtro de tendencia EMA a largo plazo. Las operaciones se abren solo cuando el precio rompe el límite del canal Donchian **y** la vela se abre en el mismo lado de una media móvil exponencial larga. El riesgo se gestiona mediante paradas protectoras inmediatas y una rutina de seguimiento de tres pasos que fija ganancias en +10, +20 y +30 puntos.

## Datos e indicadores
- **Feed principal:** suscripción de vela única (período de tiempo predeterminado de 1 minuto, configurable a través de `CandleType`).
- **Soporte/Resistencia:** `DonchianChannels` con longitud `RangeLength` (predeterminado 55) para rastrear el máximo más alto y el mínimo más bajo del rango reciente.
- **Filtro de tendencia:** `ExponentialMovingAverage` sobre aperturas de velas con período `EmaPeriod` (predeterminado 500). Solo se aceptan posiciones largas con precio superior a EMA y posiciones cortas con precio inferior a EMA.

## Lógica de trading
1. **Análisis de mercado:** en cada vela terminada se actualizan el rango Donchian y EMA. La banda superior se trata como resistencia y la banda inferior como soporte.
2. **Condiciones de entrada:**
   - **Largo:** la vela cierra por encima de la resistencia *y* su apertura estuvo por encima del EMA. Cualquier posición corta existente se cierra y se envía una orden de mercado larga.
   - **Corto:** la vela cierra por debajo del soporte *y* su apertura fue por debajo del EMA. Cualquier posición larga existente se cierra y se envía una orden de mercado corta.
3. **Parada inicial:** después de ejecutarse, se coloca una orden de parada en el último soporte (para largos) o resistencia (para cortos), reflejando el comportamiento de stop-loss de MQL.
4. **Lógica de salida:**
   - Cuando la operación genera ganancias y el cierre regresa más allá de la banda de soporte/resistencia actualizada, la posición se cierra en el mercado, coincidiendo con la condición de salida manual de EA.
   - El tope de protección permanece activo por lo que las marchas bruscas se detectan automáticamente.

## Parada final
Un mecanismo de seguimiento por etapas reproduce las tres llamadas `OrderModify` del EA:
| Umbral de beneficio (puntos) | Nueva distancia de parada (puntos) | Descripción |
| --- | --- | --- |
| `>= 20` | `10` | Saltos de parada larga a la entrada + 10 puntos (parada corta a la entrada − 10). |
| `>= 40` | `20` | El stop se mueve hacia la entrada +/− 20 puntos. |
| `>= 60` | `30` | El paso final bloquea 30 puntos de ganancia. |
La lógica nunca afloja el stop: para posiciones largas el stop sólo puede moverse hacia arriba, mientras que para posiciones cortas sólo puede moverse hacia abajo.

## Gestión del riesgo
- Todas las paradas se implementan como órdenes de parada nativas (`SellStop`/`BuyStop`) de modo que el corredor maneja la ejecución incluso si la estrategia se desconecta brevemente.
- La estrategia funciona sobre la base de la posición neta; cada nueva señal cierra la dirección opuesta antes de establecer una nueva operación.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `RangeLength` | `55` | Número de velas utilizadas para calcular el soporte (bajo) y la resistencia (alto). |
| `EmaPeriod` | `500` | Período del filtro de tendencia EMA aplicado a las aperturas de velas. |
| `CandleType` | `1 Minute` | Serie de velas utilizada para todos los cálculos (se puede cambiar a cualquier otro período de tiempo). |

## Notas
- El código está escrito en el nivel alto StockSharp API con enlace de indicador y suscripciones de vela únicamente.
- No se proporciona ningún puerto Python. La carpeta `CS` contiene la única implementación.
