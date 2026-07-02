# Estrategia Sidus v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Sidus v1 es una estrategia de seguimiento de tendencias que combina dos conjuntos de medias móviles exponenciales (EMA) con filtros de índice de fuerza relativa (RSI). El asesor experto original MetaTrader 4 abre una posición cuando un EMA rápido diverge de un EMA más lento y el RSI confirma condiciones de sobreventa o sobrecompra. Este puerto StockSharp mantiene la lógica central, limitando las operaciones a velas con bajo volumen y adjuntando órdenes de protección asimétricas para posiciones largas y cortas.

## Indicadores utilizados
- **Fast EMA (tramo de compra)**: mide el impulso a corto plazo para entradas largas.
- **EMA lenta (tramo de compra)**: representa el filtro de tendencia a largo plazo para entradas largas.
- **Fast EMA (tramo de venta)**: mide el impulso a corto plazo para entradas cortas.
- **EMA lenta (tramo de venta)**: representa el filtro de tendencia a largo plazo para entradas cortas.
- **RSI (tramo de compra)**: valida las condiciones de sobreventa para operaciones largas.
- **RSI (tramo de venta)**: valida las condiciones de sobrecompra para operaciones cortas.

## Lógica de trading
1. Suscríbase a la serie de velas configurada (período de tiempo predeterminado de 15 minutos).
2. Calcule todos los indicadores EMA y RSI en cada vela terminada.
3. Omitir la evaluación de la señal cuando el volumen de la vela exceda el límite configurado (predeterminado 10).
4. **Condición de compra**:
   - El EMA rápido menos el EMA lento está por debajo del umbral de compra.
   - El valor de RSI está por debajo del umbral de compra de RSI.
   - No existe exposición larga (la posición neta debe ser no positiva).
5. **Condición de venta**:
   - El EMA rápido (tramo de venta) menos el EMA lento (tramo de venta) está por encima del umbral de venta.
   - RSI (tramo de venta) está por encima del umbral de venta RSI.
   - No existe exposición corta (la posición neta debe ser no negativa).
6. Cuando se active una señal, cancele cualquier orden de protección pendiente, ejecute una orden de mercado del tamaño necesario para invertir la posición neta hacia el lado deseado y coloque inmediatamente órdenes de toma de ganancias y de limitación de pérdidas adaptadas a la dirección de la posición.

## Gestión del riesgo
- Las operaciones largas colocan una toma de ganancias en `entry + BuyTakeProfitPips * priceStep` y un límite de pérdidas en `entry - BuyStopLossPips * priceStep`.
- Las operaciones cortas colocan una toma de ganancias en `entry - SellTakeProfitPips * priceStep` y un límite de pérdidas en `entry + SellStopLossPips * priceStep`.
- Las órdenes de protección reutilizan el escalón actual del precio de los valores; cambie los parámetros de pip para adaptarse a instrumentos con diferentes tamaños de tick.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `FastEmaLength` | Duración del EMA rápido para señales de compra. | 23 |
| `SlowEmaLength` | Duración del EMA lento para señales de compra. | 62 |
| `FastEma2Length` | Duración del EMA rápida para señales de venta. | 18 |
| `SlowEma2Length` | Duración del EMA lento para señales de venta. | 54 |
| `RsiPeriod` | RSI período para la confirmación de compra. | 67 |
| `RsiPeriod2` | RSI período para la confirmación de venta. | 97 |
| `BuyDifferenceThreshold` | Diferencia máxima rápido-EMA lenta para permitir compras. | 63 |
| `BuyRsiThreshold` | Nivel máximo de RSI para permitir compras. | 59 |
| `SellDifferenceThreshold` | Diferencia mínima entre rápido y EMA lenta para permitir ventas. | -57 |
| `SellRsiThreshold` | Nivel mínimo de RSI para permitir ventas. | 60 |
| `BuyTakeProfitPips` | Distancia de obtención de beneficios (pips) para operaciones largas. | 95 |
| `BuyStopLossPips` | Distancia de stop-loss (pips) para operaciones largas. | 100 |
| `SellTakeProfitPips` | Distancia de obtención de beneficios (pips) para operaciones cortas. | 17 |
| `SellStopLossPips` | Distancia de stop-loss (pips) para operaciones cortas. | 69 |
| `OrderVolume` | Volumen de posiciones recién abiertas. | 0,5 |
| `MaxCandleVolume` | Volumen máximo de velas permitido para negociar. | 10 |
| `CandleType` | Marco de tiempo utilizado para los cálculos. | velas de 15 minutos |

## Notas de uso
- Asegúrese de que la seguridad conectada admita órdenes simultáneas de mercado, stop y límite para una gestión de riesgos adecuada.
- Ajuste la configuración del pip para reflejar el tamaño del tick del instrumento si difiere del valor del punto MT4 asumido por el experto original.
- La estrategia opera sobre posiciones netas; aplanará la exposición opuesta antes de establecer una nueva operación en la dirección opuesta.
