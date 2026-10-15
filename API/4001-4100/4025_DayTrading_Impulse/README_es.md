# Estrategia de impulso de negociación intradía
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia DayTrading** es una fiel conversión de C# del clásico MetaTrader 4 asesor experto "DayTrading" lanzado por NazFunds en 2005. El robot original fue diseñado para gráficos Forex de 5 minutos y combina múltiples indicadores de impulso y seguimiento de tendencias para capturar movimientos direccionales a corto plazo con un modesto objetivo fijo y un trailing stop opcional. Esta implementación StockSharp reproduce la lógica de decisión central al tiempo que expone cada umbral importante como un parámetro de estrategia para que pueda optimizarse o adaptarse a diferentes instrumentos.

## Pila de indicadores

La estrategia evalúa cuatro indicadores en la serie de velas seleccionada:

- **Parabolic SAR** (`ParabolicSar`) con aceleración, incremento y límite configurables. Define la dirección de la tendencia de referencia y tiene que girar por debajo o por encima del precio para permitir nuevas entradas.
- **MACD (12, 26, 9)** (`MovingAverageConvergenceDivergenceSignal`). La línea MACD debe estar debajo de la línea de señal para largos y por encima para cortos, reflejando la comparación de histograma/señal original en MQL.
- **Stochastic Oscilador (5, 3, 3)** (`StochasticOscillator`). La línea %K debe permanecer por debajo de 35 para largos y por encima de 60 para cortos para garantizar que el mercado salga de una zona de sobreventa/sobrecompra.
- **Impulso (14)** (`Momentum`). Un valor inferior a 100 desbloquea operaciones largas, mientras que un valor superior a 100 autoriza operaciones cortas, exactamente como en el script MT4.

Todos los indicadores se procesan a través del proceso de alto nivel `BindEx`, por lo que no se requiere gestión manual del búfer ni indexación histórica.

## Reglas de trading

### Condiciones de entrada

Una posición **larga** se abre cuando se cumple todo lo siguiente en la última vela terminada:

1. El punto Parabolic SAR se imprime al precio de venta actual o por debajo de él **y** el punto anterior estaba por encima del punto actual (nuevo cambio de SAR a alcista).
2. El impulso está por debajo de 100.
3. La línea MACD está debajo de su línea de señal.
4. Stochastic %K está por debajo de 35.

Se abre una posición **corta** cuando se cumplen las condiciones simétricas:

1. El punto Parabolic SAR se imprime al precio de oferta actual o por encima de él **y** el punto anterior estaba por debajo del punto actual (giro bajista).
2. El impulso está por encima de 100.
3. La línea MACD está por encima de su línea de señal.
4. Stochastic %K está por encima de 60.

Sólo se puede abrir una posición a la vez. Siempre que aparece una señal opuesta, la posición existente se cierra y no se produce ningún reingreso en la misma vela, al igual que en la implementación MetaTrader donde el escaneo `OrdersTotal` evita la recarga inmediata.

### Gestión de salidas

- **Stop Loss / Take Profit:** Las distancias fijas opcionales (en puntos) se convierten a precios absolutos utilizando el tamaño del tick del instrumento. Se reevalúan en cada vela y cierran la posición si se infringe la intrabarra.
- **Trailing Stop:** Una vez que el precio avanza según el número de puntos configurado, se activa un trailing stop. Para operaciones largas, el stop queda por debajo del cierre; para operaciones cortas, se sitúa por encima del cierre. El stop nunca retrocede, por lo que el beneficio se bloquea progresivamente.
- **Señal opuesta:** Una configuración opuesta válida liquida inmediatamente la posición actual antes de que se considere cualquier nueva entrada.

No se agrega lógica adicional de cuadrícula, escalamiento o cobertura; la estrategia sigue siendo tan ligera y determinista como la EA original.

## Parámetros

| Parámetro | Predeterminado | Descripción |
| --- | --- | --- |
| `LotSize` | 1 | Volumen de cada orden de mercado. La propiedad `Strategy.Volume` se sincroniza con este valor durante el inicio. |
| `TrailingStopPoints` | 15 | Distancia de seguimiento en puntos. Establezca en cero para desactivar el seguimiento. |
| `TakeProfitPoints` | 20 | Distancia de toma de ganancias fija en puntos. Establezca en cero para eliminar el objetivo. |
| `StopLossPoints` | 0 | Distancia de parada de protección en puntos. Zero reproduce el comportamiento original de "no parar". |
| `SlippagePoints` | 3 | Marcador de posición de deslizamiento máximo de ejecución (para compatibilidad con la entrada MT4). No se aplica automáticamente, pero se mantiene para que esté completo. |
| `CandleType` | marco de tiempo de 5 minutos | Serie de velas utilizadas por todos los indicadores. Manténgase en M5 para que coincida con la recomendación original de EA. |
| `MacdFastPeriod` | 12 | Longitud rápida de EMA en el cálculo de MACD. |
| `MacdSlowPeriod` | 26 | Longitud lenta de EMA en el cálculo de MACD. |
| `MacdSignalPeriod` | 9 | Longitud de la señal EMA en el cálculo MACD. |
| `StochasticLength` | 5 | %K longitud retrospectiva para el oscilador Stochastic. |
| `StochasticSignal` | 3 | %D longitud de suavizado. |
| `StochasticSlow` | 3 | Ralentización adicional aplicada a la línea %K. |
| `MomentumPeriod` | 14 | Longitud retrospectiva del impulso. |
| `SarAcceleration` | 0,02 | Factor de aceleración inicial para Parabolic SAR. |
| `SarStep` | 0,02 | Incremento aplicado al factor de aceleración después de cada nuevo extremo. |
| `SarMaximum` | 0,2 | Factor de aceleración máximo para Parabolic SAR. |

Todos los parámetros numéricos se pueden optimizar a través del flujo de trabajo de optimización de StockSharp gracias a las sugerencias de `SetCanOptimize(true)`.

## Notas de implementación

- Los precios de oferta/demanda se derivan de datos en vivo de Nivel 1 cuando están disponibles; de lo contrario, el cierre de la vela actúa como un respaldo para que la lógica siga siendo sólida en las pruebas históricas.
- La conversión de puntos depende del `Step`/`PriceStep` del instrumento. Si no se proporciona ninguno, se utiliza un respaldo conservador `0.0001`, que coincide con un pip estándar de Forex.
- La gestión de posiciones refleja el MT4 EA: la estrategia nunca es piramidal y nunca mantiene ambas direcciones simultáneamente.
- Los comentarios dentro del código están en inglés según las pautas del proyecto, mientras que este README incluye documentación ampliada para facilitar la incorporación.

## Consejos de uso

1. Asigne el par de Forex deseado a la estrategia, deje el tipo de vela en 5 minutos y comience la estrategia. Los indicadores se calentarán automáticamente.
2. Considere habilitar un stop loss distinto de cero cuando se ejecute con datos en vivo; el script original recomendaba operar sin él, pero los trailingstops por sí solos pueden no ser suficientes para el control de riesgos.
3. Para carteras algorítmicas, puede agregar esta estrategia a un `BasketStrategy` y administrar la asignación de capital externamente mientras aún se beneficia de los parámetros expuestos para la optimización.

Esta documentación, junto con las traducciones al ruso y al chino en la misma carpeta, proporciona total transparencia de la lógica convertida.
