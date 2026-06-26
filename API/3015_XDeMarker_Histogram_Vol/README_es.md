# Estrategia de Histograma de Volumen XDeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el asesor experto original de MetaTrader **Exp_XDeMarker_Histogram_Vol** sobre la API de alto nivel de StockSharp. Transforma el oscilador DeMarker en un histograma ponderado por volumen, suaviza tanto el oscilador como el volumen con medias móviles configurables, y reacciona a los cambios de régimen cuando el histograma cruza bandas predefinidas.

La lógica es deliberadamente simétrica. Las posiciones largas se abren cuando el histograma entra en una de las zonas alcistas, mientras que los cortos se abren cuando se mueve hacia zonas bajistas. Las señales opuestas cierran la posición activa y, si está habilitado, invierten inmediatamente la dirección.

## Concepto

1. **DeMarker ponderado por volumen**
   - El DeMarker se calcula con el período seleccionado.
   - El oscilador se escala al rango `[-50; +50]` y se multiplica por el volumen de vela elegido.
   - Una media móvil suaviza el oscilador ponderado. La misma media móvil se aplica al volumen mismo. Solo se proporcionan cuatro tipos de media móvil (simple, exponencial, suavizada, ponderada) porque estas están disponibles de forma nativa en StockSharp.
2. **Niveles dinámicos**
   - Cuatro multiplicadores definidos por el usuario (`HighLevel1`, `HighLevel2`, `LowLevel1`, `LowLevel2`) definen los umbrales alcistas y bajistas.
   - Los umbrales se escalan por el volumen suavizado de modo que una mayor participación amplía el rango aceptable.
3. **Máquina de estados**
   - Cada vela terminada se clasifica en uno de cinco estados: `0` (alcista extremo), `1` (alcista), `2` (neutral), `3` (bajista), `4` (bajista extremo).
   - Las señales se generan cuando el estado de la última vela cerrada (desplazada por `SignalBar`) difiere del estado anterior de una manera que indica una transición hacia territorio alcista o bajista.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal principal. Por defecto, velas de 2 horas para reflejar el asesor experto original. |
| `DeMarkerPeriod` | Período del oscilador DeMarker. |
| `HighLevel1` / `HighLevel2` | Multiplicadores positivos que definen el primer y segundo umbral alcista. |
| `LowLevel1` / `LowLevel2` | Multiplicadores negativos que definen el primer y segundo umbral bajista. |
| `Smoothing` | Tipo de media móvil tanto para el histograma como para el volumen. Opciones: Simple, Exponential, Smoothed, Weighted. |
| `SmoothingLength` | Longitud de las medias de suavizado. |
| `SignalBar` | Número de barras cerradas usadas para comparación de señales. `1` significa "usar la vela cerrada más recientemente". |
| `VolumeType` | Fuente de volumen. Ambas opciones recurren al volumen de vela porque StockSharp no expone recuentos de ticks en todos los feeds. |
| `EnableLongEntries` / `EnableShortEntries` | Permitir abrir nuevas posiciones en la dirección respectiva. |
| `EnableLongExits` / `EnableShortExits` | Permitir cerrar posiciones existentes cuando aparece la configuración opuesta. |

## Señales y Gestión de Posiciones

- **Entrar largo**: la última barra de señal transiciona al estado `1` o `0` mientras la barra anterior estaba en un estado de número mayor (>1). Las posiciones cortas se cierran opcionalmente antes de entrar.
- **Entrar corto**: la última barra de señal transiciona al estado `3` o `4` mientras la barra anterior estaba en un estado de número menor (<3 o <4 respectivamente). Las posiciones largas se cierran opcionalmente antes de entrar.
- **Salida**: siempre que se activa una señal opuesta y las salidas están habilitadas para la dirección actual. Se usa `ClosePosition()` para aplanar antes de revertir.
- **Dimensionamiento de posición**: la estrategia se basa en la propiedad estándar `Strategy.Volume`. Los bloques de gestión monetaria de la versión MetaTrader (dos "magic" IDs separados) se simplifican intencionalmente.

## Notas de Implementación

- Solo se procesan velas terminadas. La estrategia se suscribe al marco temporal configurado mediante `SubscribeCandles().WhenNew(ProcessCandle)`.
- La implementación de DeMarker mantiene sumas rodantes de valores DeMax/DeMin para coincidir con los cálculos de MetaTrader y espera hasta que se acumulen suficientes barras antes de emitir señales.
- Si faltan datos de volumen, el histograma degrada graciosamente a cero porque tanto el oscilador ponderado como los umbrales serán cero.
- Los modos de suavizado no compatibles del indicador original (JJMA, JurX, ParMA, T3, VIDYA, AMA) no se reproducen. Elija la alternativa más cercana mediante el parámetro `Smoothing`.
- El búfer `SignalBar` solo mantiene el historial mínimo necesario (actual, anterior y una ranura adicional) para imitar el comportamiento original de `CopyBuffer` y evitar señales desactualizadas.

## Consejos de Uso

- Iniciar la estrategia en el Designer o Runner después de configurar el marco temporal y el volumen deseados.
- Optimizar `DeMarkerPeriod`, `SmoothingLength` y los multiplicadores de umbral juntos: pequeños cambios en los umbrales alteran materialmente la cadencia de entradas.
- Dado que el histograma está ponderado por volumen, la calidad del feed importa. Use proveedores de datos que reporten volumen de vela confiable para capturar el efecto deseado.
- Considere agregar módulos externos de gestión monetaria o riesgo si necesita reglas de stop-loss o take-profit; no estaban presentes en la conversión de alto nivel.
