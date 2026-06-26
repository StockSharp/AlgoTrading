# Estrategia WE TRUST Channel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia WE TRUST Channel** es un port de StockSharp de alto nivel del asesor experto de MetaTrader 5 "WE TRUST". El sistema opera pullbacks hacia una media móvil ponderada lineal que está rodeada por bandas de desviación estándar. Cuando el precio cierra fuera de las bandas, la estrategia anticipa una reversión a la media y abre una posición de mercado de vuelta hacia el centro del canal. La inversión de señales, el cierre opcional de trades opuestos y los parámetros de gestión monetaria basados en pips reflejan el experto original.

## Lógica de trading
1. Suscribirse al tipo de vela configurado (velas por hora por defecto) y calcular dos indicadores en la fuente de precio seleccionada:
   - Una media móvil ponderada lineal (**LWMA**) con período y desplazamiento configurables.
   - Una envolvente de desviación estándar con su propio período y desplazamiento.
2. Convertir los offsets basados en pips en distancias de precio absolutas usando el `PriceStep` del instrumento. Las cotizaciones de cinco y tres dígitos multiplican el paso por 10 para emular la definición de pip de MetaTrader.
3. Calcular los límites superior e inferior del canal: `LWMA ± StdDev ± ChannelIndentPips` (convertidos en unidades de precio).
4. Evaluar solo velas finalizadas. Cuando el precio de la vela elegida cierra por debajo del canal inferior, la estrategia genera una señal de **compra**. Cuando cierra por encima del canal superior, genera una señal de **venta**.
5. Opcionalmente invertir las señales cuando **ReverseSignals** está habilitado. Opcionalmente aplanar una posición opuesta antes de actuar sobre una nueva señal cuando **CloseOpposite** está habilitado.
6. Enviar órdenes de mercado con el volumen configurado cuando la posición actual está plana o alineada con la dirección de la señal.

## Gestión de riesgos
- **StopLossPips** y **TakeProfitPips** traducen distancias en pips a órdenes protectoras absolutas a través de `StartProtection`. Establecerlos en `0` para deshabilitar el nivel respectivo.
- **TrailingStopPips** y **TrailingStepPips** controlan un trailing stop basado en pips que sigue los trades rentables. Ambos parámetros se convierten en distancias de precio usando la misma lógica de tamaño de pip.
- Todas las salidas se realizan con órdenes de mercado para permanecer cercanas a la implementación de MQL5.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Volumen del trade enviado con cada orden de mercado. | `0.1` |
| `StopLossPips` | Distancia de stop-loss expresada en pips (0 deshabilita el stop). | `40` |
| `TakeProfitPips` | Distancia de take-profit expresada en pips (0 deshabilita el objetivo). | `60` |
| `TrailingStopPips` | Distancia de trailing stop en pips. | `10` |
| `TrailingStepPips` | Paso de trailing en pips entre ajustes de stop. | `10` |
| `MaPeriod` | Período de la media móvil ponderada lineal. | `60` |
| `MaShift` | Número de barras que la media móvil se desplaza hacia adelante. | `0` |
| `StdDevPeriod` | Período del cálculo de desviación estándar. | `50` |
| `StdDevShift` | Número de barras que el valor de desviación se desplaza. | `0` |
| `SignalBarOffset` | Número de barras completadas hacia atrás al evaluar señales. | `1` |
| `ChannelIndentPips` | Buffer adicional añadido fuera de las bandas de desviación. | `1` |
| `ReverseSignals` | Invertir la lógica de compra/venta del rompimiento del canal. | `false` |
| `CloseOpposite` | Cerrar una posición opuesta antes de entrar en un nuevo trade. | `false` |
| `AppliedPrice` | Componente de precio de la vela alimentado en ambos indicadores. | `Weighted` |
| `CandleType` | Tipo de datos de vela solicitado al conector. | `marco temporal de 1 hora` |

## Notas
- La estrategia depende de metadatos válidos de `PriceStep`. Si el exchange no lo proporciona, el código recurre a `Security.Step` y finalmente a `1`.
- Solo la implementación en C# está incluida en este directorio. El port de Python se omite intencionalmente según las instrucciones.
- La lógica procesa solo velas finalizadas y no intenta acumular datos de barra parciales.
