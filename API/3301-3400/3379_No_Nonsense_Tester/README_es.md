# Estrategia de prueba sin tonterías
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de probador sin tonterías** es una versión StockSharp del asesor experto MQL4 "NoNonsenseTester". La implementación se centra en el flujo de trabajo principal de NNFX que valida una línea base de tendencia, espera dos indicadores de confirmación, verifica la volatilidad usando ATR y supervisa las operaciones con una lógica de salida estricta. La estrategia está diseñada para la experimentación con múltiples parámetros y, por lo tanto, expone todos los umbrales importantes a través de objetos `StrategyParam` para que puedan optimizarse dentro de StockSharp.

## Lógica de trading
1. **Filtro de línea de base**: un EMA con longitud configurable define la dirección de la tendencia principal. Las entradas solo se consideran cuando el precio cierra en la línea de base.
2. **Confirmación n.º 1**: un RSI debe estar en el lado alcista (por encima del umbral) o bajista (por debajo del umbral complementario) para confirmar la ruptura de la línea base.
3. **Confirmación n.° 2**: un CCI debe estar de acuerdo con la tendencia y exceder la magnitud absoluta configurada para bloquear señales débiles.
4. **Filtro de volatilidad**: ATR debe ser mayor que el valor de `AtrMinimum`, lo que garantiza que las operaciones se realicen solo cuando el mercado muestre un rango suficiente.
5. **Entrada** – cuando la línea base se cruza, las dos confirmaciones y el filtro de volatilidad están alineados, la estrategia abre una posición en la dirección del movimiento. El tamaño de la posición puede escalar opcionalmente con ATR a través del parámetro `AtrEntryMultiplier`.
6. **Stop and target**: inmediatamente después de la entrada, la estrategia calcula los niveles de stop loss y takeprofit basados en ATR. El seguimiento opcional ATR sigue actualizando el stop de protección mientras la operación se mueve a favor.
7. **Superposición de salida**: un RSI adicional con un período más corto supervisa las operaciones abiertas. Si cruza por debajo de la banda inferior para largos o por encima de la banda superior para cortos, la posición se cierra incluso si el precio no ha tocado los niveles de protección.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `BaselineLength` | Período de la línea base EMA. |
| `ConfirmationRsiLength` | Longitud del indicador de confirmación RSI. |
| `ConfirmationRsiThreshold` | RSI nivel que separa las confirmaciones alcistas y bajistas. |
| `ConfirmationCciLength` | Longitud del indicador de confirmación CCI. |
| `ConfirmationCciThreshold` | Magnitud mínima absoluta CCI para aceptar una señal. |
| `AtrPeriod` | periodo ATR retrospectivo. |
| `AtrEntryMultiplier` | Multiplicador ATR opcional que escala el volumen negociado. |
| `AtrTakeProfitMultiplier` | multiplicador ATR para el nivel de obtención de beneficios. |
| `AtrStopLossMultiplier` | multiplicador ATR para el nivel de stop loss. |
| `AtrTrailingMultiplier` | multiplicador ATR utilizado para el seguimiento dinámico. Establezca en `0` para desactivar. |
| `AtrMinimum` | Valor mínimo de ATR requerido antes de abrir operaciones. |
| `ExitRsiLength` | Longitud de la superposición de salida RSI. |
| `ExitRsiUpperLevel` | RSI nivel que obliga a salidas cortas. |
| `ExitRsiLowerLevel` | RSI nivel que obliga a salidas largas. |
| `CandleType` | Tipo de vela (marco de tiempo) utilizado para los cálculos. |

## Objetos de gráfico
La estrategia dibuja automáticamente:
- Velas de origen.
- EMA línea base.
- Marcadores de operaciones ejecutadas.

## Notas de optimización
Cada `StrategyParam` utilizado en la lógica expone rangos de optimización que reflejan la flexibilidad del probador original. Utilice las herramientas de optimización StockSharp para barrer las longitudes de las líneas de base, los umbrales de confirmación y las configuraciones de riesgo para reproducir las pruebas de cuadrícula de parámetros proporcionadas por la versión MQL.

## Consejos de uso
- Combine la estrategia con los ajustes preestablecidos del indicador NNFX ajustando los umbrales para que coincidan con sus herramientas personalizadas.
- Esté atento al filtro ATR; un `AtrMinimum` distinto de cero impide las operaciones durante sesiones de baja volatilidad.
- Al probar las operaciones de continuación, establezca `AtrTrailingMultiplier` mayor que cero para permitir que las posiciones rentables respiren mientras se aseguran las ganancias.
