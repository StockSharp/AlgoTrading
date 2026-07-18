# Estrategia NextBar Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rompimientos de momentum que ocurren cuando la barra completada más reciente cierra muy lejos de una barra de referencia más antigua. Fue inspirada por el asesor experto de MetaTrader "Nextbar" y mantiene las características originales de gestión del dinero, como stops basados en pips, lógica de trailing y tiempo de vida limitado de la posición.

La configuración predeterminada apunta a gráficos de FX o futuros de índices de movimiento rápido en el marco temporal de 15 minutos, pero la lógica funciona en cualquier símbolo que proporcione velas regulares. Cada orden se envía a mercado usando el tamaño de posición configurado.

## Lógica de trading

- **Detección de señal**
  - Cuando termina una nueva barra, el algoritmo compara el cierre de la barra anterior con el cierre que ocurrió hace `SignalBar` barras.
  - Si el cierre anterior es más alto que el cierre distante en más de `MinDistancePips`, se genera una configuración larga.
  - Si el cierre anterior es más bajo que el cierre distante en más de `MinDistancePips`, aparece una configuración corta.
  - El interruptor `ReverseSignals` invierte la dirección de cada configuración para adaptarse a flujos de trabajo contrarios.
- **Manejo de órdenes**
  - Las órdenes se ignoran mientras hay una posición abierta. La estrategia solo mantiene una posición a la vez, al igual que el asesor experto original.
  - Cada llenado almacena el precio de entrada y precalcula los niveles de stop-loss y take-profit en unidades de precio. Los valores basados en pips se convierten usando el paso de precio de la seguridad (los instrumentos de 5 dígitos usan automáticamente un multiplicador de 10× para coincidir con el tamaño de pip de MetaTrader).

## Reglas de salida

- **Stop loss / take profit** – Ambos niveles son opcionales. Un valor de cero deshabilita la protección correspondiente. La estrategia monitorea los máximos y mínimos de las velas para activar salidas cuando los niveles son cruzados.
- **Trailing stop** – Cuando está habilitado (`TrailingStopPips` > 0), el stop se mueve más cerca del precio actual una vez que el beneficio supera `TrailingStopPips + TrailingStepPips`. La distancia del precio al stop nunca se reduce, asegurando un comportamiento de trailing monótono.
- **Tiempo de vida de la posición** – Después de permanecer en el mercado durante `LifetimeBars` velas completadas, la posición se cierra en la siguiente apertura de barra independientemente del beneficio. Esto reproduce el mecanismo original de "expirar después de N barras".

## Parámetros

- `CandleType` – Marco temporal usado para la evaluación de señales. Por defecto velas de 15 minutos.
- `OrderVolume` – Cantidad enviada con cada orden de mercado.
- `StopLossPips` – Distancia desde el precio de entrada al stop protector, expresada en pips.
- `TakeProfitPips` – Distancia desde el precio de entrada al objetivo de beneficio, expresada en pips.
- `TrailingStopPips` – Distancia mantenida por el trailing stop. Establecer en cero para deshabilitar la lógica de trailing.
- `TrailingStepPips` – Beneficio adicional requerido antes de que el trailing stop avance de nuevo. Se ignora cuando el trailing está deshabilitado.
- `SignalBar` – Número de barras entre los cierres de comparación. Debe ser al menos dos para evitar referenciar la barra actual.
- `MinDistancePips` – Distancia mínima en pips entre los cierres comparados antes de que se acepte una señal.
- `LifetimeBars` – Número máximo de velas completadas que una posición puede permanecer abierta. Establecer en cero para deshabilitar el temporizador.
- `ReverseSignals` – Invierte las señales largas/cortas cuando está habilitado.

## Notas de implementación

- La estrategia se basa en una breve lista rodante de cierres anteriores en lugar de estructuras históricas pesadas, lo que mantiene el cálculo de señales liviano.
- Los pips se convierten en unidades de precio usando el paso de precio de la seguridad. Los instrumentos cotizados con 3 o 5 decimales se mapean automáticamente a la definición tradicional de pip.
- Todos los controles de riesgo se aplican en velas completadas. Si necesita protección intra-barra, combine la estrategia con órdenes stop nativas del exchange a través de la configuración de la plataforma.
- No se proporcionan pruebas automatizadas con esta muestra. Valídela en datos históricos antes de usarla en producción.
