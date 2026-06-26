# Estrategia de Promediado de Posición MA MACD v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La **Estrategia de Promediado de Posición MA MACD v2** es una traducción directa del asesor experto de MetaTrader de Vladimir
Karputov. Combina un filtro de media móvil ponderada, un bloque de confirmación MACD y un módulo de promediado que aumenta la
exposición cuando las operaciones existentes se mueven en contra de la posición. La versión de StockSharp mantiene la jerarquía
de señales original, procesa indicadores en velas terminadas y gestiona la lógica de protección (stop loss, take profit, trailing)
en código para reproducir el comportamiento del lado del broker en MQL.

## Lógica de Trading
1. **Preparación de indicadores**
   - Una media móvil configurable calcula en el tipo de vela y componente de precio seleccionados. El parámetro `MaShift` emula
     el desplazamiento hacia adelante de MetaTrader leyendo valores de velas más antiguas, mientras que `BarOffset` permite
     evaluar la barra actual o una anterior.
   - Un indicador de señal MACD produce las líneas principal y de señal usando períodos rápido, lento y de señal personalizables
     y un precio aplicado que coincide con el asesor experto original.
2. **Validación de señales**
   - Los setups largos requieren que ambas líneas MACD sean negativas, que el precio esté por encima de la media móvil
     desplazada y que la distancia del precio a la media supere `MaIndentPips` (convertido a precio absoluto usando el tamaño
     de pip del instrumento).
   - Los setups cortos reflejan las condiciones: ambas líneas MACD deben ser positivas, el precio debe mantenerse por debajo
     de la media móvil desplazada y la brecha a la media debe ser al menos `MaIndentPips`.
   - El filtro de ratio `MacdRatio` impone `MACD_main / MACD_signal >= MacdRatio` (usando división decimal absoluta) antes
     de permitir una operación.
   - Cuando `ReverseSignals = true`, la dirección de la orden de mercado se invierte después de que pasan todas las condiciones.
3. **Ciclo de vida de la posición**
   - Si **no existe posición**, la estrategia abre una orden de mercado con el `OrderVolume` configurado (redondeado por el
     paso de volumen del instrumento) en la dirección calculada. Los niveles de stop-loss y take-profit se aplican
     inmediatamente según `StopLossPips` y `TakeProfitPips`.
   - Si **ya existe una exposición**, la estrategia nunca abre el lado opuesto. En cambio:
     - Cierra todo si se detectan simultáneamente largos y cortos (red de seguridad que refleja la comprobación MQL), o
     - Invoca el bloque de promediado para el lado actual.
4. **Módulo de promediado**
   - Para largos, el algoritmo encuentra el tramo abierto con el precio más bajo cuya pérdida no realizada supera
     `StepLossPips`. Para cortos selecciona el tramo perdedor con el precio más alto.
   - Una vez que se encuentra un candidato, se envía una nueva orden de mercado con volumen `CandidateVolume × LotCoefficient`
     (después de ajustar al paso/mín/máx de volumen permitido). Esto reproduce la progresión geométrica del experto original.
   - Los nuevos tramos heredan las mismas distancias de stop-loss y take-profit y se vuelven elegibles para actualizaciones
     de trailing.
5. **Controles de riesgo**
   - Un trailing stop se activa solo cuando tanto `TrailingStopPips` como `TrailingStepPips` son mayores que cero. Para
     largos, el stop se mueve a `Close - TrailingStopPips` una vez que el beneficio supera `TrailingStopPips + TrailingStepPips`;
     los cortos se comportan simétricamente.
   - Las comprobaciones manuales de stop-loss y take-profit se realizan en cada vela terminada. Cuando se activan, una orden
     de mercado cierra el tramo exacto y lo elimina de la lista de promediado.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| **OrderVolume** | Volumen base para la primera operación en un ciclo. |
| **StopLossPips** | Distancia del stop-loss en pips. Poner en cero para deshabilitar el stop. |
| **TakeProfitPips** | Distancia del take-profit en pips. Poner en cero para deshabilitar el objetivo. |
| **TrailingStopPips** | Distancia entre el precio y el trailing stop. Funciona junto con `TrailingStepPips`. |
| **TrailingStepPips** | Movimiento favorable adicional requerido antes de actualizar el trailing stop. |
| **StepLossPips** | Pérdida mínima (en pips) requerida antes de añadir un tramo de promediado. |
| **LotCoefficient** | Multiplicador aplicado al volumen del tramo perdedor seleccionado al promediar. |
| **BarOffset** | Número de barras atrás para leer valores de indicadores (0 = barra terminada actual). |
| **ReverseSignals** | Invierte la ejecución largo/corto manteniendo los mismos filtros. |
| **MaPeriod** | Período de la media móvil. |
| **MaShift** | Desplazamiento hacia adelante aplicado a la media móvil (estilo MetaTrader). |
| **MaMethod** | Método de suavizado de la media móvil (Simple, Exponencial, Suavizado, Ponderado). |
| **MaPrice** | Componente de precio de la vela usado para la media móvil. |
| **MaIndentPips** | Distancia mínima del precio desde la media móvil antes de entrar. |
| **MacdFastPeriod** | Período de EMA rápida para MACD. |
| **MacdSlowPeriod** | Período de EMA lenta para MACD. |
| **MacdSignalPeriod** | Período de EMA de señal para MACD. |
| **MacdPrice** | Precio aplicado usado en el cálculo del MACD. |
| **MacdRatio** | Ratio mínimo entre las líneas principal y de señal del MACD. |
| **CandleType** | Serie de velas usada para todos los cálculos. |

## Notas de Implementación
- El tamaño del pip se calcula a partir del paso de precio del instrumento, reproduciendo el ajuste de 3/5 dígitos de la
  versión MQL. Esto mantiene idénticas las distancias basadas en pips en los símbolos Forex.
- Todos los búferes de indicadores usan colas para emular la indexación `ma_shift` y `bar` de MetaTrader sin llamar a métodos
  de búsqueda histórica prohibidos por las reglas del proyecto.
- Los ajustes de volumen respetan `Security.VolumeStep`, `Security.MinVolume` y `Security.MaxVolume`, evitando tamaños de
  orden inválidos cuando `LotCoefficient` multiplica la exposición.
- La lógica de protección (stops, takes, trailing) se ejecuta completamente en la capa de estrategia, por lo que no hay
  dependencia de las APIs de modificación de posición del broker.
- La clase reside en el espacio de nombres `StockSharp.Samples.Strategies` y sigue el requisito del repositorio de usar
  sangría de tabulación y comentarios exclusivamente en inglés.
