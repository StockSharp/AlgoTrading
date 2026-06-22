# Estrategia Oscilador de Peso Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el asesor experto "Exp_Fractal_WeightOscillator" agregando cuatro osciladores (RSI, Money Flow Index, Williams %R y DeMarker) en una única señal compuesta suavizada. El oscilador se compara con dos niveles horizontales (`HighLevel`/`LowLevel`) para activar operaciones largas o cortas en modo de seguimiento de tendencia o contratendencia. Todos los cálculos se realizan en el marco temporal de velas seleccionado y usan la API estándar de alto nivel de StockSharp.

## Pila de indicadores
- **Índice de Fuerza Relativa** – aplicado a la fuente de precio configurada.
- **Money Flow Index** – calculado a partir del precio aplicado elegido y el volumen de velas.
- **Williams %R** – calculado a partir de los valores de máximo/mínimo/cierre de la vela.
- **DeMarker** – recreado a partir de los máximos y mínimos de las velas con un suavizador de media simple.
- **Suavizador de media móvil** – postprocesamiento opcional de la suma ponderada (SMA, EMA, SMMA o LWMA).

El valor del oscilador compuesto es una media ponderada de los cuatro componentes. `HighLevel` y `LowLevel` definen zonas de sobrecompra/sobreventa. `SignalBar` controla cuántas barras completadas se inspeccionan al buscar un cruce para que pueda retrasar la ejecución en relación con la vela terminada más reciente.

## Lógica de trading
### TrendMode = Direct
- **Entrada larga / salida corta** – cuando el oscilador cae desde por encima de `LowLevel` hasta por debajo o igual a `LowLevel` (`BuyOpenEnabled` y `SellCloseEnabled` deben ser verdaderos).
- **Entrada corta / salida larga** – cuando el oscilador sube desde por debajo de `HighLevel` hasta por encima o igual a `HighLevel` (`SellOpenEnabled` y `BuyCloseEnabled` deben ser verdaderos).

### TrendMode = Counter
- **Entrada larga / salida corta** – activada por una ruptura al alza de `HighLevel`.
- **Entrada corta / salida larga** – activada por una ruptura a la baja de `LowLevel`.

Las señales se evalúan en la barra especificada por `SignalBar`. Las reversiones de posición usan `Volumen + |Posición|` para neutralizar cualquier exposición existente.

## Gestión de riesgos
Cuando se abre una nueva posición, la estrategia calcula niveles de stop-loss y toma de ganancias de precio fijo usando `StopLossPoints` y `TakeProfitPoints`. Los valores se multiplican por el `MinPriceStep` del instrumento. En cada vela completada se comprueba el mínimo/máximo contra estos objetivos; si se alcanzan, la posición se cierra inmediatamente y los rastreadores de riesgo internos se restablecen.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `TrendMode` | Seleccionar comportamiento directo (seguimiento de tendencia) o contratendencia. |
| `SignalBar` | Número de barras cerradas hacia atrás usadas para la evaluación de señales. |
| `Period` | Longitud base para RSI, MFI, Williams %R y DeMarker. |
| `SmoothingLength` | Ventana para el suavizador de media móvil. |
| `SmoothingMethod` | Tipo de media móvil (`None`, `Sma`, `Ema`, `Smma`, `Lwma`). |
| `RsiPrice`, `MfiPrice` | Fuente de precio aplicada usada en los osciladores componentes. |
| `MfiVolume` | Tipo de volumen para MFI (tick y real ambos usan volumen de velas). |
| `RsiWeight`, `MfiWeight`, `WprWeight`, `DeMarkerWeight` | Pesos relativos en el oscilador compuesto. |
| `HighLevel`, `LowLevel` | Umbrales superior e inferior para cruces de nivel. |
| `BuyOpenEnabled`, `SellOpenEnabled` | Habilitar entradas largas o cortas. |
| `BuyCloseEnabled`, `SellCloseEnabled` | Permitir cerrar posiciones existentes en señales opuestas. |
| `StopLossPoints`, `TakeProfitPoints` | Distancias de protección en pasos de precio (0 deshabilita el nivel). |
| `CandleType` | Marco temporal de las velas pasadas a la estrategia. |
| `Volume` *(Propiedad de estrategia)* | Tamaño de operación usado para entradas (las reversiones de posición añaden la posición absoluta). |

## Notas de uso
- `SignalBar = 1` reproduce el comportamiento del experto original usando la última barra completamente cerrada. Aumentar el valor retrasa las reacciones por barras adicionales.
- `SmoothingMethod` permite desactivar el suavizado (`None`) o coincidir con los diferentes estilos de media móvil disponibles en la versión MQL.
- La implementación del Money Flow Index siempre trabaja con el volumen total de la vela suministrado por el feed de datos. Por tanto, tanto las opciones `Tick` como `Real` se refieren al mismo valor agregado porque las velas de StockSharp no exponen contadores de tick separados por defecto.
- Todos los comentarios en la fuente C# están escritos en inglés según se requiere.
