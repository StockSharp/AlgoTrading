# Estrategia de Seguimiento de Tendencia (Get Trend)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port en StockSharp del asesor experto de MetaTrader **"Get trend"**, diseñado originalmente para operar en M15 con un filtro de confirmación en H1. El algoritmo combina medias móviles suavizadas y un oscilador estocástico para temporizar entradas de reversión a la media alineadas con la tendencia de un marco temporal superior.

## Lógica de trading

- **Marco temporal principal:** Las velas de 15 minutos se usan para la generación de señales y la ejecución de órdenes.
- **Marco temporal de confirmación:** Las velas horarias proporcionan la media móvil suavizada y el precio de cierre del marco temporal superior para validar la tendencia predominante.
- **Filtro de tendencia:** Tanto el cierre de M15 como el de H1 deben estar en el mismo lado de sus respectivas medias móviles suavizadas. Adicionalmente, el cierre de M15 debe mantenerse dentro de una distancia configurable de su media móvil para asegurar una entrada en retroceso.
- **Disparador de momentum:** Las operaciones largas requieren que la línea %K del estocástico cruce por encima de %D en la región de sobreventa (por debajo de 20). Las operaciones cortas requieren el cruce inverso en la región de sobrecompra (por encima de 80).
- **Gestión de órdenes:** Las posiciones están protegidas con niveles fijos de stop-loss y take-profit definidos en puntos de precio. Un trailing stop opcional ajusta la salida una vez que el precio avanza lo suficiente a favor de la operación.

## Condiciones de entrada

### Configuración larga
1. El cierre de M15 está por debajo de la media móvil suavizada de M15.
2. El cierre de H1 está por debajo de la media móvil suavizada de H1.
3. La distancia entre el cierre de M15 y la media de M15 no supera el **Price Threshold** (expresado en puntos/ticks).
4. El %K y %D del estocástico están ambos por debajo de 20.
5. El valor anterior de %K estaba por debajo de %D, y el %K actual cruzó por encima de %D.
6. No hay posición larga existente (una posición corta se cerrará y se revertirá).

### Configuración corta
1. El cierre de M15 está por encima de la media móvil suavizada de M15.
2. El cierre de H1 está por encima de la media móvil suavizada de H1.
3. La distancia entre el cierre de M15 y la media de M15 no supera el **Price Threshold**.
4. El %K y %D del estocástico están ambos por encima de 80.
5. El valor anterior de %K estaba por encima de %D, y el %K actual cruzó por debajo de %D.
6. No hay posición corta existente (una posición larga se cerrará y se revertirá).

## Reglas de salida

- **Stop-loss:** Establecido en puntos de precio absolutos desde el precio de entrada.
- **Take-profit:** Establecido en puntos de precio absolutos desde el precio de entrada.
- **Trailing stop:** Cuando está habilitado, una vez que el precio se mueve más allá de la distancia de trailing, el stop se acerca para asegurar ganancias respetando el offset de trailing configurado.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `M15CandleType` | Tipo de vela usado para la generación de señales. | Marco temporal de 15 minutos |
| `H1CandleType` | Tipo de vela usado para la confirmación. | Marco temporal de 1 hora |
| `MaM15Length` | Longitud de la MA suavizada en velas M15. | 99 |
| `MaH1Length` | Longitud de la MA suavizada en velas H1. | 184 |
| `StochasticLength` | Período %K del oscilador estocástico. | 27 |
| `StochasticSignalLength` | Período de suavizado %D. | 3 |
| `ThresholdPoints` | Distancia máxima (en puntos) entre el precio y la MA de M15 para permitir entradas. | 10 |
| `TakeProfitPoints` | Distancia de take-profit (en puntos). | 540 |
| `StopLossPoints` | Distancia de stop-loss (en puntos). | 90 |
| `TrailingStopPoints` | Distancia de trailing stop (en puntos). | 20 |
| `TradeVolume` | Volumen base de la orden al abrir nuevas operaciones. | 0.1 |

Todos los parámetros basados en puntos se multiplican por el `PriceStep` del instrumento para convertirlos a incrementos de precio absolutos.

## Notas de implementación

- La estrategia usa la API de alto nivel de StockSharp con suscripciones a velas y vinculación de indicadores (`BindEx`) para evitar la gestión manual de buffers.
- La lógica del trailing stop replica la versión de MetaTrader: se activa una vez que el beneficio no realizado supera la distancia de trailing y ajusta continuamente el stop hacia el precio.
- Las órdenes activas se cancelan antes de revertir posiciones para evitar órdenes conflictivas en el libro.
- Las áreas del gráfico muestran velas M15 con la media móvil suavizada y un panel estocástico dedicado para diagnósticos visuales.

## Consejos de uso

- Configure los tipos de velas para que coincidan con el proveedor de datos (p. ej., se pueden sustituir velas basadas en volumen si exponen el mismo concepto de DataType).
- Ajuste el umbral y los parámetros de stop cuando opere con activos de diferente volatilidad o tamaños de tick.
- Para mejores resultados, aplique la estrategia a instrumentos con tendencia donde los retrocesos hacia la media móvil son comunes.
