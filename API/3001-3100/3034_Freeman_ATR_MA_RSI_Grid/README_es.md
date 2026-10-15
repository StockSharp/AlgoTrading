# Estrategia Freeman ATR MA RSI Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el asesor experto MetaTrader "Freeman" usando la API de alto nivel de StockSharp. Acumula múltiples posiciones de mercado mientras una tendencia medida por la pendiente de una media móvil se mantiene alineada con una confirmación RSI. Cada distancia de entrada y salida se define en pips y se convierte en unidades de precio usando el tamaño de tick del instrumento, de modo que el comportamiento coincida con la implementación forex original.

## Lógica de trading
1. Suscribirse a una única serie de velas (marco temporal configurable) y actualizar los indicadores ATR, media móvil y RSI en cada vela finalizada.
2. Generar una señal direccional cuando:
   - La pendiente de la media móvil es positiva o negativa comparando el valor más reciente con la barra anterior (filtro de tendencia opcional).
   - El precio está suficientemente lejos de la media móvil para evitar entradas directamente en la línea.
   - El RSI cruza el umbral superior o inferior si el filtro RSI está habilitado. La lógica de MetaTrader se mantiene intacta, incluyendo la particularidad donde una confirmación de venta RSI devuelve `-11`, por lo que activar ambos filtros favorece solo las operaciones largas.
3. Respetar el número máximo de posiciones abiertas simultáneamente. Las entradas adicionales en la misma dirección solo se permiten cuando el precio se ha movido contra el último llenado por la distancia de pip configurada, construyendo efectivamente una cuadrícula.
4. Cada entrada usa niveles de stop-loss y take-profit basados en ATR. Los trailing stops ajustan el stop protector una vez que el precio se mueve por el paso de trailing más la distancia del trailing stop.
5. Las salidas se ejecutan mediante órdenes de mercado opuestas cuando el rango de la vela alcanza el nivel de stop, objetivo o trailing.

## Gestión de riesgo
- Los multiplicadores ATR controlan las distancias fijas de stop-loss y take-profit. Establecer un multiplicador en cero deshabilita esa protección.
- Los trailing stops son opcionales y se definen por dos parámetros de pip: la distancia de trailing real y el paso adicional requerido antes de mover el stop nuevamente.
- La estrategia depende de la propiedad base `Volume` para el dimensionamiento; no se aplica gestión monetaria automatizada más allá del límite de posición.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal utilizado para cálculos de indicadores. |
| `MaxPositions` | Número máximo de posiciones abiertas simultáneamente (suma de largas y cortas). |
| `DistancePips` | Distancia mínima en pips entre entradas consecutivas en la misma dirección. |
| `AtrPeriod` | Período de promediado para el indicador ATR. |
| `AtrStopLossMultiplier` | Multiplicador ATR para el stop protector. `0` deshabilita el stop. |
| `AtrTakeProfitMultiplier` | Multiplicador ATR para el objetivo de beneficio. `0` deshabilita el objetivo. |
| `UseTrendFilter` | Habilita el filtro de pendiente de la media móvil. |
| `DistanceFromMaPips` | Distancia mínima en pips entre precio y la media móvil cuando el filtro de tendencia está activo. |
| `MaPeriod`, `MaShift`, `MaMethod`, `MaPriceType` | Parámetros de media móvil que reflejan las entradas de MetaTrader. |
| `UseRsiFilter` | Habilita el filtro de confirmación RSI. |
| `RsiLevelUp`, `RsiLevelDown`, `RsiPeriod`, `RsiPriceType` | Configuración RSI con selección de precio aplicado. |
| `TrailingStopPips`, `TrailingStepPips` | Distancia y paso del trailing stop medidos en pips. |
| `CurrentBarOffset` | Desplazamiento aplicado al leer valores del indicador, emulando la entrada `CurrentBar` del asesor experto. |

## Notas
- La conversión de pips multiplica el `PriceStep` del instrumento por 10 cuando el instrumento tiene 3 o 5 decimales para reproducir el ajuste punto-a-pip de MetaTrader.
- La estrategia usa un modelo de posición de compensación; las señales opuestas cierran las posiciones existentes antes de abrir operaciones en la nueva dirección.
- La protección de inicio se habilita al lanzar para proteger contra reconexiones inesperadas antes de que se coloquen operaciones.
