# Estrategia Cruce de Dos iMA v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia recrea el expert advisor de MetaTrader "Crossing of two iMA v2" usando la API de alto nivel de StockSharp. Dos medias móviles desplazadas generan señales de cruce, opcionalmente filtradas por una tercera media móvil. Los stops protectores, el dimensionamiento de posición fijo o basado en porcentaje, y un trailing stop barra a barra emulan el comportamiento del robot original mientras mantienen la implementación compatible con las mejores prácticas de StockSharp.

## Indicadores y entradas
- **Primera Media Móvil** – período, desplazamiento, método de suavizado y precio aplicado configurables.
- **Segunda Media Móvil** – configuración independiente con el mismo conjunto de opciones.
- **Filtro de Tercera Media Móvil** – filtro de tendencia opcional que mantiene operaciones largas solo cuando la primera MA está por debajo del filtro y operaciones cortas cuando la primera MA está por encima del filtro.
- **Tipo de Vela** – controla el marco temporal/serie entregada por la suscripción de datos.

## Lógica de trading
### Paso 1 – Cruce inmediato
1. En cada vela terminada, la estrategia actualiza todas las medias móviles usando los precios aplicados seleccionados.
2. Una entrada **larga** se activa cuando la primera MA cruza **por encima** de la segunda MA entre la barra anterior y la actual.
3. Una entrada **corta** se activa cuando la primera MA cruza **por debajo** de la segunda MA entre la barra anterior y la actual.
4. Cuando el filtro está habilitado, las señales largas requieren que la primera MA se mantenga **por debajo** de la MA del filtro, mientras que las señales cortas requieren que se mantenga **por encima** de la MA del filtro.

### Paso 2 – Confirmación diferida
Si ninguna señal se activa en el Paso 1, la estrategia verifica un cruce que comenzó dos barras atrás pero aún es válido. Esto refleja el comportamiento original del EA que busca en el historial reciente cruces perdidos. Para evitar llenados repetidos, la señal solo se activa cuando han pasado al menos tres barras desde el último trade.

### Ejecución de órdenes
- Las entradas se ejecutan al precio de mercado. Las posiciones opuestas se cierran antes de abrir en la nueva dirección.
- Las salidas ocurren cuando se tocan los niveles de stop loss, take profit o trailing stop en la vela actual. La operación se cierra con una orden de mercado una vez que se viola un nivel protector.

## Gestión de riesgos
- Las distancias de **Stop Loss** y **Take Profit** se configuran en pips. Se convierten en offsets de precio usando el `PriceStep` del instrumento (por defecto `1` cuando no está disponible).
- El **Trailing Stop** comienza desde el precio de entrada y sigue el movimiento de precio favorable. El stop se actualiza cada vez que el mejor precio avanza al menos `TrailingStepPips` pips más allá del nivel de trailing anterior.
- Si tanto un stop fijo como un trailing stop están activos, la estrategia usa el nivel más conservador (más alto para posiciones largas, más bajo para posiciones cortas).

## Dimensionamiento de posición
- Cuando `UseRiskPercent` es **true**, el volumen equivale a `Equity * RiskPercent / (StopLossPips * PipValue)`. Si no se define stop, la estrategia recurre al volumen fijo.
- Cuando `UseRiskPercent` es **false**, el tamaño de la operación es siempre `FixedVolume`.
- `PipValue` debe reflejar el valor monetario de un solo pip por un lote/contrato del instrumento negociado.

## Notas de implementación
- La implementación de StockSharp trabaja completamente en velas cerradas y no registra órdenes pendientes. Los usuarios que necesiten entradas de stop o límite pueden extender la estrategia en consecuencia.
- El filtro de tercera media móvil puede deshabilitarse para operar cada cruce, coincidiendo con la opción `InpFilterMA = false` del EA.
- Asegúrese de que el tipo de vela, el paso de precio y los parámetros de valor de pip coincidan con el instrumento negociado para un control de riesgo correcto.

## Parámetros
| Nombre | Descripción | Por defecto |
| --- | --- | --- |
| `FirstPeriod` | Período de la primera media móvil. | 5 |
| `FirstShift` | Desplazamiento (barras) aplicado a la salida de la primera media móvil. | 3 |
| `FirstMethod` | Método de suavizado de la primera media móvil (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Smoothed` |
| `FirstAppliedPrice` | Precio aplicado para la primera media móvil (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `SecondPeriod` | Período de la segunda media móvil. | 8 |
| `SecondShift` | Desplazamiento (barras) aplicado a la salida de la segunda media móvil. | 5 |
| `SecondMethod` | Método de suavizado para la segunda media móvil. | `Smoothed` |
| `SecondAppliedPrice` | Precio aplicado para la segunda media móvil. | `Close` |
| `UseFilter` | Habilita el filtro de dirección de la tercera media móvil. | `true` |
| `ThirdPeriod` | Período del filtro de la tercera media móvil. | 13 |
| `ThirdShift` | Desplazamiento (barras) aplicado a la salida de la tercera media móvil. | 8 |
| `ThirdMethod` | Método de suavizado para el filtro de la tercera media móvil. | `Smoothed` |
| `ThirdAppliedPrice` | Precio aplicado para el filtro de la tercera media móvil. | `Close` |
| `UseRiskPercent` | Alterna entre volumen fijo y dimensionamiento de posición basado en porcentaje. | `true` |
| `FixedVolume` | Tamaño de la operación cuando el dimensionamiento fijo está activo. | 0.1 |
| `RiskPercent` | Fracción del capital de la cuenta arriesgada por operación. | 5 |
| `PipValue` | Valor monetario de un pip por lote/contrato. | 1 |
| `StopLossPips` | Distancia del stop-loss en pips. | 50 |
| `TakeProfitPips` | Distancia del take-profit en pips. | 50 |
| `TrailingStopPips` | Distancia del trailing stop en pips. | 10 |
| `TrailingStepPips` | Incremento mínimo de pips requerido para avanzar el trailing stop. | 4 |
| `CandleType` | Tipo de datos de vela / marco temporal usado por la estrategia. | Velas de 1 minuto |
