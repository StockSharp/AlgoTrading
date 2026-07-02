# Estrategia inversa media
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Mean Reverse replica el asesor experto "MeanReversionTrendEA". Combina un módulo de tendencia cruzada de media móvil con una superposición de reversión a la media impulsada por bandas de volatilidad del rango verdadero promedio (ATR). La idea es abrir una posición cuando el precio confirme un cambio de tendencia alcista o bajista o se aleje demasiado del promedio móvil más lento en una distancia ajustada por la volatilidad.

## Lógica de trading
- **Componente de tendencia**: aparece una configuración larga cuando la media móvil simple rápida (SMA) cruza por encima de la lenta SMA. Se activa una configuración corta cuando el SMA rápido cruza por debajo del SMA lento.
- **Componente de reversión a la media**: se activa una configuración larga cada vez que el precio de cierre cae por debajo del SMA lento en más de `ATR × Multiplier`. Aparece una configuración corta cuando el precio sube por encima del lento SMA en más de la misma distancia.
- **Combinación de señales**: si el módulo de tendencia o el módulo de reversión a la media señalan una posición larga (corta) mientras no hay ninguna posición abierta, la estrategia ingresa una posición larga (corta) con el volumen configurado.

## Gestión Comercial
- **Stop-loss**: inmediatamente después de la entrada, la estrategia coloca un nivel de precio en `entry − StopLossPoints × Step` para posiciones largas o `entry + StopLossPoints × Step` para posiciones cortas. Cuando los extremos de las velas tocan este nivel, la posición se cierra.
- **Take-profit**: un objetivo de ganancias se coloca en `entry + TakeProfitPoints × Step` para operaciones largas o en `entry − TakeProfitPoints × Step` para operaciones cortas. Un toque en el máximo o mínimo de la vela respectiva cierra la posición.
- **Restricción de posición única**: el algoritmo mantiene como máximo una posición abierta. Las nuevas señales se ignoran hasta que se cierra la operación actual.
- **Módulo de seguridad**: la llamada `StartProtection()` incorporada refleja la capa de validación de operaciones de seguridad del asesor experto original y protege contra estados de posición inesperados.

## Indicadores
- **Promedio móvil simple (SMA)** con período `FastMaPeriod`.
- **Promedio móvil simple (SMA)** con período `SlowMaPeriod`.
- **Rango verdadero promedio (ATR)** con período `AtrPeriod`.

Todos los indicadores se actualizan desde la misma suscripción de vela definida por `CandleType`.

## Parámetros
| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `FastMaPeriod` | Vista retrospectiva del SMA rápido utilizado tanto en la detección de tendencias como en las bandas de reversión a la media. | 20 |
| `SlowMaPeriod` | Mirando hacia atrás del SMA lento que representa la media de equilibrio. | 50 |
| `AtrPeriod` | Número de velas para el cálculo de volatilidad de ATR. | 14 |
| `AtrMultiplier` | Multiplicador aplicado a ATR para controles de distancia. | 2.0 |
| `StopLossPoints` | Distancia de stop-loss medida en `Security.Step` unidades. | 500 |
| `TakeProfitPoints` | Distancia de obtención de beneficios medida en `Security.Step` unidades. | 1000 |
| `TradeVolume` | Volumen enviado con cada orden de mercado. | 1 |
| `CandleType` | Tipo de datos de vela que alimenta los indicadores. | plazo de 1 hora |

## Notas
- El tamaño de vela predeterminado es de una hora para reflejar la lógica del "plazo actual" de la versión MetaTrader. Ajústelo para que coincida con el período del gráfico original.
- Los sobres basados en ATR utilizan el cierre de la vela como precio de referencia, reflejando el punto medio original entre la oferta y la demanda.
- Utilice los indicadores de optimización adjuntos a los parámetros para calibrar el sistema para diferentes mercados.
