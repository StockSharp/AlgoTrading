# Estrategia de Stoch Sell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el comportamiento del experto MetaTrader original **stochSell**. Escucha un único flujo de velas y espera una confirmación estocástica triple combinada con un filtro de volatilidad antes de enviar una orden de venta de mercado inicial. Inmediatamente después de la entrada corta, despliega una escalera de sell stops pendientes para escalar en el movimiento si el precio continúa bajando.

## Lógica de trading
- **Filtro de volatilidad** – un Rango Verdadero Promedio (ATR) con longitud configurable debe mantenerse por debajo del umbral especificado.
- **Confirmación estocástica lenta** – el oscilador estocástico de mayor período debe mantenerse por debajo del nivel de sobrevendido a largo plazo antes de que se permita cualquier operación.
- **Confirmación de cruce** – tanto el oscilador estocástico medio como el rápido deben cruzar hacia abajo a través del disparador de sobrevendido durante la misma vela finalizada.
- **Verificación de posición** – las nuevas entradas se colocan solo cuando la estrategia no tiene órdenes activas y la posición es plana.

Una vez que se cumplen todas las condiciones, la estrategia envía una orden de venta de mercado usando el volumen configurado e inmediatamente programa un conjunto de órdenes de venta stop según la configuración de la cuadrícula. Las órdenes pendientes son opcionales y pueden desactivarse estableciendo el conteo de órdenes de cuadrícula en cero.

## Reglas de salida
- **Objetivo de beneficio** – cuando la cesta corta acumula el beneficio deseado en pips (calculado desde el precio de entrada ponderado por volumen), la estrategia recompra toda la posición y elimina cada orden pendiente restante.
- **Stop manual** – las órdenes de cuadrícula respetan un tiempo de vida configurable. Cuando una orden de stop expira sin ejecutarse, se cancela automáticamente.
- **Cierre completo** – cualquier operación de compra que devuelva la posición a cero borra las estadísticas de entrada internas y cancela la cuadrícula pendiente.

## Gestión de la cuadrícula
- Las órdenes pendientes se colocan por debajo del precio de referencia usando el offset inicial y el paso expresado en pips.
- Cada orden pendiente usa el multiplicador de volumen de la cuadrícula, permitiendo que el tamaño de la cesta difiera de la entrada de mercado inicial.
- La expiración (en minutos) se aplica a cada orden pendiente; cero desactiva el tiempo de espera.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal principal para cada indicador y decisión de trading. |
| `AtrPeriod` / `AtrThreshold` | Filtro de volatilidad que controla cuándo la estrategia puede operar. |
| `FastKPeriod`, `FastDPeriod`, `FastSlowing` | Configuración del oscilador estocástico rápido. |
| `MediumKPeriod`, `MediumDPeriod`, `MediumSlowing` | Configuración del oscilador estocástico medio. |
| `SlowKPeriod`, `SlowDPeriod`, `SlowSlowing` | Configuración del oscilador estocástico lento. |
| `OversoldLevel` | Nivel que los valores estocásticos rápido y medio deben cruzar hacia abajo. |
| `LongTermOversoldLevel` | Límite superior para el estocástico lento durante la entrada. |
| `ProfitTargetPips` | Beneficio neto en pips requerido para cerrar la cesta corta. |
| `GridOrdersCount` | Número de sell stops pendientes creados después de la entrada. |
| `GridStartOffsetPips` | Offset en pips entre el precio de entrada y la primera orden pendiente. |
| `GridStepPips` | Distancia en pips entre órdenes pendientes consecutivas. |
| `GridVolume` | Volumen aplicado a cada orden pendiente. |
| `GridExpirationMinutes` | Tiempo de vida de las órdenes pendientes en minutos. |
| `MarketVolume` | Volumen usado para la venta de mercado inicial. |

## Notas
- Los valores de los indicadores se procesan a través de la API `BindEx` de alto nivel y solo las velas finalizadas desencadenan decisiones de trading.
- La lógica de seguimiento de posición mantiene un precio de entrada ponderado por volumen para traducir el objetivo de beneficio bruto en pips.
- Para deshabilitar el escalado, simplemente establezca el conteo de órdenes de cuadrícula en cero; la estrategia seguirá dependiendo de la confirmación estocástica y el filtro ATR para operaciones de disparo único.
