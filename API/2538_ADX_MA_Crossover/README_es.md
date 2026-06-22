# Cruce ADX MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el Asesor Experto "ADX & MA" combinando una media móvil suavizada con un filtro de tendencia de Índice Direccional Promedio (ADX). La lógica analiza las últimas dos velas completadas en el marco temporal seleccionado y reacciona solo después de que tanto la media móvil como el ADX hayan producido valores confirmados. Está diseñada para entradas de estilo de cobertura pero implementada en un modelo de posición neta, revirtiendo automáticamente la posición cuando aparecen señales opuestas.

La media móvil se calcula sobre el precio mediano de cada vela, coincidiendo con la versión de MetaTrader que usó una SMMA construida sobre `(High + Low) / 2`. El umbral ADX evita las operaciones cuando la fuerza de la tendencia es débil, reduciendo las señales falsas de cruces de corta duración.

## Lógica de entrada
- Esperar hasta que tanto la media móvil suavizada como el ADX hayan producido valores finales.
- Evaluar el cierre de la vela anterior (`n-1`) relativo al valor de la MA suavizada tomado en la misma vela.
- Ir largo cuando:
  - El cierre de la vela `n-1` está por encima del valor MA de `n-1`.
  - El cierre de la vela `n-2` estaba por debajo de ese valor MA (cruce alcista), y
  - El valor ADX de la vela `n-1` es mayor o igual a `AdxThreshold`.
- Ir corto cuando ocurren las condiciones inversas (cruce bajista con confirmación de ADX).
- El tamaño de la posición usa el `Volume` de la estrategia más el valor absoluto de cualquier exposición opuesta para garantizar una reversión con señales opuestas.

## Lógica de salida
Las operaciones largas se cierran cuando se activa cualquiera de las siguientes condiciones:
- El último cierre confirmado (`n-1`) cae de nuevo por debajo de la MA suavizada (cruce opuesto).
- El precio alcanza la distancia configurada de toma de ganancias larga en pips.
- El precio cae a la distancia configurada de stop-loss largo en pips.
- El trailing stop para operaciones largas bloquea ganancias una vez que el precio se ha movido `TrailingStopBuy` pips más allá del precio de entrada.

Las operaciones cortas replican las mismas reglas con sus respectivos parámetros y lógica de trailing. Cada vez que aparece una señal opuesta, la estrategia envía una orden de mercado lo suficientemente grande para cerrar la posición actual y abrir una en la nueva dirección.

## Gestión de riesgo y operaciones
- Las distancias para toma de ganancias, stop-loss y trailing stop se expresan en **pips**. La estrategia deriva el tamaño del pip de `Security.PriceStep`; cuando el símbolo usa 3 o 5 decimales, el pip se define como `PriceStep × 10`, coincidiendo con el ajuste original de MetaTrader.
- `InitializeLongTargets` e `InitializeShortTargets` calculan niveles de precio absolutos inmediatamente después de enviar la orden de mercado, almacenando la aproximación del precio de entrada basada en el último cierre confirmado.
- Cuando los trailing stops están habilitados y el precio se mueve favorablemente más allá de la distancia configurada, el nivel de stop se desplaza para preservar la ganancia no realizada.
- Ambos conjuntos de objetivos se reinician cuando la posición se cierra para que los niveles obsoletos nunca se reutilicen.

## Parámetros
- `MaPeriod` – longitud de la media móvil suavizada (predeterminado 15).
- `AdxPeriod` – período de suavizado ADX (predeterminado 12).
- `AdxThreshold` – valor ADX mínimo requerido para confirmar una tendencia (predeterminado 16).
- `TakeProfitBuy` / `StopLossBuy` / `TrailingStopBuy` – distancias en pips para operaciones largas.
- `TakeProfitSell` / `StopLossSell` / `TrailingStopSell` – distancias en pips para operaciones cortas.
- `CandleType` – marco temporal para las velas de entrada, predeterminado 1 minuto.

Configure el `Volume` de la estrategia para controlar el tamaño base de la orden. La implementación conserva el comportamiento original donde las operaciones cortas reciben sus propias configuraciones de riesgo en lugar de reutilizar los parámetros largos.
