# Estrategia SAR RSI MTS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia SAR RSI MTS** es una traducción directa del asesor experto original de MetaTrader 5 "SAR RSI MTS" a la API de alto nivel de StockSharp. El sistema sigue la dirección del indicador Parabolic SAR y confirma las entradas con el Índice de Fuerza Relativa (RSI). Trabaja únicamente sobre velas completadas (marco temporal predeterminado de 1 hora) y respeta un límite configurable sobre el tamaño neto de la posición.

## Indicadores y datos

- **Parabolic SAR** (`Acceleration = SarStep`, `AccelerationStep = SarStep`, `AccelerationMax = SarMax`).
- **Índice de Fuerza Relativa** con período personalizable y nivel neutro (por defecto 50).
- Velas suministradas por `CandleType`, que por defecto es datos de marco temporal horario.

Internamente la estrategia calcula un valor de pip a partir de los metadatos del instrumento. Si el símbolo tiene 3 o 5 decimales, multiplica el paso de precio por 10, lo que coincide con el manejo de pips del programa MQL original.

## Lógica de entrada

Una nueva operación se evalúa al cierre de cada vela terminada una vez que ambos indicadores han producido valores válidos:

- **Configuración larga**
  1. El valor del Parabolic SAR de la barra anterior está por debajo del cierre actual y el SAR actual ha aumentado respecto al valor anterior.
  2. El RSI está por encima del umbral neutro y está subiendo en comparación con su lectura anterior.
  3. Si la cuenta ya está neta corta, la estrategia primero compra suficiente volumen para voltear la posición y luego abre un nuevo largo dimensionado según el parámetro `Volume`, respetando el límite `MaxPosition`.

- **Configuración corta**
  1. El valor anterior del Parabolic SAR está por encima del cierre actual y el SAR actual ha disminuido.
  2. El RSI está por debajo del umbral neutro y está cayendo en comparación con su valor anterior.
  3. La exposición larga existente se elimina antes de establecer el nuevo corto. Se permiten cortos adicionales hasta que la posición absoluta alcance `MaxPosition`.

Todas las comparaciones utilizan la precisión del instrumento para que las pruebas de igualdad coincidan con el helper `CompareDoubles` original de MQL.

## Salida y gestión de riesgos

Los controles de riesgo se evalúan antes de verificar nuevas entradas en cada vela terminada:

- **Stop-loss fijo** en pips convertidos a unidades de precio y aplicado al precio promedio de entrada de la posición neta actual.
- **Take-profit fijo** en pips, manejado simétricamente al stop-loss.
- **Stop trailing** que se activa solo después de que el beneficio no realizado supera `TrailingStop + TrailingStep`. El stop se mueve en pasos discretos, imitando la rutina "Trailing" de la estrategia MQL.
- Si ninguno de los anteriores aplica, el estado del trailing se reinicia cada vez que la posición se aplana.

Todas las salidas cierran la posición neta completa (larga o corta). Cuando se activa una regla de protección, la estrategia omite la evaluación de señales para la misma barra, reflejando el comportamiento de las órdenes stop del lado del broker en la implementación original.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `StopLossPips` | Distancia del stop-loss expresada en pips. Un valor de `0` deshabilita el stop de protección. |
| `TakeProfitPips` | Distancia del take-profit en pips. Deshabilitado cuando se establece en `0`. |
| `TrailingStopPips` | Distancia del trailing stop. Deshabilitado cuando se establece en `0`. |
| `TrailingStepPips` | Mejora mínima de precio requerida antes de avanzar el trailing stop. |
| `SarStep` | Paso de aceleración para Parabolic SAR; también se usa como factor de aceleración inicial. |
| `SarMax` | Factor de aceleración máximo para Parabolic SAR. |
| `RsiPeriod` | Período de lookback para el indicador RSI. |
| `RsiNeutralLevel` | Umbral RSI que separa el sesgo alcista y bajista (por defecto 50). |
| `CandleType` | Suscripción de velas usada para los cálculos (por defecto 1 hora). |
| `MaxPosition` | Máxima posición neta absoluta permitida por la estrategia. |

## Notas adicionales

- La configuración predeterminada reproduce las entradas originales del EA: stop de 10 pips, objetivo de 40 pips, trailing stop de 15/5 pips, Parabolic SAR `0.05/0.5` y período RSI `14`.
- El volumen se controla mediante la propiedad base `Strategy.Volume`. El escalado de posición respeta `MaxPosition` y gestiona automáticamente las reversiones.
- Los enlaces de indicadores y el enrutamiento de órdenes se basan íntegramente en la API de alto nivel de StockSharp sin acceso manual a series, garantizando el cumplimiento de las directrices del proyecto.
