# Estrategia de canal de precios de veinte pips
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia de canal de precios de veinte pips es una conversión del asesor experto original de MetaTrader *20 pips* que combina un canal de precios de estilo Donchian con filtros de media móvil a corto plazo. El algoritmo abre operaciones solo cuando la vela actual se abre opuesta a la anterior, filtra la dirección con promedios móviles calculados sobre precios típicos y gestiona las salidas a través de un objetivo fijo de veinte pips respaldado por un trailing stop dinámico basado en canales.

La versión StockSharp mantiene el espíritu del enfoque original al tiempo que adapta la gestión de pedidos al API de alto nivel. Las órdenes de mercado se utilizan para entradas y salidas, los objetivos de ganancias se monitorean internamente y los niveles de parada se emulan con las condiciones del canal de precios.

## Lógica de trading

1. **Pila de indicadores**
   - Una media móvil simple de un período del precio típico (H+L+C)/3 actúa como una línea de base rápida que refleja el precio típico de la vela anterior.
   - Un promedio móvil simple lento configurable (predeterminado 20) calculado sobre los precios de cierre desempeña el papel del filtro `MA_Low` del EA.
   - Los indicadores más altos y más bajos con el mismo período que el canal de precios (predeterminado 20) emulan los buffers de indicadores personalizados originales.

2. **Condiciones de entrada**
   - Configuración larga: el precio típico rápido anterior está por encima del promedio móvil lento anterior **y** la vela actual se abre por debajo de la apertura anterior. Después de una operación perdedora, el volumen se multiplica por el factor de recuperación (predeterminado 2). El precio de entrada se registra para realizar un seguimiento de pérdidas y ganancias.
   - Configuración corta: el precio típico rápido anterior está por debajo del promedio móvil lento anterior **y** la vela actual se abre por encima de la apertura anterior. El escalado de volumen sigue la misma lógica de recuperación que para las operaciones largas.

3. **Gestión de salida**
   - Cuando se abre la posición, se coloca un objetivo fijo de obtención de beneficios igual a `TakeProfitPips` multiplicado por el paso del precio del instrumento.
   - Un trailing stop impulsado por canal imita la llamada `OrderModify` original. Cuando la barra anterior supera el canal de precios (desplazamiento de dos barras de la lógica MT4), el stop de protección se mueve al extremo anterior menos/más el desplazamiento final en pips. Si la siguiente vela supera ese extremo, la posición sale inmediatamente al precio de apertura.
   - Las salidas de take-profit, trailing stop y gap se ejecutan a través de órdenes de mercado mientras se rastrea el precio de salida real para actualizar el indicador de ganancias/pérdidas para la escala estilo martingala.

4. **Martingale recuperación**
   - Después de cada posición perdedora cerrada, el siguiente tamaño de entrada se multiplica por `RecoveryMultiplier`. Las operaciones rentables restablecen la bandera y vuelven al volumen base.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Periodo de tiempo principal utilizado para los cálculos. | velas de 1 hora |
| `ChannelPeriod` | Período retroactivo para el canal de estilo Donchian. | 20 |
| `SlowMaPeriod` | Longitud del filtro de media móvil lenta. | 20 |
| `TakeProfitPips` | Distancia en pips para el objetivo de beneficio fijo. | 20 |
| `TrailingOffsetPips` | Desplazamiento utilizado al apretar el tope hasta el extremo anterior. | 10 |
| `RecoveryMultiplier` | Multiplicador de volumen aplicado después de una pérdida. | 2 |
| `Volume` | Volumen de operaciones base antes del escalado de recuperación. | 0.1 |

## Notas de uso

- La estrategia espera que `Security.PriceStep` refleje el valor del pip del instrumento negociado. Ajuste `TakeProfitPips` y `TrailingOffsetPips` si el símbolo usa una definición de pip diferente.
- Debido a que StockSharp utiliza órdenes de mercado para las salidas, las pruebas retrospectivas pueden mostrar un deslizamiento en comparación con las órdenes de parada y límite MT4 originales. La lógica sigue reproduciendo los mismos umbrales de precios.
- Los valores del canal se cambian para emular las llamadas `iCustom(..., shift=2)`; tenga esto en cuenta al modificar el comportamiento de seguimiento.
- El multiplicador de recuperación se puede establecer en 1 para desactivar la escala estilo martingala.
