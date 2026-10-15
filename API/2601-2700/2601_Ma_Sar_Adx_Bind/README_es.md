# Estrategia Ma SAR ADX Bind
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión a la API de alto nivel de StockSharp del asesor experto original **MaSarADX.mq5** de MetaTrader 5. El sistema combina un filtro de tendencia de media móvil simple con señales del Índice de Movimiento Direccional (ADX) y el trailing stop Parabolic SAR. Las decisiones de trading se evalúan solo en velas completadas, replicando el comportamiento del "primer tick de una nueva barra" de la versión MQL. Cuando el cierre de la vela está alineado tanto con la tendencia de la media móvil como con el equilibrio direccional del ADX, se abre una posición. El Parabolic SAR guía tanto la dirección del trade como las salidas forzando una liquidación total cuando el precio cruza al lado opuesto de los puntos SAR.

## Indicadores y datos
- **Media Móvil Simple (SMA)** – proporciona el filtro de dirección de tendencia primaria. Longitud predeterminada: 100 velas.
- **Índice de Dirección Promedio (ADX)** – suministra +DI y −DI para confirmar la fuerza direccional. Longitud predeterminada: 14.
- **Parabolic SAR** – actúa como una superposición de stop y reversión y define las condiciones de salida. Aceleración predeterminada: 0.02; aceleración máxima: 0.10.
- **Velas** – cualquier marco temporal puede solicitarse. Por defecto la estrategia se suscribe a velas de 1 hora, pero el parámetro puede ajustarse para adaptarse al símbolo y régimen de prueba.

La implementación se suscribe a flujos de velas de StockSharp y vincula los tres indicadores usando el helper `BindEx` de modo que cada callback recibe valores sincronizados para la misma vela.

## Lógica de trading
### Entrada larga
1. El cierre de la vela está por encima de la media móvil.
2. +DI es mayor o igual a −DI, indicando presión direccional alcista.
3. El cierre de la vela está por encima del valor del Parabolic SAR.
4. No hay ninguna posición larga actualmente abierta (`Position <= 0`).

Cuando todas las reglas se alinean, se envía una orden de compra de mercado por el volumen base configurado más cualquier tamaño requerido para cubrir una posición corta.

### Entrada corta
1. El cierre de la vela está por debajo de la media móvil.
2. +DI es menor o igual a −DI, indicando presión direccional bajista.
3. El cierre de la vela está por debajo del valor del Parabolic SAR.
4. No hay ninguna posición corta actualmente abierta (`Position >= 0`).

Se coloca una orden de venta de mercado cuando todas las reglas cortas coinciden.

### Salidas
- Las **posiciones largas** se cierran inmediatamente una vez que el precio cae por debajo del Parabolic SAR.
- Las **posiciones cortas** se cubren cuando el precio sube por encima del Parabolic SAR.

No se agregan niveles separados de stop-loss o take-profit; el cruce del SAR es la única señal de salida, siguiendo al asesor experto original. Dado que las salidas se evalúan antes de nuevas entradas, la estrategia no cambiará de corto a largo (o viceversa) en la misma vela, reflejando el ciclo de apertura/cierre en dos pasos del script MQL.

## Parámetros
| Nombre | Descripción | Valor predeterminado | Notas |
| --- | --- | --- | --- |
| `MaPeriod` | Longitud de la media móvil simple usada para definir el filtro de tendencia. | 100 | Optimizable, debe ser mayor que cero. |
| `AdxPeriod` | Período del cálculo de ADX que produce +DI y −DI. | 14 | Optimizable, debe ser mayor que cero. |
| `SarStep` | Factor de aceleración e incremento para el Parabolic SAR. | 0.02 | Equivalente al parámetro `step` de MQL. |
| `SarMax` | Factor máximo de aceleración para el Parabolic SAR. | 0.10 | Refleja la configuración `maximum` de MQL. |
| `Volume` | Tamaño de orden base para nuevas entradas. | 1 | Reemplaza el dimensionamiento de lotes basado en margen de la versión MetaTrader. El tamaño real de la orden es `Volume + |Position|` para que las reversiones aplasten la exposición existente. |
| `CandleType` | El tipo de datos de velas suscrito a través de StockSharp. | 1 hora | Ajustable a cualquier marco temporal. |

## Notas de implementación
- El procesamiento de indicadores usa el pipeline de alto nivel `BindEx` de StockSharp, asegurando que SMA, ADX y SAR se actualicen en perfecta sincronía sin buffering manual.
- Las salidas se ejecutan incluso si `AllowTrading` está temporalmente deshabilitado, manteniendo los controles de riesgo activos en todo momento.
- Se incluyen helpers de graficación: el panel principal muestra precio, SMA y SAR, mientras que un panel secundario muestra el indicador ADX para diagnósticos.
- Las declaraciones de log describen cada decisión de trading con los valores subyacentes del indicador para simplificar las pruebas hacia adelante y la depuración.

## Directrices de uso
1. Adjunte la estrategia a un valor y portfolio en el Designer o Backtester.
2. Ajuste el tipo de vela para que coincida con su horizonte de trading (p.ej., velas M15, H1, o D1).
3. Ajuste el período de la media móvil, el período de ADX y los parámetros SAR para adaptarse a la volatilidad del instrumento.
4. Establezca el parámetro `Volume` al tamaño de posición preferido. Si necesita la gestión de dinero adaptativa usada en el script original, integre su propio dimensionamiento basado en cartera antes de enviar órdenes.
5. Ejecute la estrategia. Los trades solo se activarán después de que todos los indicadores hayan producido suficientes valores históricos para estar formados.

## Diferencias con el asesor experto original
- El cálculo de lotes basado en margen ha sido reemplazado con un parámetro fijo `Volume` para mantener la estrategia broker-neutral dentro de StockSharp.
- La gestión de trades, los valores del indicador y el orden de evaluación (salida antes de entrada) siguen estrictamente la lógica de referencia de MetaTrader.
- Todos los comentarios dentro del código fuente están en inglés para cumplir con las pautas del proyecto.
