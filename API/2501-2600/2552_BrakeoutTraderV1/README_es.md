# BrakeoutTraderV1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

BrakeoutTraderV1 es un sistema de rompimiento simple construido alrededor de un nivel de precio estático. La estrategia observa los precios de cierre de velas completadas y entra cuando el mercado cierra a través del nivel de rompimiento elegido. Cuando el cierre cruza por encima del nivel, se abre una posición larga (sujeto a filtros de dirección); cuando cruza por debajo, se toma una posición corta. El tamaño de la posición se calcula a partir del porcentaje de riesgo configurado y la distancia al stop-loss, habilitando escalado automático con la equidad de la cuenta.

## Lógica de trading
- Procesar solo velas finalizadas del `CandleType` seleccionado. Las velas incompletas se ignoran.
- Mantener el último precio de cierre para detectar rompimientos del `BreakoutLevel` especificado por el usuario.
- **Entrada larga**: la última vela cierra por encima de `BreakoutLevel` mientras que el cierre anterior estaba en o por debajo del nivel, y `EnableLong` es verdadero. Cualquier posición corta abierta se aplana antes de enviar la nueva orden.
- **Entrada corta**: la última vela cierra por debajo de `BreakoutLevel` mientras que el cierre anterior estaba en o por encima del nivel, y `EnableShort` es verdadero. Primero se cierra cualquier posición larga.
- Las órdenes se envían a mercado. La cantidad se calcula para que la pérdida entre el precio de entrada y la distancia al stop-loss corresponda al `RiskPercent` de la equidad de la cuenta actual. Si el tamaño basado en riesgo no puede determinarse, la estrategia recurre al valor base `Volume`.
- Después de cada entrada, la estrategia almacena niveles estáticos de toma de ganancias y stop-loss expresados en pips (`StopLossPoints` y `TakeProfitPoints`). Cuando el precio alcanza cualquiera de los niveles, la posición abierta se cierra a mercado y los niveles en caché se limpian.
- Nunca hay múltiples operaciones abiertas en la misma dirección simultáneamente porque la posición neta se gestiona explícitamente.

## Gestión de posición
- Un stop protector se establece por debajo de la entrada para operaciones largas y por encima de la entrada para cortas. La distancia es `StopLossPoints * pip`, donde pip se deriva de `Security.PriceStep` y su precisión (3 o 5 decimales implican un ajuste de diez veces, como en la implementación MQL original).
- Un objetivo de ganancia se establece simétricamente usando `TakeProfitPoints`.
- Si el stop y el objetivo se activarían durante la misma vela, se evalúa primero el stop, reflejando la ejecución conservadora del servidor.
- Las señales opuestas siempre cierran cualquier posición activa antes de establecer la nueva, evitando exposición hedgeada.
- El helper reinicia automáticamente los niveles en caché cuando la posición vuelve a cero.

## Parámetros
- `BreakoutLevel` – Nivel de precio estático monitoreado para rompimientos.
- `EnableLong` / `EnableShort` – Filtros de dirección que permiten abrir posiciones largas o cortas.
- `StopLossPoints` – Distancia del stop-loss en pips (múltiplos del tamaño de pip derivado).
- `TakeProfitPoints` – Distancia del take-profit en pips.
- `RiskPercent` – Porcentaje de la equidad de la cuenta a arriesgar por operación. Usado para determinar el volumen de la orden desde la distancia del stop-loss.
- `CandleType` – Serie de datos de vela usada para generación de señales (por defecto velas de 15 minutos).
- `Volume` – Tamaño base de la orden usado cuando el cálculo basado en riesgo no está disponible.

## Detalles
- **Criterios de entrada**: El cierre cruza por encima/debajo de `BreakoutLevel` en la última vela completada.
- **Largo/Corto**: Opera ambas direcciones, controlado por los indicadores `EnableLong` y `EnableShort`.
- **Criterios de salida**: Niveles estáticos de stop-loss y take-profit, más aplanamiento en señales de rompimiento opuestas.
- **Stops**: Stop-loss de distancia fija medida en pips.
- **Valores predeterminados**: `BreakoutLevel = 0`, `StopLossPoints = 140`, `TakeProfitPoints = 180`, `RiskPercent = 10`, `CandleType = 15 minutos`, `EnableLong = EnableShort = true`.
- **Filtros**: Ninguno más allá de los selectores de dirección; no se aplican filtros de tendencia o volatilidad.

## Notas de uso
- El instrumento debe soportar el cálculo de pip utilizado por el EA original. Para símbolos con 3 o 5 decimales, el pip se escala automáticamente por diez.
- Asegurarse de que el portafolio conectado proporcione `CurrentValue` para que el dimensionamiento basado en riesgo funcione correctamente. Si la equidad no está disponible, las operaciones se ejecutarán con el `Volume` configurado.
- Dado que las órdenes se ejecutan a mercado, los llenados reales pueden diferir del cierre de la vela. Ajustar las distancias de stop y take para tener en cuenta el deslizamiento si es necesario.
