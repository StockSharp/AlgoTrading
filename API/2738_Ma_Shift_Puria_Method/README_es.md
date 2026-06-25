# Estrategia Ma Shift Puria Method
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Ma Shift Puria Method es una implementación del clásico Expert Advisor "Puria" adaptado para la API de alto nivel de StockSharp. El algoritmo combina múltiples medias móviles exponenciales (EMAs) con un filtro MACD y una lógica de trailing opcional basada en fractales. Las señales se evalúan únicamente en velas completadas. La gestión de posiciones incluye niveles fijos de stop-loss y take-profit, trailing stops configurables y un modo de trailing fractal opcional que asegura ganancias cerca del objetivo cuando aparece un punto de swing confirmado.

## Indicadores y cálculos
- **EMA rápida (por defecto 14)** – captura el momentum a corto plazo y define la pendiente de la media rápida.
- **EMA lenta (por defecto 80)** – representa la dirección más amplia del mercado. La distancia entre las EMAs rápida y lenta debe superar un umbral en pips definido por el usuario para validar señales.
- **MACD (rápido 11, lento 102, señal 9)** – confirma el momentum direccional requiriendo que la línea principal cruce el eje cero en la dirección de la operación mientras estaba en el lado opuesto tres barras antes.
- **Ventana fractal (5 barras)** – se usa cuando el trailing fractal está habilitado. La estrategia deriva máximos y mínimos de swing de un buffer de cinco barras móviles, coincidiendo con la definición fractal de MetaTrader (la barra central es el extremo local comparado con dos barras a cada lado).

## Lógica de entrada
Se abre una nueva posición solo cuando la estrategia tiene permitido operar y las siguientes condiciones son verdaderas en la vela completada más reciente:

### Entrada larga
1. La EMA rápida está por encima de la EMA lenta.
2. La EMA lenta está tendiendo hacia arriba en comparación con su valor de tres barras atrás.
3. La EMA rápida tiene una pendiente ascendente (valor actual por encima del valor anterior).
4. La línea principal MACD está por encima de cero y estaba por debajo de cero tres barras atrás.
5. La EMA rápida aumentó más que el **Shift Minimum** configurado (en pips) entre las dos últimas barras, y o bien sigue acelerando o el incremento previo era no positivo.

### Entrada corta
1. La EMA rápida está por debajo de la EMA lenta.
2. La EMA lenta está tendiendo hacia abajo en comparación con tres barras atrás.
3. La EMA rápida tiene una pendiente descendente (valor actual por debajo del valor anterior).
4. La línea principal MACD está por debajo de cero y estaba por encima de cero tres barras atrás.
5. La EMA rápida disminuyó más que el umbral **Shift Minimum** y o bien sigue acelerando o el incremento previo era no negativo.

La estrategia abre posiciones en incrementos fijos (volumen manual) o unidades de tamaño dinámico basadas en el riesgo de cartera, dependiendo del modo elegido. Cuando hay una posición opuesta abierta, el algoritmo la cierra y abre una nueva en la dirección actual en una sola orden de mercado.

## Salida y gestión de riesgo
- **Stop Loss** – establecido en pips relativo al precio de entrada. Si el mínimo/máximo de la vela toca el nivel de protección, la posición se cierra inmediatamente.
- **Take Profit** – también expresado en pips. Alcanzar el objetivo cierra toda la posición.
- **Trailing Stop** – cuando está habilitado, el nivel de stop sigue el precio a la distancia configurada después de que las ganancias superen la distancia de trailing más el paso de trailing. La lógica refleja el experto MQL original, actualizando solo cuando el stop puede moverse al menos el paso de trailing.
- **Trailing Fractal** – opcional. Una vez que el precio cubre el 95% de la distancia al take-profit, el stop puede moverse al último mínimo de swing (largo) o máximo de swing (corto) identificado por el patrón fractal de cinco barras, apretando el riesgo mientras deja margen para un breakout.
- **Dimensionamiento basado en riesgo** – si el volumen manual está deshabilitado, la estrategia arriesga un porcentaje fijo de la cartera por operación. Divide el capital en riesgo por la distancia monetaria del stop y redondea el resultado al paso de volumen permitido más cercano dentro de los límites del exchange.

## Parámetros
| Nombre | Descripción | Por defecto |
|------|-------------|---------|
| `UseManualVolume` | Alternar entre volumen fijo y dimensionamiento basado en riesgo. | `true` |
| `ManualVolume` | Volumen usado por operación cuando el dimensionamiento manual está activo. | `0.1` |
| `RiskPercent` | Porcentaje del capital arriesgado por operación (usado cuando `UseManualVolume` es false). | `9` |
| `StopLossPips` | Distancia del stop-loss en pips. | `45` |
| `TakeProfitPips` | Distancia del take-profit en pips. | `75` |
| `TrailingStopPips` | Distancia del trailing stop en pips. | `15` |
| `TrailingStepPips` | Movimiento mínimo en pips antes de actualizar el trailing stop. | `5` |
| `MaxPositions` | Número máximo de unidades de posición que pueden acumularse en una dirección. | `1` |
| `ShiftMinPips` | Pendiente mínima de EMA en pips requerida para una señal válida. | `20` |
| `FastLength` | Longitud de la EMA rápida. | `14` |
| `SlowLength` | Longitud de la EMA lenta. | `80` |
| `MacdFast` | Período rápido del MACD. | `11` |
| `MacdSlow` | Período lento del MACD. | `102` |
| `UseFractalTrailing` | Habilitar/deshabilitar ajustes de trailing fractal. | `false` |
| `CandleType` | Tipo de vela (marco temporal) usado para los cálculos. | `15 minutos` |

## Notas de implementación
- La estrategia se suscribe a un flujo de velas y vincula los indicadores EMA y MACD vía `SubscribeCandles().Bind(...)`, asegurando que los valores de los indicadores se reciban en el manejador de señales sin consultas manuales al buffer.
- El estado interno rastrea los últimos tres valores de EMA y MACD para imitar el indexado `shift` de MQL requerido por la lógica original.
- Los fractales se calculan localmente usando una ventana de cinco barras, coincidiendo con el comportamiento de MetaTrader sin llamar a `GetValue` en el indicador.
- La gestión de stop y take-profit se realiza con salidas de mercado cuando se violan los niveles de precio, reflejando el efecto de las modificaciones de posición originales.
- La llamada `StartProtection()` habilita el monitoreo de posiciones integrado de StockSharp para resiliencia durante desconexiones inesperadas.

## Recomendaciones de uso
1. Seleccione un tipo de vela apropiado (p. ej., barras de 15 minutos para pares de divisas principales) para reflejar la configuración original de Puria.
2. Ajuste los parámetros basados en pips para que coincidan con el valor de punto del instrumento. El helper escala automáticamente a cotizaciones de cinco dígitos, pero los instrumentos exóticos pueden requerir ajuste personalizado.
3. Al habilitar el dimensionamiento basado en riesgo, verifique la valoración de la cartera y las restricciones de paso de volumen para asegurar que el volumen calculado sea negociable.
4. Combine con gestión de capital a nivel de cartera o filtros de sesión si es necesario; la estrategia se centra estrictamente en la señal y la lógica de trailing del experto MQL original.
