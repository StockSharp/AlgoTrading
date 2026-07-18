# Fractals Distancia Mínima
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Fractals Distancia Mínima replica el asesor experto de MetaTrader "Fractals minimum distance" utilizando la API de estrategia de alto nivel de StockSharp. El sistema escanea la serie de velas configurada en busca de patrones fractales de cinco barras estilo Bill Williams. Cada vez que aparece un nuevo fractal confirmado en el desplazamiento de barra de señal especificado, la estrategia mide la brecha entre los fractales de subida y bajada más recientes. Solo se permite una orden de mercado cuando esta distancia supera el umbral requerido expresado en pips.

La conversión mantiene el comportamiento original de cerrar cualquier exposición opuesta inmediatamente antes de revertir. A diferencia de la versión MQL, el tamaño de la posición se toma de la propiedad `Volume` de la estrategia en lugar de realizar cálculos de riesgo basados en la cuenta. No se envían órdenes de stop-loss ni take-profit, coincidiendo con el experto fuente.

## Lógica de señales
1. Suscribirse al tipo de vela definido por `CandleType` y construir buffers deslizantes de máximos y mínimos que siempre contengan la barra ubicada `SignalBar` velas en el pasado junto con dos vecinas en cada lado.
2. Detectar un **fractal superior** cuando el máximo de la barra central es estrictamente mayor que los máximos de las dos velas precedentes y las dos siguientes. Detectar un **fractal inferior** de forma análoga para los mínimos.
3. Convertir el parámetro `DistancePips` a una distancia de precio usando el `PriceStep` del símbolo. Los símbolos con tres o cinco dígitos decimales se ajustan automáticamente para tratar las cotizaciones 0.001/0.00001 como un pip.
4. Cuando se confirma un fractal superior:
   - Almacenar el nuevo nivel superior y cerrar las posiciones largas existentes.
   - Si tanto el último fractal superior como el inferior son conocidos y su diferencia absoluta es al menos el umbral de distancia, enviar una orden de venta de mercado usando `Volume`.
5. Cuando se confirma un fractal inferior:
   - Almacenar el nuevo nivel inferior y cerrar las posiciones cortas existentes.
   - Si se cumple la condición de distancia, enviar una orden de compra de mercado usando `Volume`.

Las operaciones se realizan solo después de que se cierra la vela que finaliza el fractal, asegurando que las barras inacabadas nunca desencadenen entradas. La estrategia se basa en `IsFormedAndOnlineAndAllowTrading()` para evitar colocar órdenes antes de que el entorno esté listo.

## Parámetros
| Nombre | Descripción | Notas |
| --- | --- | --- |
| `DistancePips` | Espaciado mínimo entre los últimos fractales de subida y bajada medido en pips. | Convertido internamente a unidades de precio usando el tamaño del tick del instrumento. |
| `SignalBar` | Número de barras completamente cerradas que deben pasar después de la barra que aloja el fractal. | El valor efectivo mínimo es 2, coincidiendo con la confirmación de dos barras usada por los fractales de Bill Williams. |
| `CandleType` | Serie de datos que alimenta los cálculos. | El valor predeterminado es el marco temporal de un minuto; cambiar para trabajar con otras resoluciones. |
| `Volume` | Propiedad estándar de la estrategia StockSharp que define el tamaño de la operación. | Reemplaza el dimensionamiento basado en riesgo original del experto MetaTrader. |

## Gestión de posiciones y diferencias con MQL
- Las posiciones siempre se aplanan antes de revertir la dirección, exactamente como lo hacía el helper `ClosePositions` fuente.
- El experto original llamaba a `RefreshRates()` y realizaba configuraciones explícitas de deslizamiento. Esos aspectos se delegan a la infraestructura de StockSharp en este port.
- Las órdenes de stop-loss y take-profit no eran parte de la lógica MQL y permanecen ausentes aquí.
- `DistancePips` usa precisión entera como la entrada `ushort`, mientras que `SignalBar` refleja la entrada `uchar` de MQL.
- Dado que StockSharp trabaja con posiciones netas, abrir una orden en dirección opuesta automáticamente voltea la exposición, coincidiendo con el comportamiento de netting de MetaTrader.

## Consejos de uso
- Comience con el mismo desplazamiento de barra de señal (`SignalBar = 3`) del código original y calibre el umbral de distancia según la volatilidad del instrumento.
- Aumente `SignalBar` para esperar más velas después de que aparezca un fractal, lo que puede filtrar oscilaciones rápidas.
- Combine con gestión de riesgo externa como el helper integrado `StartProtection()` si se requiere un stop de protección.
