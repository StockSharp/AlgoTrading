# iVIDyA Estrategia Simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una adaptación de alto nivel StockSharp del experto MetaTrader **"iVIDyA Simple"**. Opera con un solo símbolo siguiendo un promedio dinámico de índice variable (VIDYA) que se adapta al impulso del mercado a través del oscilador de impulso Chande (CMO). Cada vez que la vela terminada más reciente cruza la línea VIDYA desplazada, la estrategia abre una posición de mercado en la dirección de la ruptura y, opcionalmente, adjunta órdenes protectoras de stop-loss y take-profit.

## Lógica de trading
1. Los datos de las velas se leen desde el período de tiempo configurado (`CandleType`).
2. El CMO con período `CmoPeriod` está vinculado a la serie de velas. Su valor absoluto escala dinámicamente el factor de suavizado de VIDYA. El factor base es igual a `2 / (EmaPeriod + 1)` al igual que la implementación original de MQL.
3. Se mantiene un valor VIDYA móvil. En cada vela terminada el algoritmo:
   - Selecciona el precio aplicado (`AppliedPrice`) de la vela (cierre, apertura, mediana, etc.).
   - Actualiza VIDYA con el coeficiente de suavizado adaptativo.
   - Almacena valores históricos para emular la opción `MA shift` de MetaTrader.
4. La vela se compara con el valor VIDYA desplazado (`MaShift` barras hacia atrás):
   - Si la vela se abre por debajo de VIDYA y cierra por encima de ella, se genera una señal de **compra**.
   - Si la vela se abre por encima de VIDYA y cierra por debajo de ella, se genera una señal de **venta**.
5. Antes de abrir una nueva posición, la estrategia aplana cualquier exposición opuesta negociando todo el volumen necesario para revertir.
6. Después de cada entrada, se llama a `SetStopLoss` y `SetTakeProfit` cuando las distancias respectivas son positivas.

Esto refleja el asesor experto original que activaba órdenes estrictamente en barras nuevas, utilizaba un VIDYA calculado a partir de CMO y EMA períodos, y adjuntaba paradas opcionales expresadas en puntos.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `Volume` | `1` | Volumen de negociación base utilizado para las órdenes. La exposición existente se compensa automáticamente al invertir posiciones. |
| `StopLossPoints` | `150` | Distancia de stop-loss en pasos de precio. Establezca en `0` para desactivar. |
| `TakeProfitPoints` | `460` | Distancia de obtención de beneficios en pasos de precio. Establezca en `0` para desactivar. |
| `CmoPeriod` | `15` | Longitud del oscilador Chande Momentum que determina el peso adaptativo de VIDYA. |
| `EmaPeriod` | `12` | EMA longitud que define el coeficiente de suavizado base en la fórmula VIDYA. |
| `MaShift` | `1` | Número de velas completadas utilizadas para mover la línea VIDYA hacia adelante, coincidiendo con la entrada `ma_shift` del indicador MetaTrader. |
| `AppliedPrice` | `Close` | Fuente de precio pasada al cálculo de VIDYA (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `CandleType` | `TimeSpan.FromMinutes(5)` | Tipo de vela y período de tiempo utilizados para todos los cálculos y señales. |

## Notas adicionales
- Las órdenes de protección se administran a través del API (`SetStopLoss`/`SetTakeProfit`) integrado de alto nivel, mientras que el código original MQL realizaba verificaciones manuales de los niveles de congelación.
- La estrategia se suscribe únicamente a velas terminadas, replicando la restricción de ejecución de "nueva barra" de MetaTrader.
- El historial de VIDYA se recorta automáticamente para que el uso de memoria se mantenga pequeño incluso cuando `MaShift` es grande.
- Todos los comentarios dentro del código están escritos en inglés para cumplir con los requisitos del proyecto.
