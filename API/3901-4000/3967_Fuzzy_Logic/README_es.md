# Estrategia heredada de lógica difusa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia reproduce el asesor experto MetaTrader de "lógica difusa" de 2007 en StockSharp. Combina varias herramientas de Bill Williams
con osciladores de impulso y los evalúa a través de una tabla de puntuación difusa. Sólo cuando el puntaje agregado muestra una fuerte tendencia alcista.
r consenso bajista hace que el sistema abra una nueva posición. Un stop-loss fijo y un trailing stop opcional reflejan el método comercial original
reglas de gestión.

## Lógica de trading

1. Construya el Bill Williams Alligator (mandíbula, dientes, labios) utilizando promedios móviles suavizados y calcule la extensión de *Gator* como su
m de distancias absolutas entre las líneas.
2. Calcule Williams %R (período 14), DeMarker (período 14) y RSI (período 14) en las mismas velas.
3. Derive el Oscilador Acelerador (AC) de la secuencia Awesome Oscillator y rastrea hasta cinco barras consecutivas para detectar ac.
rayas de aceleración.
4. Cada indicador alimenta una tabla de membresía difusa de cinco niveles con puntos de interrupción predefinidos copiados del código original.
5. Las sumas ponderadas de las membresías producen un valor de decisión entre 0 y 1:
   - Los valores **> 0,75** indican un consenso alcista y desencadenan entradas largas.
   - Los valores **< 0,25** indican un consenso bajista y desencadenan entradas cortas.
6. Sólo se puede abrir una posición a la vez. Inmediatamente después de la entrada se colocan topes de protección.

## Gestión de Puestos

- **Stop-loss**: Distancia fija en pasos de precio (parámetro `Stop Loss (points)`).
- **Parada de seguimiento**: Opcional; cuando está habilitado, sigue la parada de protección por el número especificado de pasos de precio.
- **Administración de dinero**: tamaño opcional basado en el saldo que imita la fórmula MetaTrader `Volumen = (Saldo * (PorcentajeMM + DeltaM
M) - Saldo Inicial * DeltaMM) / 10000`.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Candle Type` | Serie de datos de velas utilizadas para el análisis. |
| `Long Threshold` | Nivel de decisión que se debe superar para abrir una posición larga. |
| `Short Threshold` | Nivel de decisión que se debe cruzar para abrir una posición corta. |
| `Stop Loss (points)` | Distancia del stop-loss inicial en pasos de precio. |
| `Trailing Stop (points)` | Distancia del trailing stop en incrementos de precio; configúrelo en `0` para deshabilitarlo. |
| `Fixed Volume` | Volumen de operaciones cuando la gestión del dinero está desactivada. |
| `Use Money Management` | Habilita la fórmula de administración de dinero estilo MetaTrader. |
| `Percent MM` | Porcentaje del saldo de la cuenta utilizado en la fórmula de administración del dinero. |
| `Delta MM` | Compensación porcentual adicional para la fórmula de administración del dinero. |
| `Initial Balance` | Saldo de referencia utilizado por la fórmula de gestión del dinero. |

## Notas

- La estrategia utiliza solo velas completadas (`CandleStates.Finished`) para evitar repintar.
- Todos los niveles y pesos de los indicadores siguen el asesor experto original, preservando su comportamiento.
- Para ejecutar el sistema intradía, ajuste el período de tiempo y los umbrales de las velas para reflejar la volatilidad deseada.
