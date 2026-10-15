# Estrategia Exp XBullsBearsEyes Vol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión en C# del expert de MetaTrader **Exp_XBullsBearsEyes_Vol**. El asesor original combina las lecturas de Bulls Power y Bears Power, multiplica el resultado por el volumen de la vela y colorea el histograma de acuerdo al impulso resultante. Se mantienen dos slots de posición independientes tanto para el lado largo como para el corto, permitiendo al sistema escalar cuando la intensidad del color aumenta. El port de StockSharp recrea el filtro multi-etapa, la lógica de colores y la gestión de operaciones mientras usa llamadas de API de alto nivel para órdenes y control de riesgo.

El algoritmo se suscribe a un marco temporal configurable, reconstruye el indicador XBullsBearsEyes personalizado y reacciona solo a velas terminadas. Las transiciones de color determinan tanto las entradas como las salidas: los colores alcistas cierran operaciones cortas y pueden abrir uno o dos slots largos; los colores bajistas realizan la acción espejo. Las distancias de stop-loss y take-profit se traducen en parámetros de `StartProtection` para que los gestores de riesgo de la plataforma puedan manejar órdenes protectoras.

## Lógica del indicador
1. Los valores de Bulls Power y Bears Power se reconstruyen con una EMA de período `IndicatorPeriod` usando el máximo/mínimo de la vela contra el cierre suavizado.
2. Un filtro adaptativo de cuatro etapas acumula presión alcista (`CU`) y bajista (`CD`) con coeficiente `Gamma`. El valor del indicador es `CU / (CU + CD) * 100 - 50`.
3. El valor filtrado se multiplica por el volumen de tick o volumen real, dependiendo de `VolumeType`.
4. Las series multiplicadas y el volumen bruto se suavizan por una media móvil elegida a través de `SmoothingMethod`, `SmoothingLength` y `SmoothingPhase` (la fase Jurik se respeta cuando la clase subyacente la expone).
5. Los niveles de color se derivan de `HighLevel1`, `HighLevel2`, `LowLevel1` y `LowLevel2`. Los valores por encima de las bandas superiores producen colores `0` o `1`, mientras que los valores por debajo de las bandas inferiores producen colores `3` o `4`. El color `2` indica un estado neutral.
6. El historial de colores se almacena para que las señales puedan evaluarse en la barra `SignalBar` (predeterminado: una vela cerrada atrás). El color de la barra de señal actual se compara con el color anterior para detectar transiciones.

## Reglas de trading
- Los colores `1` y `0` denotan presión alcista. Cuando el color cambia a uno de esos valores y el color anterior era más débil, el slot 1 (`PrimaryVolume`) o slot 2 (`SecondaryVolume`) abre una posición larga respectivamente. Ambos eventos cierran cualquier exposición corta existente si `AllowShortExit` está habilitado.
- Los colores `3` y `4` denotan presión bajista. Cuando el color se mueve a estos valores y el color anterior era más fuerte, el slot 1 o slot 2 abre una posición corta respectivamente. Ambos eventos cierran cualquier exposición larga existente si `AllowLongExit` está habilitado.
- Cada slot recuerda si ya tiene una posición abierta e ignora señales repetidas hasta que la dirección correspondiente haya sido cerrada.
- `SignalBar` define cuántas velas completadas se omiten antes de evaluar el color (0 = última vela terminada). El código requiere al menos dos colores históricos para comparar.
- El stop-loss y take-profit expresados en puntos (`StopLossPoints`, `TakeProfitPoints`) se convierten a distancias de precio absoluto con `Security.PriceStep` y se usan para iniciar la protección de la plataforma con salidas de mercado.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `PrimaryVolume` | Volumen para el primer slot (activado por color 1 / 3). |
| `SecondaryVolume` | Volumen para el segundo slot (activado por color 0 / 4). |
| `StopLossPoints` / `TakeProfitPoints` | Distancias protectoras en pasos de precio. Establecer en cero para deshabilitar. |
| `AllowLongEntry` / `AllowShortEntry` | Habilitar escalar hacia la dirección correspondiente. |
| `AllowLongExit` / `AllowShortExit` | Habilitar salidas automatizadas cuando aparece el color opuesto. |
| `CandleType` | Marco temporal suscrito para velas y cálculo del indicador (predeterminado: 8 horas). |
| `IndicatorPeriod` | Período EMA usado para reconstruir Bulls/Bears Power. |
| `Gamma` | Factor de suavizado adaptativo para el filtro de cuatro etapas (0.0 – 0.999). |
| `VolumeType` | Seleccionar volumen de tick o volumen real para ponderación. |
| `HighLevel1`, `HighLevel2`, `LowLevel1`, `LowLevel2` | Multiplicadores de nivel que definen umbrales de color. |
| `SmoothingMethod` | Tipo de media móvil usado para suavizar el indicador y el volumen (SMA, EMA, SMMA, LWMA, Jurik, JurX, ParMA→EMA, T3, VIDYA→EMA, AMA). |
| `SmoothingLength` | Longitud de la media móvil de suavizado. |
| `SmoothingPhase` | Parámetro de fase Jurik (limitado a [-100, 100]). |
| `SignalBar` | Número de velas cerradas para retroceder antes de evaluar las transiciones de color. |

## Notas de uso
- La estrategia opera con un único instrumento retornado por `GetWorkingSecurities()` y usa órdenes de mercado para entradas y salidas.
- La gestión de slots es neta: entradas adicionales se añaden a la posición neta, mientras que las salidas aplanan toda la exposición para el lado afectado.
- Si la plataforma proporciona solo volumen de tick, seleccionar `VolumeType = Real` recurrirá al conteo de tick disponible.
- Los suavizados VIDYA y Parabólico recurren a medias móviles exponenciales porque StockSharp expone esas implementaciones directamente.
- Asegurarse de configurar el paso de precio del instrumento para que `StopLossPoints` y `TakeProfitPoints` se conviertan en las distancias absolutas previstas.
