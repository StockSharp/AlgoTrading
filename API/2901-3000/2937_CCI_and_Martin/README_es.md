# Estrategia CCI and Martin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
La estrategia CCI and Martin busca reversiones bruscas después de una secuencia corta bajista o alcista y confirma el movimiento con el Índice de Canal de Materias Primas (CCI). La lógica replica el asesor experto original de MetaTrader 5 mientras usa la API de alto nivel de StockSharp. La estrategia trabaja solo con velas terminadas y puede operar en cualquier instrumento para el que estén disponibles los valores de CCI y los pasos de precio.

## Reglas de Trading
- **Configuración alcista**
  - Las velas `-2` y `-1` deben ser ambas bajistas (apertura mayor que cierre).
  - La vela `0` debe cerrar por encima de su apertura y por encima de la apertura de la vela `-1`.
  - El CCI en la vela `-1` debe estar por debajo de `+5`, por debajo del valor de la vela `-2`, y tanto `-2` como `-3` deben mostrar una secuencia decreciente. El CCI actual (vela `0`) debe girar hacia arriba por encima del valor anterior.
  - Cuando todas las condiciones se cumplen y no hay posición abierta, la estrategia entra en un trade largo.
- **Configuración bajista**
  - Las velas `-2` y `-1` deben ser ambas alcistas (apertura menor que cierre).
  - La vela `0` debe cerrar por debajo de su apertura y por debajo de la apertura de la vela `-1`.
  - El CCI en la vela `-1` debe estar por encima de `-5`, por encima del valor de la vela `-2`, y tanto `-2` como `-3` deben formar una secuencia creciente. El CCI actual (vela `0`) debe girar hacia abajo por debajo del valor anterior.
  - Cuando todas las condiciones se cumplen y no hay posición abierta, la estrategia entra en un trade corto.

El algoritmo monitorea solo velas completadas. La implementación MQL original esperaba 40 segundos después de la apertura del minuto para evitar señales prematuras; el uso de velas terminadas hace innecesario este filtro.

## Gestión de Riesgo
- Las distancias de **stop-loss** y **take-profit** se definen en pips. Se convierten a offsets de precio multiplicando el paso de precio del instrumento por diez cuando el paso corresponde a una cotización de 3 o 5 dígitos, reflejando el cálculo original de pips.
- El **trailing stop** se activa después de que el precio avanza la distancia del trailing stop más el paso de trailing. El stop se mueve entonces para mantener la distancia de trailing y solo avanza cuando la mejora de precio supera el paso configurado.
- Si el stop-loss o el take-profit se establece en cero, la salida respectiva se deshabilita. El trailing requiere que tanto la distancia del stop como el paso sean positivos.

## Gestión de Volumen
Dos motores opcionales de dimensionamiento de posición pueden alterar el tamaño del lote después de cada trade.
- **Escalado Martingala** multiplica el volumen actual por el coeficiente martingala una vez que el número de pérdidas consecutivas alcanza el disparador. El escalado se detiene después del número configurado de pasos martingala. Cualquier trade rentable restablece el volumen al valor inicial.
- **Ajustes por pasos** incrementan el volumen en una cantidad fija ya sea después de pérdidas o después de ganancias, dependiendo del modo seleccionado. El incremento se normaliza al paso de volumen del instrumento y está limitado por el parámetro de volumen máximo. Cuando se supera el límite o un trade no cumple la condición de disparador, el volumen vuelve al tamaño inicial.

El asesor experto original prohíbe habilitar la lógica martingala y de pasos simultáneamente; el port en C# aplica la misma restricción.

## Parámetros
- `CandleType` – serie de velas usada para análisis.
- `CciPeriod` – longitud de promedio para el Índice de Canal de Materias Primas.
- `InitialVolume` – tamaño base de la orden antes de cualquier escalado.
- `StopLossPips` – distancia del stop-loss expresada en pips.
- `TakeProfitPips` – distancia del take-profit expresada en pips.
- `TrailingStopPips` – distancia del trailing stop en pips (0 deshabilita el trailing).
- `TrailingStepPips` – mejora de precio mínima requerida antes de que el trailing stop se mueva.
- `EnableMartingale` – activa el escalado estilo martingala después de pérdidas.
- `MartingaleCoefficient` – multiplicador aplicado al volumen actual para trades martingala.
- `MartingaleTriggerLosses` – número de trades perdedores consecutivos necesarios antes del escalado.
- `MartingaleMaxSteps` – número máximo de multiplicaciones martingala.
- `EnableStepAdjustments` – habilita incrementos de volumen basados en pasos.
- `StepVolumeIncrement` – incremento absoluto aplicado cuando la regla de pasos se activa.
- `StepVolumeMax` – límite superior para el volumen basado en pasos.
- `StepAdjustmentMode` – selecciona si el incremento por pasos se activa después de una pérdida o después de una ganancia.

## Notas
- La estrategia asume que las órdenes de mercado se ejecutan cerca del precio solicitado. La lógica protectora recalcula los stops en cada vela terminada para emular el trailing basado en ticks del EA original.
- Si el paso de precio del instrumento no corresponde a la cotización FX clásica, la conversión de pips sigue funcionando, pero las distancias basadas en pips pueden representar valores monetarios diferentes.
