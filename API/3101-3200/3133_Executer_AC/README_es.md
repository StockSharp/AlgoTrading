# Estrategia de Executer AC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Executer AC** es un port fiel de StockSharp del asesor experto MetaTrader 5 "Executer AC". El EA original opera sobre el **Accelerator Oscillator (AC)** desarrollado por Bill Williams y combina sus oscilaciones de momentum con un marco fijo de stop/límite y un módulo de trailing stop. Esta conversión mantiene el comportamiento de la versión MQL5 al tiempo que expone parámetros fáciles de usar que se integran con la API de alto nivel de StockSharp.

## Lógica de trading

La estrategia opera con velas completadas del marco temporal seleccionado y se basa en los últimos cuatro valores del Accelerator Oscillator:

- `AC[0]` – barra completada más reciente (llamada `ac[1]` en el código original).
- `AC[1]`, `AC[2]`, `AC[3]` – valores progresivamente más antiguos usados para la detección de patrones.

El árbol de decisiones es idéntico al EA:

1. **Gestión de posición**
   - Las posiciones largas se cierran cuando `AC[0] < AC[1]` (momentum decreciente).
   - Las posiciones cortas se cierran cuando `AC[0] > AC[1]` (momentum creciente).
   - Una rutina de trailing stop ajusta dinámicamente el stop protector una vez que el precio supera la distancia configurada más el paso de trailing.
2. **Reglas de entrada cuando está plano**
   - **Aceleración alcista por encima de cero:** si `AC[0] > 0` y `AC[1] > 0` y `AC[0] > AC[1] > AC[2]`, se emite una compra de mercado.
   - **Aceleración bajista por encima de cero:** si `AC[0] > 0` y `AC[1] > 0` y `AC[0] < AC[1] < AC[2] < AC[3]`, se emite una venta de mercado.
   - **Aceleración alcista por debajo de cero:** si `AC[0] < 0` y `AC[1] < 0` y `AC[0] > AC[1] > AC[2] > AC[3]`, se emite una compra de mercado.
   - **Aceleración bajista por debajo de cero:** si `AC[0] < 0` y `AC[1] < 0` y `AC[0] < AC[1] < AC[2]`, se emite una venta de mercado.
   - **Cruces de la línea cero:** un cruce descendente (`AC[0] > 0` y `AC[1] < 0`) activa una compra, mientras que un cruce ascendente (`AC[0] < 0` y `AC[1] > 0`) activa una venta.

Las señales se evalúan solo después de confirmar que las velas están completadas, los valores del indicador están formados y el trading está habilitado.

## Gestión de riesgos

- **Stop-loss y take-profit:** distancias configurables (en pips) convertidas a unidades de precio usando el paso del instrumento. Los stops se recalculan en cada nueva entrada y permanecen sin cambios hasta que se alcancen o sean reemplazados por el trailing stop.
- **Trailing stop:** replica la lógica del EA. Cuando el beneficio no realizado supera `TrailingStop + TrailingStep` (ambos en pips), el precio del stop se mueve a `Close - TrailingStop` para posiciones largas y `Close + TrailingStop` para posiciones cortas, exigiendo la mejora requerida antes de cada paso.
- **Protección de posición:** se invoca el helper integrado `StartProtection()` para que StockSharp proteja contra desconexiones inesperadas.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Volumen de orden usado para todas las entradas de mercado. Se normaliza según el paso de volumen y los límites del instrumento. |
| `StopLossPips` | Distancia del stop-loss en pips. Un valor de cero deshabilita el stop-loss. |
| `TakeProfitPips` | Distancia del take-profit en pips. Un valor de cero deshabilita el take-profit. |
| `TrailingStopPips` | Distancia del trailing stop en pips. Establezca en cero para deshabilitar el trailing. |
| `TrailingStepPips` | Beneficio adicional mínimo (en pips) requerido antes de mover el trailing stop nuevamente. |
| `CandleType` | Marco temporal de las velas usadas para calcular el Accelerator Oscillator. |

## Notas de implementación

- La normalización de precios respeta tanto el tamaño del tick de la bolsa como los símbolos Forex de tres/cinco dígitos multiplicando el tamaño del punto por diez cuando corresponde.
- El historial del indicador se mantiene en un buffer de tamaño fijo para replicar las comparaciones originales `ac[1] … ac[4]` sin recurrir a colecciones costosas o consultas de historial.
- La estrategia siempre sale antes de evaluar nuevas entradas en la misma vela, coincidiendo con el flujo de control del EA MQL5 donde las instrucciones `return` previenen la re-entrada inmediata.
- Los valores del trailing stop actualizan tanto el estado de trailing interno como el precio efectivo del stop usado para las comprobaciones de stop-loss, asegurando consistencia con el comportamiento `PositionModify` del EA.

## Consejos de uso

1. Elija un marco temporal de velas que coincida con el mercado que opera (el script original se usaba comúnmente en gráficos Forex intradía).
2. Ajuste las distancias de stop-loss, take-profit y trailing a la volatilidad del instrumento elegido; valores extremadamente ajustados pueden provocar whipsaws frecuentes.
3. Habilite controles de riesgo en el lado del broker conectado cuando sea posible, ya que la estrategia depende de salidas del lado del software.
4. Combine con gestión de dinero a nivel de cartera si pretende ejecutar múltiples estrategias simultáneamente.
