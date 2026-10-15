# Estrategia VR Moving Distance
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de StockSharp replica el asesor experto VR-Moving de MetaTrader 5. Monitorea una media móvil configurable y reacciona cuando el precio se aleja más allá de una distancia fija en pips. El algoritmo puede escalar en tendencias multiplicando el volumen base de la orden en entradas posteriores y aplica lógica simple de toma de ganancias mientras solo una posición está abierta.

## Descripción general
- Opera el instrumento asignado a la estrategia usando una única suscripción de velas.
- Calcula una media móvil con longitud, tipo de suavizado y fuente de precio seleccionables.
- Convierte los ajustes de distancia y toma de ganancias de pips a desplazamientos de precio usando el paso de precio del instrumento.
- Añade posiciones largas cuando el precio sube suficientemente por encima de la media móvil, o posiciones cortas cuando el precio cae por debajo de ella.
- Invierte la exposición neta actual antes de abrir una posición en la dirección opuesta para mantener la estrategia compatible con el neteo del portafolio.

## Indicadores y datos
- Una media móvil (`Simple`, `Exponential`, `Smoothed`, `Weighted` o `VolumeWeighted`).
- Las velas llegan con el `Candle Type` configurado; el mismo flujo impulsa los valores del indicador y las decisiones de trading.

## Lógica de entrada
1. En cada vela finalizada la estrategia espera a que la media móvil esté completamente formada.
2. Si el máximo de la barra está al menos `DistancePips` por encima de la media móvil, se activa una entrada larga.
3. Si el mínimo de la barra está al menos `DistancePips` por debajo de la media móvil, se activa una entrada corta.
4. Al cambiar de dirección la estrategia cierra la exposición existente añadiendo el volumen opuesto a la nueva orden de mercado.

## Escalado y gestión de volumen
- La primera orden usa el `BaseVolume` configurado.
- Las órdenes posteriores en la misma dirección usan `BaseVolume * VolumeMultiplier`.
- Se registra el precio de ejecución más alto en el lado largo y el más bajo en el lado corto. Cada nueva orden de escalado requiere que el precio se extienda otro `DistancePips` desde ese extremo antes de ejecutarse.

## Lógica de salida
- Cuando exactamente una posición larga está abierta, se coloca un objetivo de beneficio en el precio de entrada más `TakeProfitPips` (convertidos a unidades de precio). Si el máximo de una vela toca el objetivo, la posición se cierra.
- Del mismo modo, una posición corta única recibe un objetivo de beneficio en la entrada menos `TakeProfitPips` y se cierra cuando el mínimo de la vela lo toca.
- Una vez que existen múltiples entradas la estrategia mantiene las posiciones abiertas y espera nuevas señales de escalado; no se intenta una salida promediada en este puerto.

## Notas de gestión de riesgo
- `StartProtection()` se activa al inicio para conectarse con los subsistemas de protección estándar de StockSharp.
- Los valores de distancia y toma de ganancias se miden en pips. Para símbolos cotizados con 3 o 5 decimales la estrategia multiplica el paso de precio por 10 para coincidir con la semántica de pips de MetaTrader.
- No hay stop-loss automático; el riesgo debe controlarse mediante los parámetros elegidos y los límites externos del portafolio.

## Parámetros
- **Candle Type** – Tipo de datos usado para la suscripción de velas.
- **MA Length** – Período de la media móvil.
- **MA Type** – Método de suavizado de la media móvil.
- **Price Source** – Precio de la vela usado para calcular la media móvil.
- **Distance (pips)** – Brecha mínima en pips entre el precio y la media móvil para activar entradas.
- **Take Profit (pips)** – Distancia del objetivo de beneficio aplicada cuando solo una posición está abierta.
- **Volume Multiplier** – Multiplicador aplicado al volumen base para entradas adicionales.
- **Base Volume** – Cantidad de la operación inicial.
