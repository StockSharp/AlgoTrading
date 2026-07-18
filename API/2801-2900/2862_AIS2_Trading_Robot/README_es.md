# Robot de Trading AIS2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El Robot de Trading AIS2 es un sistema de rompimiento multi-timeframe convertido del asesor experto original de MetaTrader 5. Escanea un timeframe superior (por defecto velas de 15 minutos) para detectar rompimientos direccionales, mientras un timeframe más rápido (por defecto velas de 1 minuto) proporciona trailing stops adaptativos. La colocación de órdenes, el presupuesto de riesgo y la lógica de trailing siguen las reglas codificadas en la versión MQ5 heredada, pero se implementan sobre la API de estrategia de alto nivel de StockSharp.

## Lógica de trading
- **Vela de señal primaria**: Para cada vela terminada en el timeframe primario la estrategia captura el máximo, mínimo, cierre, punto medio y rango.
- **Configuración larga**:
  - El cierre anterior debe estar por encima del punto medio de la vela, señalando presión alcista.
  - El precio ask actual debe operar por encima del máximo anterior más el spread medido (confirmación de rompimiento).
  - El precio de entrada es el ask actual. El stop-loss es igual a `high + spread - (range × StopFactor)`. El take-profit es igual a `ask + (range × TakeFactor)`.
  - Las verificaciones adicionales de seguridad del broker aseguran que tanto el riesgo como la recompensa sean mayores que la distancia de buffer de stop configurada.
- **Configuración corta**:
  - El cierre anterior debe estar por debajo del punto medio, señalando presión bajista.
  - El bid actual debe imprimir por debajo del mínimo anterior (rompimiento bajista).
  - El precio de entrada es el bid actual. El stop-loss es igual a `low + (range × StopFactor)`. El take-profit es igual a `bid - (range × TakeFactor)`.
- **Resolución de conflictos**: Las nuevas operaciones se toman solo cuando la estrategia está plana o posicionada en la dirección opuesta (el volumen de entrada compensa automáticamente la exposición existente antes de abrir la nueva posición).

## Gestión de órdenes
- **Trailing stop**: El rango del timeframe secundario se multiplica por `TrailFactor` para construir un trail dinámico. Para posiciones largas el stop se lleva a `bid - trailDistance`; para cortas se empuja a `ask + trailDistance`. Las actualizaciones de trailing se omiten cuando el precio no está en ganancia o cuando la modificación solicitada es menor que el step de trail configurado y los buffers de congelamiento.
- **Toma de beneficios y salida por stop**: Tanto las posiciones largas como cortas se liquidan con órdenes de mercado cuando los precios bid/ask cruzan los niveles de stop-loss o take-profit almacenados.
- **Feed de libro de órdenes**: Una suscripción al libro de órdenes en vivo rastrea los precios bid/ask actuales para que la estrategia pueda reproducir la lógica MQ5 que dependía de los valores `SymbolInfo.Ask/Bid`.

## Dimensionamiento de posición y controles de riesgo
- **Reserva de cuenta**: Una fracción configurable del capital del portafolio está bloqueada y no puede usarse para trading. Esto replica el parámetro `Inp_aed_AccountReserve` del EA original.
- **Reserva de orden**: El capital restante se limita aún más por una fracción de asignación de orden que limita el presupuesto máximo de riesgo por operación.
- **Verificaciones de riesgo**:
  - Si el capital reservado es menor que el límite de asignación (`Equity × OrderReserve`), la estrategia se niega a colocar nuevas operaciones.
  - El tamaño de posición se calcula como `riskBudget / |entry - stop|`, alineado al paso de volumen de seguridad. Cuando no hay información del portafolio disponible se usa el parámetro de respaldo `BaseVolume`.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `AccountReserve` | Fracción del capital retenida del trading (0–0.95).
| `OrderReserve` | Fracción del capital negociable que define el presupuesto de riesgo por operación (0–1).
| `PrimaryCandleType` | Marco temporal de trabajo para la detección de rompimientos (por defecto 15 minutos).
| `SecondaryCandleType` | Marco temporal más rápido que impulsa las actualizaciones del trailing stop (por defecto 1 minuto).
| `TakeFactor` | Multiplicador aplicado al rango primario para calcular la distancia de take-profit.
| `StopFactor` | Multiplicador aplicado al rango primario para calcular la distancia de stop-loss.
| `TrailFactor` | Multiplicador aplicado al rango secundario para calcular la distancia de trailing.
| `BaseVolume` | Tamaño de orden de respaldo usado cuando las métricas del portafolio no están disponibles.
| `StopBufferTicks` | Distancia adicional (en ticks) requerida más allá de las restricciones de stop de la bolsa.
| `FreezeBufferTicks` | Buffer adicional que previene ajustes menores de trailing cerca del nivel de congelamiento.
| `TrailStepMultiplier` | Multiplicador de spread que define el incremento mínimo entre actualizaciones de trailing.

## Notas
- Siempre alimente la estrategia con ambas series de velas primaria y secundaria más un stream de libro de órdenes en vivo para desbloquear todas las ramas de lógica.
- Las verificaciones de rompimiento dependen de precios bid/ask, por lo que el paper trading con precios de última operación únicamente puede entregar un comportamiento diferente en comparación con un entorno real.
- La protección de posición se inicia automáticamente una vez que la estrategia corre, reflejando las rutinas de seguridad presentes en la versión MQ5.
