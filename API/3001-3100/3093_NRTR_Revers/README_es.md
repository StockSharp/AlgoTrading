# Estrategia NRTR Revers
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia NRTR Revers es una conversión en C# del expert advisor original de MetaTrader 5 `NRTR_Revers.mq5`. El sistema utiliza el enfoque Nick Rypock Trailing Reverse (NRTR) para alternar entre sesgo largo y corto dependiendo de cómo interactúa el precio con las bandas de soporte y resistencia proyectadas por ATR. Las decisiones de trading se evalúan al cierre de cada vela terminada proveniente de una suscripción de un solo marco temporal.

## Lógica de trading

1. **Proyección ATR** – La estrategia calcula un Average True Range (ATR) con el período configurable. El valor ATR se multiplica por el `VolatilityMultiplier` para obtener el desplazamiento de la banda.
2. **Bandas dinámicas** – Para la dirección de tendencia actual la estrategia encuentra:
   - El mínimo más bajo (o el máximo más alto) entre las velas que se alinean con la configuración original de ventana MQL.
   - Un extremo secundario que se desplaza más profundo en la historia. La distancia entre la banda primaria y este extremo secundario se utiliza junto con el umbral `ReversePips` para confirmar reversiones fuertes.
3. **Cambios de tendencia** – Cuando el cierre anterior se mueve fuera de la banda ATR o la diferencia del extremo secundario supera la distancia de reversión, el sesgo cambia (de largo a corto o viceversa). Si existe una posición opuesta se cierra primero; de lo contrario, se abre inmediatamente una nueva posición en la nueva dirección.
4. **Espera de posición plana** – Después de emitir una orden de mercado opuesta para cerrar una posición existente, la estrategia espera hasta que el portafolio esté plano antes de enviar la nueva orden de entrada. Este comportamiento refleja el expert advisor original.
5. **Gestión del riesgo** – Los niveles de stop-loss, take-profit y trailing stop se definen en pips y se convierten a precios absolutos usando un valor de punto ajustado (compatible con símbolos forex de 3 y 5 decimales). Las actualizaciones del trailing requieren un progreso de precio mayor que `TrailingStopPips + TrailingStepPips`, coincidiendo con la lógica de MT5.

## Parámetros

- `CandleType` – Marco temporal principal al que suscribirse para obtener datos de precio.
- `AtrPeriod` – Longitud de promediado del ATR utilizada en el cálculo de la banda.
- `VolatilityMultiplier` – Multiplicador aplicado al valor del ATR para dimensionar el desplazamiento desde el extremo.
- `ReversePips` – Distancia adicional basada en pips que debe exceder el extremo secundario antes de que el sesgo cambie.
- `StopLossPips` – Distancia de stop protector en pips desde el precio de entrada (establecer en cero para deshabilitar).
- `TakeProfitPips` – Distancia de objetivo de beneficio en pips desde el precio de entrada (establecer en cero para deshabilitar).
- `TrailingStopPips` – Distancia de activación del trailing stop medida en pips (establecer en cero para deshabilitar el trailing).
- `TrailingStepPips` – Distancia extra en pips requerida antes de que ocurran actualizaciones del trailing; debe ser positivo cuando el trailing está activo.
- `TradeVolume` – Volumen de orden utilizado para nuevas entradas (en lotes/contratos dependiendo de la configuración del valor).

## Notas

- Los cálculos de indicadores y las verificaciones de reversión solo usan velas terminadas; las velas incompletas se ignoran.
- El valor ATR suministrado por la vinculación es equivalente al ATR de la barra anterior utilizado en el EA fuente porque los cálculos ocurren después de la finalización de la vela.
- El cálculo del punto ajustado maneja automáticamente cotizaciones forex de 3 y 5 decimales para mantener los parámetros basados en pips compatibles con el script original.
- No se proporciona un port de Python por solicitud. La carpeta actualmente contiene solo la implementación en C# y la documentación.
