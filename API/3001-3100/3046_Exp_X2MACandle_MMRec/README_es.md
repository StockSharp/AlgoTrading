# Estrategia Exp X2MA Candle MM Recovery
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Esta estrategia es una conversión a C# del experto de MetaTrader **Exp_X2MACandle_MMRec**. Observa el color de una vela doblemente suavizada, producida por el indicador personalizado X2MA original, para decidir cuándo abrir o cerrar posiciones. La versión de StockSharp recrea el pipeline de doble suavizado y mantiene una capa ligera de gestión de dinero que reduce el volumen de trading tras un número configurable de pérdidas recientes.

El algoritmo procesa solo velas completadas. Se suscribe a un marco temporal configurable, aplica dos medias móviles encadenadas a los valores OHLC de la vela, deriva un color sintético de vela (verde, gris o rojo) y usa transiciones de color con un desplazamiento de barra seleccionable por el usuario para activar acciones. Las operaciones largas se abren cuando el color cambia de alcista a cualquier otra cosa. Las operaciones cortas siguen la condición simétrica. Las salidas de posición están alineadas con los mismos controles de color y pueden habilitarse o deshabilitarse por separado para cada lado.

## Lógica del indicador
1. Cada vela se suaviza dos veces. Ambas etapas pueden usar diferentes métodos y longitudes.
2. Las opciones de suavizado se mapean a indicadores de StockSharp:
   - `Simple` → `SimpleMovingAverage`
   - `Exponential` → `ExponentialMovingAverage`
   - `Smoothed` → `SmoothedMovingAverage` (RMA)
   - `Weighted` → `WeightedMovingAverage`
   - `Jurik` → `JurikMovingAverage` (el parámetro Phase se respeta cuando está disponible).
3. El cuerpo de la vela sintética se aplana cuando la diferencia absoluta apertura/cierre es menor que `GapPoints * Security.StepPrice`.
4. Los colores se asignan de la siguiente manera: apertura < cierre → `2` (alcista), apertura > cierre → `0` (bajista), en caso contrario → `1` (neutral).
5. Las señales se evalúan en la barra `SignalBar + 1` (dos barras atrás con la configuración predeterminada) para que las órdenes se envíen solo después de que una vela completa confirme el cambio de color.

## Gestión de dinero
- El experto original reducía dinámicamente el tamaño de la posición tras una serie de pérdidas usando estadísticas históricas de transacciones. StockSharp no expone el historial exacto de MetaTrader, por lo que el port mantiene una cola interna de transacciones cerradas recientes.
- La longitud de la cola está controlada por `HistoryDepth` y el volumen cae a `ReducedVolume` una vez que se detectan `LossTrigger` o más pérdidas dentro de la ventana.
- La estrategia registra los resultados de las transacciones usando los precios de cierre de las velas cuando se activa una salida manual. Las órdenes stop-loss/take-profit de la versión MetaTrader no se recrean. Puedes agregar tus propias reglas de protección a través de los gestores de riesgo de StockSharp si es necesario.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `CandleType` | Marco temporal de las velas usadas para el suavizado y el trading. |
| `FirstMethod`, `FirstLength`, `FirstPhase` | Método de suavizado primario, longitud y fase Jurik. |
| `SecondMethod`, `SecondLength`, `SecondPhase` | Método de suavizado secundario, longitud y fase Jurik. |
| `GapPoints` | Umbral de aplanamiento del cuerpo en pasos de precio. |
| `SignalBar` | Desplazamiento (0 = última vela terminada) usado al leer los buffers de color. |
| `AllowLongEntry` / `AllowShortEntry` | Habilitar apertura de posiciones largas o cortas. |
| `AllowLongExit` / `AllowShortExit` | Habilitar cierre de posiciones largas o cortas. |
| `NormalVolume` | Tamaño de orden estándar (lotes, acciones, contratos). |
| `ReducedVolume` | Tamaño de orden usado tras el número configurado de pérdidas. |
| `HistoryDepth` | Número de transacciones recientes inspeccionadas para pérdidas (0 desactiva el seguimiento del historial). |
| `LossTrigger` | Recuento de pérdidas que activa el volumen reducido (0 desactiva el interruptor). |

## Notas de uso
- La estrategia opera en un único instrumento devuelto por `GetWorkingSecurities()`.
- Las señales y salidas se procesan una vez por vela terminada para evitar órdenes duplicadas.
- Establecer `ReducedVolume` igual a `NormalVolume` si deseas desactivar la reducción de volumen mientras mantienes las estadísticas del historial.
- Dado que el port se basa en los precios de cierre de las velas para clasificar las transacciones, el contador de pérdidas puede diferir ligeramente de MetaTrader cuando ocurren deslizamientos o ejecuciones parciales. La documentación debería ayudarte a ajustar los parámetros para lograr un comportamiento similar.
- Los stops y take-profits de la versión MQL no se recrean automáticamente. Usar los gestores de riesgo de StockSharp (`StartProtection`) si necesitas protección a nivel de plataforma.
