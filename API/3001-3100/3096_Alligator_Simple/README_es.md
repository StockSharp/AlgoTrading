# Estrategia Simple de Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Simple de Alligator recrea el expert advisor de MetaTrader "Alligator Simple v1.0" usando la API de alto nivel de StockSharp. Lee el indicador Alligator de Bill Williams en velas terminadas y abre una posición cuando las líneas Lips, Teeth y Jaw se expanden en la misma dirección en la barra completada anterior. Cada operación puede incluir opcionalmente gestión de stop-loss, take-profit y trailing stop basados en pips que refleja la implementación MQL original.

## Indicadores y datos
- **Líneas Alligator**: tres Medias Móviles Suavizadas (SMMA) calculadas sobre el precio mediano de la vela `(high + low) / 2` con longitudes configurables y desplazamientos hacia adelante para el Jaw, Teeth y Lips.
- **Velas**: la estrategia se suscribe a un único `CandleType` configurable (velas de una hora por defecto) y solo procesa velas terminadas para evitar el sesgo de mirar hacia adelante.

## Lógica de trading
1. **Evaluación de señales**
   - Recuperar los valores desplazados del Alligator para la vela completada anterior.
   - Señal larga: `Lips[t-1] > Teeth[t-1] > Jaw[t-1]`.
   - Señal corta: `Lips[t-1] < Teeth[t-1] < Jaw[t-1]`.
2. **Ejecución**
   - Entrar al mercado con `OrderVolume` cuando no hay posición abierta.
   - Solo se mantiene una posición a la vez; las señales opuestas se ignoran hasta que la posición actual se cierra.

## Salida y gestión del riesgo
- **Stop-loss inicial**: si `StopLossPips > 0`, la estrategia desplaza el precio de ejecución por la distancia en pips convertida con el paso de precio del instrumento (incluyendo el multiplicador de pips de 3/5 dígitos usado por los símbolos de MetaTrader).
- **Take-profit**: cuando `TakeProfitPips > 0`, se coloca un objetivo de beneficio simétricamente alrededor del precio de entrada. Un valor de cero desactiva el objetivo.
- **Trailing stop**: cuando tanto `TrailingStopPips` como `TrailingStepPips` son positivos, el stop avanza a `close − TrailingStop` (largos) o `close + TrailingStop` (cortos) una vez que el precio se ha movido al menos `TrailingStop + TrailingStep` a favor de la operación. Las actualizaciones del trailing se basan en el máximo/mínimo de la vela para simular toques intra-barra.
- **Gestión de salida**: las condiciones de stop-loss, take-profit y trailing emiten órdenes de mercado para aplanar la posición y se evalúan en cada vela terminada.

## Parámetros
- `OrderVolume` (predeterminado **1**): tamaño de la operación en lotes o contratos.
- `StopLossPips` (predeterminado **100**): distancia del stop-loss inicial en pips. Establecer en cero para deshabilitar.
- `TakeProfitPips` (predeterminado **100**): distancia del take-profit en pips. Establecer en cero para deshabilitar.
- `TrailingStopPips` (predeterminado **5**): distancia del trailing stop en pips. Cero desactiva el trailing.
- `TrailingStepPips` (predeterminado **5**): distancia extra en pips que el precio debe recorrer antes de que avance el trailing stop. Debe ser positivo cuando el trailing está habilitado.
- `JawPeriod`, `TeethPeriod`, `LipsPeriod`: longitudes SMMA para el jaw, teeth y lips (predeterminados 13/8/5).
- `JawShift`, `TeethShift`, `LipsShift`: desplazamientos hacia adelante aplicados al leer los valores del Alligator (predeterminados 8/5/3).
- `CandleType`: tipo/marco temporal de datos de velas para los cálculos (predeterminado velas de una hora).

## Notas de implementación
- Las distancias en pips se adaptan automáticamente al tamaño del tick del valor. Los instrumentos con tres o cinco decimales multiplican el paso de precio por diez para replicar la definición de pip de MetaTrader.
- Los buffers de historial del indicador almacenan suficientes valores para respetar los desplazamientos hacia adelante configurados, eliminando la manipulación manual de arrays.
- La estrategia usa los helpers `BuyMarket` y `SellMarket` para enviar órdenes, manteniendo el código enfocado en la generación de señales y el manejo del riesgo.
