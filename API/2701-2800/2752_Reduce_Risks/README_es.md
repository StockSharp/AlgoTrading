# Estrategia de Reducción de Riesgos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Reducción de Riesgos es un sistema de seguimiento de tendencia multi-temporal convertido del asesor experto de MetaTrader "Reduce_risks.mq5". Analiza velas de un minuto para activar entradas mientras filtra el régimen de mercado con promedios de 15 minutos y 1 hora. El algoritmo original fue diseñado para divisas principales altamente líquidas (EURUSD, USDCHF, USDJPY) y se enfoca en entrar en tendencias solo cuando la volatilidad es reducida y la estructura confirma la continuación.

## Mercado y marcos temporales
- **Marco temporal primario:** velas de 1 minuto para la generación de señales.
- **Marco temporal de confirmación:** velas de 15 minutos para validación de momentum y posicionamiento de onda.
- **Filtro de tendencia:** velas de 1 hora para asegurar el trading en la dirección de la tendencia más amplia.
- **Instrumentos recomendados:** EURUSD, USDCHF, USDJPY o instrumentos con estructura de pips similar (cotización de 4 o 5 decimales).

## Indicadores y datos
- Cuatro medias móviles simples (SMA) en M1: períodos 5, 8, 13 y 60 calculados sobre el precio típico.
- Tres SMA en M15: períodos 4, 5 y 8 calculados sobre el precio típico.
- Una SMA en H1: período 24 calculado sobre el precio típico.
- Estadísticas de velas (tamaño del cuerpo, rango, sombras) tanto para M1 como para M15.
- Los contadores internos rastrean el precio más alto o más bajo desde la entrada para emular la lógica de trailing de MQL.

## Reglas de entrada
### Configuración larga
1. Las velas recientes de M1 y M15 deben mostrar baja volatilidad: las tres barras anteriores en cada marco temporal tienen rangos por debajo de 20 y 30 pips respectivamente, y el ancho del canal de 15 minutos está limitado a 30 pips.
2. La última vela M1 completada es más activa que su predecesora pero no tres veces más grande, y el precio actual rompe tanto los máximos recientes de M1 como de M15 (resistencia local despejada).
3. La jerarquía de SMA apunta hacia arriba: SMA5 > SMA8 > SMA13 y SMA60 en alza; el precio de cierre se sitúa por encima de los cuatro promedios.
4. SMA4 en M15 está en alza y posicionada por encima de SMA8, mientras que el precio de cierre está por encima de los promedios de M15 y H1.
5. Confirmación de onda: SMA8 en M1 cruzó dentro de cualquiera de las tres velas anteriores, y SMA5 en M15 se encuentra dentro del rango de la vela M15 anterior.
6. Filtros de estructura de velas: las velas anteriores de M1 y M15 tienen cuerpos alcistas que superan la mitad de sus rangos, mantienen máximos más altos, muestran retrocesos aceptables (<25% del rango de la vela anterior) y contienen sombras intrábarra (sin marubozu).
7. Todas las condiciones anteriores deben satisfacerse simultáneamente sin posición abierta antes de emitir una orden de compra de mercado.

### Configuración corta
1. Se aplican los mismos filtros de volatilidad, pero el rompimiento debe ocurrir por debajo de los mínimos recientes (violación de soporte).
2. La jerarquía de SMA se invierte: SMA5 < SMA8 < SMA13 con SMA60 cayendo; el precio de cierre se sitúa por debajo de los cuatro promedios.
3. SMA4 en M15 declina y está por debajo de SMA8; el precio de cierre está por debajo de los promedios de M15 y H1.
4. Validación de onda: SMA8 en M1 se encuentra dentro de cualquiera de los tres rangos de velas M1 anteriores, SMA5 en M15 reside dentro de la última vela M15, y las velas recientes muestran estructura bajista persistente (mínimos más bajos, cuerpos bajistas, retrocesos limitados, sombras presentes).
5. Sin posición activa, se envía una orden de venta de mercado una vez que todas las condiciones se alinean.

## Reglas de salida
- Las órdenes de stop-loss y take-profit de protección se adjuntan automáticamente usando las distancias de pips configuradas (refleja el comportamiento original del EA).
- Las salidas discrecionales adicionales replican la lógica MQL:
  - Cerrar largos si la vela M1 actual colapsa al menos 10 pips desde su apertura o si aparece una vela M1 bajista fuerte después de que la operación haya estado abierta más de un minuto.
  - Tomar beneficio anticipadamente cuando el precio avanza al menos 10 pips, o cuando ocurre una reversión de trailing: después de la primera barra tras la entrada, si el precio retrocede 20 pips desde el nivel más alto alcanzado desde la entrada mientras ese máximo está por encima del precio de entrada.
  - Cerrar largos en una excursión adversa de 20 pips o siempre que el capital del portafolio caiga por debajo del umbral de drawdown configurado. Las posiciones cortas usan lógica simétrica con comparaciones invertidas.

## Gestión de riesgos
- El trading se detiene automáticamente cuando el capital del portafolio cae por debajo de `(InitialDeposit * (100% - RiskPercent))`. El límite se verifica en cada intento de señal y se restablece cuando el capital se recupera por encima del umbral.
- El script MQL original incluía extensas verificaciones del terminal; esas se omiten porque StockSharp maneja la conectividad y los permisos de forma nativa.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `StopLossPips` | Distancia del stop de protección en pips (reflejada por la lógica de trailing). | `30` |
| `TakeProfitPips` | Distancia del take-profit en pips. | `60` |
| `InitialDeposit` | Capital de referencia usado para calcular el stop de drawdown. | `10000` |
| `RiskPercent` | Porcentaje máximo del depósito inicial que puede perderse antes de bloquear nuevas operaciones y forzar el cierre de posiciones activas. | `5` |
| `M1CandleType` | Tipo de datos para la suscripción de velas de 1 minuto. | Marco temporal de `1 minuto` |
| `M15CandleType` | Tipo de datos para la suscripción de confirmación de 15 minutos. | Marco temporal de `15 minutos` |
| `H1CandleType` | Tipo de datos para la suscripción de filtro de tendencia de 1 hora. | Marco temporal de `1 hora` |

## Notas
- La estrategia espera instrumentos cotizados con tamaños de pip similares a los pares de divisas principales. Ajuste los parámetros basados en pips cuando use otros mercados.
- Solo se proporciona la implementación en C#; la versión en Python se omite intencionalmente según los requisitos.
