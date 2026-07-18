# Estrategia de Explosion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia reproduce la lógica del experto de MetaTrader "Explosion". Observa el rango de cada vela terminada y entra al mercado cuando la última barra "explota" — su altura más que duplica el rango de la barra anterior. La dirección la decide el cuerpo de la vela: un cuerpo alcista abre una posición larga, mientras que un cuerpo bajista abre una corta.

## Reglas de trading

1. Procesa solo las velas terminadas provenientes de la suscripción configurada `CandleType`.
2. Calcula el rango actual como `High - Low` y lo compara con el rango de la vela anterior.
3. Se abre una entrada **larga** cuando `currentRange > previousRange * 2` y el cierre está por encima de la apertura.
4. Se abre una entrada **corta** cuando `currentRange > previousRange * 2` y el cierre está por debajo de la apertura.
5. Cuando `OnlyOnePositionPerBar` está habilitado, se permite como máximo una operación por dirección para un tiempo de apertura de vela. Los intentos en la misma barra se ignoran.
6. La estrategia mantiene una posición neta única, por lo tanto una operación opuesta cierra automáticamente cualquier exposición existente antes de establecer la nueva dirección.
7. Mecánicas de protección:
   - `StopLossPips` y `TakeProfitPips` colocan niveles de salida virtuales medidos en pips desde el precio de entrada.
   - `TrailingStopPips` y `TrailingStepPips` mueven el stop una vez que el precio viaja a favor de la posición por al menos la distancia de trailing y cada paso adicional.
   - El multiplicador de pip opcional emula el asistente de auto-dígitos MQL multiplicando el tamaño del pip por 10 en instrumentos de 3 y 5 dígitos.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `TradeVolume` | `0.01` | Volumen de orden de mercado usado en entradas. |
| `StopLossPips` | `20` | Distancia de stop-loss en pips. Cero deshabilita el stop. |
| `TakeProfitPips` | `10` | Distancia de take-profit en pips. Cero deshabilita el take. |
| `TrailingStopPips` | `25` | Distancia de activación para el trailing stop en pips. Cero deshabilita el trailing. |
| `TrailingStepPips` | `5` | Movimiento adicional en pips requerido antes de que el trailing stop avance. Debe permanecer positivo cuando el trailing está habilitado. |
| `UseAutoPipMultiplier` | `true` | Multiplica el tamaño del pip por 10 en instrumentos con 3 o 5 decimales, coincidiendo con el asistente de auto-dígitos MQL. |
| `OnlyOnePositionPerBar` | `true` | Prohíbe más de una entrada por tiempo de apertura de barra. |
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Serie de velas usada para los cálculos. |

## Notas sobre la conversión

- StockSharp trabaja con una posición neta, por lo que el hedging de múltiples órdenes simultáneas del Expert Advisor original no está soportado. El comportamiento es equivalente a habilitar `OnlyOneOpenedPos` en la versión MQL.
- Las actualizaciones del trailing stop se realizan en cierres de velas en lugar de en cada tick. La lógica coincide con los umbrales originales mientras permanece compatible con la API de alto nivel.
- El multiplicador de pip reproduce la detección automática de dígitos que escala las distancias por 10 en símbolos forex de 5 dígitos.

## Uso sugerido

1. Elija el instrumento y marco temporal que coincidan con el experto original (por ejemplo, los gráficos M15/M30 recomendados para pares forex).
2. Ajuste los parámetros de riesgo basados en pips a la volatilidad del instrumento.
3. Habilite el registro para monitorear cuándo avanza el trailing stop y cómo se recalculan los niveles de protección.
