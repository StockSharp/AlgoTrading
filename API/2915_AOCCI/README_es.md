# Estrategia AOCCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del asesor experto de MetaTrader 5 `AOCCI` a la API de alto nivel de StockSharp.
- Combina el Awesome Oscillator y el Commodity Channel Index con un simple filtro de nivel pivote.
- Incluye protección de spread a través de filtros de "salto grande" y "salto doble" para omitir acción de precio inestable.
- Reproduce la lógica MQL5 original donde la configuración corta usa las mismas condiciones que la configuración larga.

## Datos e indicadores
- Usa el marco temporal primario definido por `CandleType` para la generación de señales.
- Se suscribe a un marco temporal superior adicional (`HigherCandleType`, predeterminado 1 hora) para leer el cierre anterior como filtro de tendencia.
- Indicadores:
  - `AwesomeOscillator` para detectar la dirección del impulso.
  - `CommodityChannelIndex` con período configurable y desplazamiento de señal opcional.
- Calcula un nivel pivote de la vela ubicada en `SignalCandleShift + 1` en el marco temporal de trabajo: `(High + Low + Close) / 3`.

## Lógica de entrada
1. Esperar hasta que ambos indicadores estén completamente formados y al menos seis velas terminadas estén disponibles.
2. Recopilar valores CCI con el desplazamiento configurado (`SignalCandleShift` para la comparación actual y `SignalCandleShift + 1` para la barra anterior).
3. Rechazar la barra cuando se activa cualquier filtro de salto:
   - `BigJumpPips` compara precios de apertura consecutivos de los últimos cinco intervalos.
   - `DoubleJumpPips` compara precios de apertura separados por una barra.
4. Entrada larga cuando se cumplen todas las condiciones siguientes y no hay posición activa:
   - El Awesome Oscillator es positivo en la barra actual.
   - El valor CCI desplazado es mayor o igual a cero.
   - El precio de cierre actual está por encima del nivel pivote.
   - Al menos una confirmación es bajista en los datos anteriores: valor AO anterior por debajo de cero, CCI desplazado anterior ≤ 0, o el último cierre del marco temporal superior por debajo del pivote.
5. La entrada corta usa exactamente el mismo conjunto de reglas que la entrada larga (el experto original contiene condiciones idénticas para ambas direcciones).

## Lógica de salida y gestión de riesgo
- Cuando se abre una operación, se asignan niveles opcionales de stop-loss y take-profit usando las distancias en pips configuradas multiplicadas por el tamaño de pip detectado del instrumento.
- En cada vela terminada, la estrategia verifica si se han alcanzado los niveles de take-profit o stop-loss usando los extremos de la vela y cierra la posición a mercado.
- El trailing stop se activa cuando tanto `TrailingStopPips` como `TrailingStepPips` son positivos:
  - Las operaciones largas mueven el stop a `Close - TrailingStopPips` una vez que el precio avanza al menos `TrailingStopPips + TrailingStepPips` desde la entrada.
  - Las operaciones cortas mueven el stop a `Close + TrailingStopPips` una vez que el precio cae la misma distancia combinada.
- Si se cierra una posición (por stop, objetivo o trailing), la estrategia espera hasta la siguiente vela para evaluar nuevas entradas.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `TradeVolume` | 1 | Volumen de orden base usado para entradas de mercado. |
| `StopLossPips` | 50 | Distancia en pips para el stop protector. Establecer en 0 para deshabilitar. |
| `TakeProfitPips` | 50 | Distancia en pips para el take-profit. Establecer en 0 para deshabilitar. |
| `TrailingStopPips` | 5 | Distancia del trailing stop en pips. Requiere `TrailingStepPips` > 0. |
| `TrailingStepPips` | 5 | Buffer adicional antes de que el trailing stop sea actualizado. |
| `CciPeriod` | 55 | Período del Commodity Channel Index. |
| `SignalCandleShift` | 0 | Desplazamiento aplicado al leer el buffer CCI y la vela pivote. |
| `BigJumpPips` | 100 | Diferencia máxima permitida (en pips) entre aperturas consecutivas de las últimas velas. |
| `DoubleJumpPips` | 100 | Diferencia máxima permitida (en pips) entre la apertura de cada segunda vela. |
| `CandleType` | velas de 15 minutos | Marco temporal de trabajo para las señales primarias. |
| `HigherCandleType` | velas de 1 hora | Marco temporal superior usado para obtener el cierre anterior de confirmación. |

## Notas
- El tamaño de pip se deriva de `Security.PriceStep` y se ajusta para instrumentos cotizados con 3 o 5 dígitos decimales.
- Debido a que el EA original usó filtros idénticos para ambas direcciones, las operaciones cortas solo ocurrirán si la condición larga también se cumple y la estrategia puede vender. Deshabilitar operaciones cortas externamente si no se desean.
- Los filtros de salto requieren al menos seis velas completadas antes de que se evalúe la primera operación.
