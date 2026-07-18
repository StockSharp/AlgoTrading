# Estrategia de 800BB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el asesor experto MetaTrader 4 "800BB" utilizando la API de alto nivel de StockSharp. Realiza operaciones de reversión a la media cuando el precio perfora una Bollinger Band muy larga y vuelve a entrar inmediatamente al canal en la siguiente barra. El riesgo se controla mediante distancias de stop y take-profit basadas en ATR combinadas con un dimensionamiento dinámico de posición basado en el porcentaje de riesgo configurado.

## Descripción general

- Funciona en cualquier instrumento y marco temporal suministrado a través del parámetro `CandleType`.
- Utiliza una Bollinger Band de 800 períodos con un envolvente de dos desviaciones estándar para detectar excursiones extremas.
- Confirma entradas en la barra que abre de vuelta dentro de la banda justo después de un cierre exterior.
- Dimensiona las órdenes estimando la distancia de stop derivada del ATR en pips y aplicando el `RiskPercent` seleccionado al valor actual de la cartera.
- Replica el cálculo de pips de MetaTrader multiplicando el paso de precio por 10 cuando el símbolo tiene 3 o 5 decimales.

## Lógica de trading

### Configuración larga

1. La vela completada anterior abrió o cerró por debajo de la banda inferior de Bollinger, señalando una excursión de sobrecompra.
2. La vela actual abre en o por encima del nivel anterior de la banda inferior (el precio ha vuelto a entrar en el canal).
3. No hay ninguna posición larga actualmente activa. Cualquier posición corta abierta se cierra antes de abrir la nueva larga.
4. El tamaño de la posición se calcula usando la distancia de stop basada en ATR y el porcentaje de riesgo configurado.
5. Se envía una orden de compra de mercado en la apertura de la vela. El stop-loss se coloca `StopLossAtrMultiplier × ATR` por debajo del precio de entrada, mientras que el take-profit es `TakeProfitAtrMultiplier × ATR` por encima.

### Configuración corta

1. La vela completada anterior abrió o cerró por encima de la banda superior de Bollinger, señalando una excursión de sobrecompra.
2. La vela actual abre en o por debajo del nivel anterior de la banda superior (el precio ha vuelto a entrar en el canal).
3. No hay ninguna posición corta actualmente activa. Cualquier posición larga abierta se cierra antes de abrir la nueva corta.
4. El tamaño de la posición se determina mediante el mismo cálculo de ATR y porcentaje de riesgo.
5. Se envía una orden de venta de mercado en la apertura de la vela. El stop-loss se coloca `StopLossAtrMultiplier × ATR` por encima del precio de entrada, mientras que el take-profit es `TakeProfitAtrMultiplier × ATR` por debajo.

### Gestión de salidas

- **Órdenes protectoras:** Los niveles de stop-loss y take-profit se rastrean internamente y se evalúan en cada vela completada. Si se supera cualquiera de los umbrales, la posición se cierra a mercado.
- **Señales opuestas:** Cuando se activa una configuración opuesta, la posición actual se cierra antes de colocar la nueva orden.
- **Visualización:** El EA original podía dibujar líneas verticales para operaciones potenciales. Las anotaciones de gráfico no se recrean aquí; en cambio, la estrategia dibuja velas, la Bollinger Band y las propias operaciones cuando hay un área de gráfico disponible.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `RiskPercent` | `2` | Porcentaje del valor de la cartera arriesgado por operación. |
| `TakeProfitAtrMultiplier` | `1.5` | Múltiplo de ATR utilizado para calcular la distancia del take-profit. |
| `StopLossAtrMultiplier` | `1` | Múltiplo de ATR utilizado para calcular la distancia del stop-loss. |
| `AtrPeriod` | `14` | Período de retrospectiva para el indicador ATR. |
| `BollingerPeriod` | `800` | Período de la media móvil de la Bollinger Band. |
| `BollingerDeviation` | `2` | Multiplicador de desviación estándar para la Bollinger Band. |
| `CandleType` | `1 hour` | Marco temporal (o cualquier otro tipo de vela) utilizado para la generación de señales. |

## Notas

- Asegúrese de que el adaptador de cartera proporcione `Portfolio.CurrentValue`; de lo contrario, el dimensionamiento de posición basado en riesgo devuelve cero y la estrategia no operará.
- Si el símbolo no expone un paso de precio o valor de tick válido, los cálculos de pips y dinero por pip recurren a valores predeterminados conservadores.
- El largo período de retrospectiva de Bollinger (800 barras) significa que la primera operación solo puede ocurrir después de recibir suficientes datos históricos para calentar tanto los indicadores de Bollinger como de ATR.
