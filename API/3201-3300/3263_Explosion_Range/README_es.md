# Estrategia de Explosion Range Expansion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Explosion Range Expansion Strategy es un sistema de ruptura convertido del asesor experto de MetaTrader 5 "Explosion". El algoritmo compara el rango de la vela completada actual con la vela anterior y abre una posición de mercado en la dirección del cuerpo de la vela siempre que la expansión del rango supere una proporción configurable. La versión de StockSharp mantiene las características originales de gestión de dinero y añade parámetros convenientes para control de horario y gestión del trailing stop.

## Reglas de trading
- **Expansión del rango:** Calcula el rango de la vela actual (`High - Low`) y lo compara con el rango de la vela anterior. Si el rango actual es mayor que el rango anterior multiplicado por `Range Ratio`, se genera una señal.
- **Filtro de dirección:**
  - Si la vela cierra por encima de su apertura y la posición actual es plana o corta, se envía una orden de mercado larga.
  - Si la vela cierra por debajo de su apertura y la posición actual es plana o larga, se envía una orden de mercado corta.
- **Ventana de trading:** Las señales se aceptan solo cuando el tiempo de cierre de la vela cae entre `Start Hour` y `End Hour` (inclusive).
- **Límite diario:** Cuando `One Trade Per Day` está habilitado, solo se ejecuta la primera entrada calificada del día de trading.
- **Pausa entre operaciones:** Después de una entrada de posición, la estrategia espera `Pause (sec)` segundos antes de aceptar una nueva señal.
- **Exposición máxima:** El tamaño neto de la posición no puede superar `Max Positions * Order Volume`.

## Salidas y gestión de riesgo
- **Protección inicial:** Los niveles opcionales de stop-loss y take-profit se definen en pasos de precio y se calculan desde el precio de entrada.
- **Trailing Stop:** Cuando está habilitado, el stop-loss se mueve más cerca del precio después de alcanzar un umbral mínimo de beneficio (`Trailing Stop + Trailing Step`). La lógica de trailing mantiene el mismo comportamiento que en el EA original.
- **Cierre manual en objetivos:** Si el rango de la vela alcanza el nivel de stop-loss o take-profit intrabarra, la posición se cierra usando una orden de mercado.

## Parámetros
- `Candle Type` – Tipo de datos usado para la suscripción de velas.
- `Order Volume` – Tamaño de cada posición en lotes.
- `Range Ratio` – Multiplicador aplicado al rango de la vela anterior para disparar entradas.
- `Max Positions` – Número máximo de lotes permitidos simultáneamente.
- `Pause (sec)` – Tiempo mínimo en segundos entre entradas.
- `Start Hour` / `End Hour` – Filtro de horas de trading (0–23).
- `One Trade Per Day` – Restringe la estrategia a una entrada por día calendario.
- `Stop Loss` – Distancia inicial de stop-loss en pasos de precio.
- `Take Profit` – Distancia inicial de take-profit en pasos de precio.
- `Trailing Stop` – Distancia de trailing stop en pasos de precio.
- `Trailing Step` – Distancia adicional requerida antes de actualizar el trailing.

## Notas de conversión
- La estrategia usa la API `SubscribeCandles` y `Bind` de alto nivel para el procesamiento de señales sin indicadores.
- El trailing stop, la ventana de trading, la pausa y el límite diario reproducen la lógica MQ5 original.
- La gestión de dinero se expresa mediante un único parámetro de volumen; el dimensionamiento de lote basado en porcentaje de riesgo del script original no está soportado en esta versión.
