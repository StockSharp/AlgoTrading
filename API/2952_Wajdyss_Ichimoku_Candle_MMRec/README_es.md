# Estrategia Wajdyss Ichimoku Candle MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un porte directo del experto MetaTrader *Exp_wajdyss_Ichimoku_Candle_MMRec*. Recrea el indicador "wajdyss Ichimoku candle"
calculando la línea base de Ichimoku (Kijun) y clasificando cada vela terminada en uno de cuatro estados de color. El sistema busca entonces
una reversión en esos colores para desvanecerse del movimiento más reciente. Cuando la barra anterior estaba por encima del Kijun y la última
barra de señal cae por debajo, el algoritmo cierra cualquier exposición corta y abre una operación larga. La transición opuesta cambia a una
posición corta. Un módulo de gestión de dinero adaptativo replica la lógica MMRec original reduciendo el tamaño de la posición después de un
número configurable de operaciones perdedoras en la misma dirección.

La versión convertida usa la API de alto nivel de StockSharp. Las velas se suministran mediante una única llamada `SubscribeCandles`, y el
nivel Kijun se calcula con los indicadores `Highest`/`Lowest`. Las decisiones de trading solo se evalúan en velas terminadas para mantener el
comportamiento determinista en modos en tiempo real e histórico.

## Lógica de coloración de velas
Cada vela cerrada recibe un índice de color numérico que coincide con el indicador MQL5 original:

| Color | Condición | Significado |
|-------|-----------|-------------|
| `0` | Cierre por debajo del Kijun y cuerpo bajista | Fuerte sentimiento bajista por debajo de la línea base |
| `1` | Cierre por debajo del Kijun pero cuerpo alcista | Débil reacción alcista por debajo de la línea base |
| `2` | Cierre por encima del Kijun pero cuerpo bajista | Débil reacción bajista por encima de la línea base |
| `3` | Cierre por encima del Kijun y cuerpo alcista | Fuerte continuación alcista por encima de la línea base |

## Lógica de señales
Las señales se generan en velas terminadas comparando el color de dos barras históricas:

- **Configuración largo**: la barra en `SignalBarShift + 1` tenía un color mayor que `1` (precio por encima del Kijun) y la barra en `SignalBarShift`
  tiene un color por debajo de `2` (precio se movió por debajo del Kijun). La estrategia opcionalmente cierra cualquier posición corta abierta y puede abrir un nuevo largo.
- **Configuración corto**: la barra en `SignalBarShift + 1` tenía un color por debajo de `2` (precio por debajo del Kijun) mientras la barra en `SignalBarShift`
  imprime un color por encima de `1` (precio se movió por encima del Kijun). La estrategia opcionalmente cierra los largos existentes y puede entrar en una posición corta.

El parámetro `SignalBarShift` corresponde a la entrada `SignalBar` de la versión MetaTrader. El valor por defecto `1` significa que la señal usa
la última vela completamente cerrada y la anterior. Aumentar el desplazamiento retrasa las entradas por el número de barras solicitado.

## Gestión de dinero
El módulo MMRec mantiene un historial corto de resultados de operaciones por dirección. Si las últimas `LossTriggerCount` operaciones en una
dirección fueron todas perdedoras, la estrategia cambia al tamaño de orden reducido (`ReducedVolume`). Después de una operación rentable, o cuando
hay menos del número solicitado de operaciones disponibles, se restaura el volumen por defecto (`NormalVolume`). Esto refleja el comportamiento
de `BuyTradeMMRecounter` y `SellTradeMMRecounter` de la biblioteca MQL original.

## Gestión de riesgos
Los niveles protectores de stop-loss y take-profit se expresan en pasos de precio. Cuando una posición larga está abierta, la estrategia verifica
si el mínimo de la vela alcanzó `entrada - StopLossPoints * PriceStep` o si el máximo tocó `entrada + TakeProfitPoints * PriceStep`. El lado corto
espeja la lógica. Los stops se evalúan una vez por vela terminada, similar al EA fuente que dependía de órdenes del lado del servidor con una
distancia fija.

## Parámetros
| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|---------------------|
| `CandleType` | Tipo de datos de velas (marco temporal) usado para el indicador | Velas de 1 hora |
| `KijunLength` | Lookback de la línea base de Ichimoku | 26 |
| `SignalBarShift` | Número de barras cerradas a omitir antes de evaluar la transición de color | 1 |
| `BuyPosOpen` / `SellPosOpen` | Habilitar o deshabilitar la apertura de posiciones en cada dirección | `true` |
| `BuyPosClose` / `SellPosClose` | Permitir el cierre de posiciones existentes en la señal opuesta | `true` |
| `NormalVolume` | Volumen de orden por defecto | `1` |
| `ReducedVolume` | Volumen de orden después del número configurado de pérdidas | `0.1` |
| `LossTriggerCount` | Número de operaciones perdedoras seguidas antes de reducir el tamaño | `2` |
| `StopLossPoints` | Distancia del stop en pasos de precio (establecer en `0` para deshabilitar) | `1000` |
| `TakeProfitPoints` | Distancia del take-profit en pasos de precio (establecer en `0` para deshabilitar) | `2000` |

## Notas de uso
- La estrategia abre operaciones solo cuando la transición de color indica agotamiento y la dirección relevante está habilitada.
- El escalado de volumen requiere que la plataforma reporte resultados de operaciones; en backtests las salidas generadas por la estrategia actualizarán el historial de pérdidas automáticamente.
- Si no se define ningún paso de precio para el instrumento, las entradas de stop-loss y take-profit se ignoran.
- Establecer `SignalBarShift` en `0` imita una reacción inmediata a la última vela terminada pero aumenta el riesgo de whipsaws.
