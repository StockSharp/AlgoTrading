# Estrategia XROC2 VG con Filtro de Tiempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto de MetaTrader **Exp_XROC2_VG_Tm** usando la API de alto nivel de StockSharp. Construye dos curvas suavizadas de tasa de cambio (ROC) y abre operaciones contrarias cuando la curva más rápida cruza la más lenta. Un filtro de sesión de trading y objetivos de protección opcionales reproducen la configuración original de gestión de capital.

## Lógica de trading

- Se calculan dos valores ROC a partir del precio de cierre usando períodos de retroceso independientes.
- Cada stream ROC se suaviza con un método de media móvil configurable.
- Las señales se evalúan en un índice de barra desplazado, coincidiendo con el comportamiento original de `SignalBar`.
- Cuando la línea rápida estaba por encima de la lenta en la barra anterior pero cae por debajo en la barra de señal, la estrategia cierra cualquier posición corta y puede abrir una posición larga.
- Cuando la línea rápida estaba por debajo de la lenta en la barra anterior pero sube por encima en la barra de señal, la estrategia cierra cualquier posición larga y puede abrir una posición corta.
- Una ventana de trading opcional puede liquidar todas las posiciones fuera de la sesión permitida antes de colocar nuevas operaciones.

El lado de la orden solo cambia después de que la posición anterior esté completamente cerrada, imitando los algoritmos de trading de MetaTrader.

## Indicadores

- **ROC rápido** – Momentum, porcentaje o ratio de cambio de precio sobre `RocPeriod1` barras, suavizado con `SmoothMethod1` y longitud `SmoothLength1`.
- **ROC lento** – Mismo cálculo sobre `RocPeriod2` barras, suavizado con `SmoothMethod2` y longitud `SmoothLength2`.
- Métodos de suavizado soportados: media móvil Simple, Exponencial, Suavizada (RMA) y Ponderada. Las opciones originales JJMA/VIDYA/AMA se aproximan mediante suavizado exponencial.

## Gestión de riesgo

- `StopLoss` y `TakeProfit` especifican salidas opcionales de distancia fija en unidades de precio absolutas. Cuando se alcanza cualquier umbral, la posición se cierra inmediatamente.
- `OrderVolume` define el tamaño de todas las nuevas posiciones.
- Las salidas basadas en indicadores también pueden liquidar posiciones incluso si los objetivos de protección están deshabilitados.

## Filtro de sesión

- `UseTimeFilter` activa/desactiva la ventana horaria del día.
- `StartTime` / `EndTime` especifican los límites de la sesión. Cuando el intervalo cruza la medianoche, la ventana se trata como dos segmentos, exactamente como en la versión MQL.
- Si una posición aún está abierta cuando la ventana se cierra, se liquida al mercado antes de que la estrategia evalúe nuevas entradas.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Tipo de datos de vela usado para los cálculos (predeterminado: velas de 4 horas). |
| `RocPeriod1`, `RocPeriod2` | Longitudes de retroceso para los streams ROC rápido y lento. |
| `SmoothLength1`, `SmoothLength2` | Longitudes de suavizado para cada stream. |
| `SmoothMethod1`, `SmoothMethod2` | Tipos de media móvil aplicados a los outputs ROC. |
| `RocType` | Fórmula de cálculo ROC: momentum, cambio porcentual o ratio. |
| `SignalShift` | Número de barras completadas hacia atrás usadas para leer los valores de señal. |
| `AllowBuyOpen`, `AllowSellOpen` | Habilitar o deshabilitar apertura de posiciones largas/cortas. |
| `AllowBuyClose`, `AllowSellClose` | Habilitar o deshabilitar salidas basadas en indicadores para posiciones largas/cortas. |
| `UseTimeFilter` | Activa la ventana de sesión de trading. |
| `StartTime`, `EndTime` | Horarios de inicio y fin de sesión. |
| `OrderVolume` | Volumen para cada nueva operación. |
| `StopLoss`, `TakeProfit` | Distancias absolutas opcionales para salidas de protección. |

## Notas de implementación

- La estrategia mantiene historiales cortos de precios y valores suavizados en lugar de usar buffers de indicadores, lo que reproduce el offset `SignalBar` original sin depender de `GetValue`.
- Los suavizados JJMA, VIDYA y AMA del indicador MQL se mapean al suavizado exponencial para mantenerse dentro del conjunto de indicadores estándar de StockSharp.
- Todos los comentarios en el código están en inglés y el namespace sigue las pautas del repositorio.
