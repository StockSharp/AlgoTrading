# Estrategia ColorJjrsxTimePlus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida del experto MetaTrader5 `Exp_ColorJJRSX_Tm_Plus`. La estrategia opera reversiones de tendencia detectadas con un oscilador RSI suavizado por Jurik e incluye salidas opcionales basadas en tiempo, imitando los toggles de gestión monetaria originales.

## Descripción general

- **Idea**: Rastrear la pendiente del oscilador Color JJRSX (aproximado mediante RSI suavizado por una Media Móvil Jurik). Cuando el oscilador sube, el sistema puede cerrar cortos y opcionalmente abrir largos, y viceversa para las caídas.
- **Mercado**: Instrumento único definido por el `Security` conectado.
- **Marco temporal**: Configurable; por defecto velas de 4 horas (coincidiendo con la entrada EA original).
- **Dirección**: Largo y corto. Cada dirección puede deshabilitarse independientemente.
- **Tipo de orden**: Órdenes de mercado mediante `BuyMarket()` / `SellMarket()`.

## Pila de indicadores

1. **Relative Strength Index (RSI)** — oscilador de momentum base usando el parámetro `RSI Length` (refleja `JurXPeriod`).
2. **Jurik Moving Average (JMA)** — suaviza la salida del RSI con `Smoothing Length` (refleja `JMAPeriod`). El parámetro de fase JMA de la versión MQL no está expuesto por StockSharp y por lo tanto se omite.
3. **Signal Shift** — reproduce el parámetro `SignalBar`. Las señales se generan a partir del valor `Signal Shift` barras atrás y los dos valores anteriores para detectar cambios de pendiente.

## Lógica de trading

### Gestión de largos
- **Entrada**: Habilitada por `Enable Long Entries`. Requiere que el oscilador suavizado estuviera declinando dos barras atrás (`previous > older` es falso), giró hacia arriba en la última barra completada (`previous < older`), y continúa más alto en la barra actual (`current > previous`). La posición debe estar plana o corta.
- **Salida**: Si `Exit Long on Downturn` está habilitado y el oscilador se inclina hacia abajo (`previous > older`), cualquier largo abierto se cierra.

### Gestión de cortos
- **Entrada**: Habilitada por `Enable Short Entries`. Requiere que el oscilador gire hacia abajo (`previous > older`) y continúe cayendo en la barra actual (`current < previous`) mientras la estrategia está plana o larga.
- **Salida**: Si `Exit Short on Upturn` está habilitado y el oscilador se inclina hacia arriba (`previous < older`), cualquier corto abierto se cubre.

### Filtro de tiempo
- `Enable Time Exit` cierra posiciones una vez que su tiempo de tenencia supera `Holding Minutes`. Esto refleja el temporizador del experto original que liquida posiciones después de `nTime` minutos.

### Controles de riesgo
- `Stop Loss (pts)` y `Take Profit (pts)` se convierten en niveles de protección de StockSharp mediante `StartProtection` usando `UnitTypes.PriceStep`.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `Indicator Timeframe` | Tipo de vela para los cálculos del indicador. | Velas de 4 horas |
| `RSI Length` | Período para el RSI (análogo al período JurX). | 8 |
| `Smoothing Length` | Longitud del suavizado Jurik MA (análogo al período JMA). | 3 |
| `Signal Shift` | Número de barras completadas a saltar antes de verificar pendientes (`SignalBar`). | 1 |
| `Enable Long Entries` / `Enable Short Entries` | Permitir abrir operaciones en cada dirección. | true |
| `Exit Long on Downturn` / `Exit Short on Upturn` | Permitir salidas impulsadas por el oscilador para posiciones existentes. | true |
| `Enable Time Exit` | Activar la liquidación basada en tiempo de tenencia. | true |
| `Holding Minutes` | Minutos máximos para mantener una posición abierta. | 240 |
| `Stop Loss (pts)` | Distancia del stop de protección en pasos de precio. | 1000 |
| `Take Profit (pts)` | Distancia del objetivo de ganancia en pasos de precio. | 2000 |

## Notas sobre la conversión

- El buffer del histograma JJRSX del indicador original se emula con RSI + suavizado Jurik. Solo se usa información de pendiente, por lo que las diferencias de escala numérica no afectan las decisiones.
- Las opciones de gestión monetaria (`MM`, `MMMode`, `Deviation`) no están portadas. El dimensionamiento de órdenes en StockSharp debe manejarse a través de la propiedad `Strategy.Volume` o configuraciones de cartera externas.
- Las variables globales usadas en MQL para limitar la tasa de órdenes son innecesarias aquí porque la estrategia solo reacciona a velas finalizadas.
- Todos los comentarios y documentación están en inglés según las directrices del repositorio.
