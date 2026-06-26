# Exp RJTX Coincidencias Suavizadas Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia recrea el comportamiento del asesor experto de MetaTrader 5 `Exp_RJTX_Matches_Smoothed_Duplex.mq5`. Dos bloques de señal RJTX independientes analizan precios de apertura y cierre suavizados en sus respectivos marcos temporales. Cada bloque clasifica cada vela completada como alcista o bajista dependiendo de si el cierre suavizado sube por encima de la apertura suavizada de `Period` barras atrás. Las "coincidencias" alcistas activan entradas para el módulo largo, mientras que las coincidencias bajistas gestionan el módulo corto.

## Generación de señales
1. **Suavizado** – ambos bloques alimentan las aperturas y cierres de las velas en el algoritmo de suavizado seleccionado. Se aplica el mismo método a los flujos de apertura y cierre, pero se usan instancias separadas para mantener los búferes internos independientes.
2. **Comparación** – una vez que hay suficiente historia disponible, el cierre suavizado actual se compara con la apertura suavizada registrada `Period` barras antes.
3. **Detección de coincidencia** – si el cierre es mayor, la vela recibe una coincidencia alcista; de lo contrario se vuelve bajista. Las señales se evalúan después de desplazarse por `SignalBar` velas cerradas, igual que el acceso al búfer MT5.

## Gestión de posiciones
- El **bloque largo** abre una posición larga (cubriendo cualquier corto existente si está permitido) cuando una coincidencia alcista alcanza la ventana de evaluación. Una coincidencia bajista cierra la posición larga si las salidas largas están habilitadas.
- El **bloque corto** espeja esta lógica: una coincidencia bajista abre una operación corta (cerrando la exposición larga si está permitido) y una coincidencia alcista cubre el corto.
- Las estrategias de StockSharp son neteadas. Por lo tanto, los módulos opuestos cierran la posición actual antes de abrir una nueva, en lugar de mantener dos posiciones hedgeadas independientes como la versión MT5. Desactiva el parámetro `Allow ... Close` correspondiente para prohibir la cobertura automática.

## Gestión del riesgo
Los stops y objetivos de ganancia se expresan en pasos de precio (`PriceStep × points`). Para cada vela terminada, la estrategia verifica si el rango de la barra toca el nivel de stop-loss o take-profit activo y cierra la posición correspondiente inmediatamente. Esto emula el comportamiento de las órdenes de protección MT5 sin depender de órdenes gestionadas por el broker.

## Parámetros
| Sección | Parámetro | Predeterminado | Descripción |
| --- | --- | --- | --- |
| Long | `LongCandleType` | H4 | Marco temporal usado por el bloque RJTX largo. |
| Long | `LongVolume` | 0.1 | Volumen abierto cuando se ejecuta una señal larga. |
| Long | `LongAllowOpen` | `true` | Habilitar apertura de posiciones largas. |
| Long | `LongAllowClose` | `true` | Habilitar cierre de posiciones largas en coincidencias bajistas. |
| Long | `LongStopLossPoints` | 1000 | Distancia de stop-loss para operaciones largas en pasos de precio (0 deshabilita la comprobación). |
| Long | `LongTakeProfitPoints` | 2000 | Distancia de take-profit para operaciones largas en pasos de precio (0 deshabilita la comprobación). |
| Long | `LongSignalBar` | 1 | Desplazamiento aplicado al leer búferes RJTX (`0` = vela cerrada actual). |
| Long | `LongPeriod` | 10 | Número de barras entre el cierre suavizado actual y la apertura suavizada histórica. |
| Long | `LongMethod` | `Sma` | Algoritmo de suavizado para el bloque largo (`Sma`, `Ema`, `Smma`, `Lwma`, `Jjma`, `Jurx`, `Parma`, `T3`, `Vidya`, `Ama`). |
| Long | `LongLength` | 12 | Longitud del filtro de suavizado aplicado a las series de apertura/cierre. |
| Long | `LongPhase` | 15 | Parámetro de fase para filtros de estilo Jurik (mantenido por compatibilidad). |
| Short | `ShortCandleType` | H4 | Marco temporal usado por el bloque RJTX corto. |
| Short | `ShortVolume` | 0.1 | Volumen abierto cuando se ejecuta una señal corta. |
| Short | `ShortAllowOpen` | `true` | Habilitar apertura de posiciones cortas. |
| Short | `ShortAllowClose` | `true` | Habilitar cierre de posiciones cortas en coincidencias alcistas. |
| Short | `ShortStopLossPoints` | 1000 | Distancia de stop-loss para operaciones cortas en pasos de precio (0 deshabilita la comprobación). |
| Short | `ShortTakeProfitPoints` | 2000 | Distancia de take-profit para operaciones cortas en pasos de precio (0 deshabilita la comprobación). |
| Short | `ShortSignalBar` | 1 | Desplazamiento aplicado al leer búferes RJTX para el bloque corto. |
| Short | `ShortPeriod` | 10 | Número de barras entre el cierre suavizado actual y la apertura suavizada histórica. |
| Short | `ShortMethod` | `Sma` | Algoritmo de suavizado para el bloque corto. |
| Short | `ShortLength` | 12 | Longitud del filtro de suavizado aplicado a las señales cortas. |
| Short | `ShortPhase` | 15 | Parámetro de fase para filtros de estilo Jurik en el bloque corto. |

## Notas
- `Jjma` se mapea a Jurik Moving Average. `Jurx`, `Parma` y `Vidya` se aproximan con Zero-Lag EMA, Arnaud Legoux MA y EMA respectivamente, porque StockSharp no expone filtros idénticos de la biblioteca SmoothAlgorithms.
- La lógica de stop-loss / take-profit se evalúa en los extremos de la vela. Los picos intrabarra más cortos que el máximo/mínimo de la vela no activarán salidas.
- Las señales se procesan en velas completadas únicamente; las coincidencias intrabarra se ignoran conforme al comportamiento `IsNewBar` de MT5.
