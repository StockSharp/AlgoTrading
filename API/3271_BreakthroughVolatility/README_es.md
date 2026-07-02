# Estrategia de volatilidad por ruptura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de volatilidad por ruptura busca estallidos breves de volatilidad intrabar. Espera una vela cuyo rango se expanda por encima del de la vela anterior, pero solo dentro de una banda estrecha (dos equivalentes de pip después de la normalización de dígitos). Cuando dicha vela cierra alcista, la estrategia compra; cuando cierra bajista, vende. Stops de protección, un trailing stop opcional y una secuencia automática de inversión tras pérdida gestionan el riesgo e intentan recuperarse de movimientos adversos.

## Lógica de trading

1. **Filtro de expansión de rango**
   - Calcular el rango de la vela actual (`High - Low`) y compararlo con el de la vela anterior.
   - Exigir que el rango actual sea mayor, pero que no supere el rango anterior por más de dos pips normalizados.
   - Esto crea una configuración en la que la volatilidad aumenta pero sigue contenida, lo que apunta a una posible ruptura sin ruido excesivo.
2. **Sesgo direccional**
   - Si la vela cierra por encima de su apertura, entrar largo.
   - Si la vela cierra por debajo de su apertura, entrar corto.
   - La estrategia puede prohibir opcionalmente más de una entrada por barra para evitar señales repetidas en la misma vela.
3. **Gestión de posiciones**
   - El stop-loss inicial y el take-profit se asignan en puntos (equivalentes de pip) relativos al precio de entrada.
   - Un trailing stop opcional ajusta el nivel de protección cuando el precio se ha movido una distancia especificada a favor de la operación. Un paso trailing evita ajustes diminutos.
   - Cuando una posición se cierra con pérdida, la estrategia puede invertir la dirección de inmediato. Cada inversión aumenta la distancia de take-profit para compensar el riesgo adicional. Un límite en el número de inversiones consecutivas evita un comportamiento martingala infinito.

## Parámetros

| Nombre | Descripción | Predeterminado | Optimizable |
| --- | --- | --- | --- |
| `TradeVolume` | Volumen base de orden para entradas de mercado. | `0.1` | Sí |
| `StopLossPoints` | Distancia del stop-loss en puntos. | `20` | Sí |
| `TakeProfitPoints` | Distancia del take-profit en puntos. | `10` | Sí |
| `TrailingStopPoints` | Distancia del trailing stop en puntos. Establecer en `0` para desactivar. | `25` | No |
| `TrailingStepPoints` | Paso incremental mínimo al mover el trailing stop. | `5` | No |
| `OnlyOnePositionPerBar` | Prohíbe múltiples entradas durante la misma vela. | `true` | No |
| `UseAutoDigits` | Multiplica el tamaño del punto por 10 en símbolos con 3 o 5 decimales para convertir a unidades de pip. | `true` | No |
| `ReverseAfterStop` | Habilita el flujo de inversión tras pérdida. | `true` | No |
| `MaxReverseOrders` | Número máximo de operaciones inversas consecutivas. | `2` | No |
| `TakeProfitIncrease` | Puntos extra de take-profit añadidos por cada orden inversa. | `100` | No |
| `CandleType` | Tipo de vela y marco temporal para los cálculos. | `TimeSpan.FromMinutes(1)` | No |

## Gestión de riesgos

- Los desplazamientos de stop-loss y take-profit se recalculan usando el paso de precio del instrumento. La detección automática de dígitos convierte las cotizaciones de cinco dígitos en distancias del tamaño de un pip.
- La lógica trailing solo se activa después de que el mercado avance la distancia trailing especificada y exige un paso mínimo antes de modificar el stop.
- El trading inverso se reinicia después de una salida rentable o después de alcanzar el límite configurado de inversiones consecutivas.

## Notas prácticas

- Funciona mejor en pares de divisas con spreads ajustados, donde pequeños cambios de volatilidad pueden indicar ráfagas de momentum.
- Considere alinear el marco temporal de las velas con la sesión de mercado objetivo; el marco temporal predeterminado de 1 minuto captura rupturas de alta frecuencia.
- Como las inversiones se ejecutan inmediatamente después de un cierre perdedor, asegúrese de que haya margen suficiente para operaciones consecutivas.
