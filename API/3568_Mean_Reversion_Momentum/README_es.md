# Estrategia de impulso de reversión a la media
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Mean Reversion es una adaptación directa del asesor experto MetaTrader *Mean reversion.mq4*. La versión StockSharp mantiene la idea comercial original: comprar después de una serie prolongada de cierres a la baja y vender después de una carrera alcista similar. Las entradas se confirman mediante la alineación de tendencias utilizando dos promedios móviles ponderados lineales, la fuerza del impulso en un período de tiempo más alto y un filtro MACD mensual.

Una vez en posición, la estrategia recrea las reglas de administración de dinero de la versión MQL: stop-loss y take-profit configurables en pips, reubicación opcional del punto de equilibrio y un trailing stop que bloquea las ganancias a medida que el mercado se mueve a favor de la operación.

## Lógica de trading
1. **Período de tiempo de la señal**: la estrategia opera en la serie de velas seleccionada (predeterminado 15 minutos).
2. **Detección de agotamiento**: recopila los últimos `BarsToCount` cierres. Una configuración larga requiere que el cierre más reciente sea más bajo que cada uno de los cierres anteriores, lo que indica una venta masiva. Una configuración corta necesita la condición opuesta.
3. **Filtro de tendencias**: el LWMA rápido (longitud `FastMaLength`) debe estar por encima del LWMA lento (`SlowMaLength`) para largos y por debajo para cortos.
4. **Filtro de impulso**: el indicador de impulso (período `MomentumLength`) se calcula en el período de tiempo superior de estilo MetaTrader (M15 → H1, H1 → D1, etc.). Al menos una de las últimas tres lecturas de impulso debe desviarse de 100 en más de `MomentumThreshold`.
5. **MACD confirmación**: un MACD mensual (26/12/9) debe tener la línea principal por encima de la línea de señal para largos y por debajo para cortos.

Si se cumplen todas las condiciones, la estrategia abre una posición usando `OrderVolume`. Las operaciones opuestas aplanan la posición actual antes de revertirse.

## Gestión de Puestos
- **Stop-loss & take-profit**: configurado en pips mediante `StopLossPips` y `TakeProfitPips`.
- **Equipo**: cuando está habilitado, el tope se mueve al precio de entrada más `BreakEvenOffsetPips` después de que el precio avanza en `BreakEvenTriggerPips`.
- **Trailing stop**: si `EnableTrailing` es verdadero y el beneficio no realizado supera `TrailingStopPips`, el stop sigue al precio con el paso `TrailingStepPips`.

Todas las conversiones de precios utilizan el tamaño del pip del instrumento para coincidir con el comportamiento de MetaTrader.

## Parámetros
| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `OrderVolume` | Tamaño de orden utilizado para las entradas al mercado. | `1` |
| `CandleType` | Serie de velas primarias utilizadas para señales. | `M15` |
| `BarsToCount` | Número de cierres anteriores comprobados por agotamiento. | `10` |
| `FastMaLength` | Período LWMA rápido. | `6` |
| `SlowMaLength` | Período LWMA lento. | `85` |
| `MomentumLength` | Período de impulso en el marco temporal superior. | `14` |
| `MomentumThreshold` | Desviación absoluta mínima de 100 para confirmación del impulso. | `0.3` |
| `StopLossPips` | Distancia de stop-loss en pips. | `20` |
| `TakeProfitPips` | Distancia de toma de ganancias en pips. | `50` |
| `UseBreakEven` | Habilite la reubicación de parada para alcanzar el punto de equilibrio. | `false` |
| `BreakEvenTriggerPips` | Beneficio en pips necesarios antes de mover el stop. | `30` |
| `BreakEvenOffsetPips` | Se agregan puntos adicionales al pasar al punto de equilibrio. | `30` |
| `EnableTrailing` | Activar la gestión de trailing stop. | `true` |
| `TrailingStopPips` | Beneficio en pips necesarios para comenzar a seguir. | `40` |
| `TrailingStepPips` | Distancia mantenida por el trailing stop. | `40` |

## Notas
- El marco de tiempo superior para el impulso sigue MetaTrader pasos: M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1, W1→MN1.
- La confirmación de MACD siempre utiliza el período de tiempo mensual (MN1).
- La estrategia espera tipos de velas basadas en plazos; No se admiten velas de tick o de rango.
