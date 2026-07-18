# Estrategia de cruce por cero CCIT3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia CCIT3 Zero Cross es un puerto StockSharp del asesor experto MetaTrader 5 que negocia reversiones de línea cero del oscilador CCIT3. El indicador se construye aplicando la cadena de suavizado Tillson T3 a un índice de canal de productos básicos (CCI). Cada vez que los interruptores del oscilador suavizados firman, la estrategia abre una nueva posición en la dirección del giro o, si está configurada, cierra la posición actual y la invierte.

## Lógica comercial
- Calcule el CCI utilizando el precio aplicado y el período seleccionados.
- Suavice el oscilador con una tubería Tillson T3. Se proporcionan dos modos de cálculo:
  - **Simple**: suavizado persistente de seis etapas que se comporta como el indicador de recálculo original MetaTrader.
  - **NoRecalc**: evalúa el polinomio T3 solo para la barra más reciente, recreando la versión ligera "sin recálculo" del código fuente.
- Cuando el valor CCIT3 cruce de positivo a negativo, abra una posición larga (o invierta una posición corta si `Trade Overturn` está habilitado).
- Cuando el valor CCIT3 cruce de negativo a positivo, abra una posición corta (o invierta una posición larga si `Trade Overturn` está habilitado).
- Los niveles opcionales de take-profit, stop-loss y trailing stop se gestionan a través del asistente `StartProtection` de StockSharp.

## Indicadores y cálculos.
- **Índice de canales de productos básicos (CCI)**: se ejecuta según el precio aplicado configurable (cierre, apertura, máximo, mínimo, mediana, típico, ponderado) y el período.
- **Suavizado Tillson T3**: implementado exactamente como en el indicador MQL5 con el factor de volumen `B`. El modo Simple mantiene cadenas EMA con estado en las barras, mientras que NoRecalc vuelve a calcular el polinomio a partir de la última lectura sin procesar de CCI.
- **Detección de cruce por cero**: las operaciones se activan estrictamente en velas terminadas, reflejando las comprobaciones originales de barras nuevas en el asesor experto.

## Gestión de riesgos y posiciones.
- `Take Profit (pts)` y `Stop Loss (pts)` se convierten en distancias de precios absolutas utilizando el `PriceStep` del instrumento.
- `Trailing Stop (pts)` activa el motor de seguimiento de StockSharp con la misma distancia de punto.
- `Max Drawdown Target` reescala el volumen de pedido base utilizando el valor de cartera actual o inicial (`volume = OrderVolume * balance / target`). Deje el parámetro en cero para mantener un tamaño de lote fijo.
- `Trade Overturn` permite la reversión completa: la posición actual se cierra primero y luego se abre una nueva en la dirección opuesta.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Volume` | 1 | Volumen de orden base antes de cualquier escala de reducción. |
| `Take Profit (pts)` | 1750 | Distancia de toma de ganancias en puntos. |
| `Stop Loss (pts)` | 0 | Distancia de stop-loss en puntos. |
| `Trailing Stop (pts)` | 0 | Distancia del trailing stop en puntos (0 desactiva el trailing). |
| `Trade Overturn` | falso | Invierta la posición en señales CCIT3 opuestas. |
| `CCI Period` | 285 | Período retroactivo para el indicador CCI. |
| `CCI Price` | Típico | Precio aplicado utilizado para alimentar el CCI. |
| `T3 Period` | 60 | Longitud de alisado Tillson T3. |
| `T3 Volume Factor` | 0,618 | Coeficiente Tillson T3 `B`. |
| `Mode` | Sencillo | Modo de cálculo CCIT3 (`Simple` o `NoRecalc`). |
| `Candle Type` | plazo de 1 hora | Plazo utilizado para las suscripciones de velas. |
| `Max Drawdown Target` | 0 | Divisor de equilibrio para dimensionamiento de volumen adaptable (0 deshabilita el escalado). |

## Notas de implementación
- La estrategia se suscribe a una única fuente de velas especificada por `Candle Type` y procesa solo velas completadas.
- Todos los valores de volumen están alineados con el paso de volumen del valor y limitados por `VolumeMin`/`VolumeMax`.
- Los parámetros predeterminados replican la configuración MT5 publicada: modo simple CCIT3 con un período CCI de 285, longitud T3 de 60 y factor de volumen de 0,618.
- Cambiar a NoRecalc mantiene el comportamiento del indicador original de reaccionar instantáneamente al signo CCI sin procesar mientras sigue produciendo señales positivas/negativas.
