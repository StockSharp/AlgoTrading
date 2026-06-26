# Estrategia Exp XHullTrend Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del experto MQL5 `Exp_XHullTrend_Digit.mq5` ubicado en `MQL/22117`.
- Utiliza la API de alto nivel de StockSharp con el `XHullTrendDigitIndicator` personalizado que replica la lógica original de XHullTrend Digit.
- Enfocado en el seguimiento de tendencia a medio plazo en el marco temporal configurado del indicador (8 horas por defecto).

## Lógica del indicador
1. El precio se toma de la fuente de vela seleccionada (cierre por defecto).
2. Se calculan dos medias móviles con longitudes `BaseLength` y `BaseLength / 2` usando el método de suavizado elegido (simple, exponencial, suavizado o ponderado).
3. Una proyección estilo Hull `2 * shortMA - longMA` se suaviza dos veces: primero por `SignalLength`, luego por `sqrt(BaseLength)`.
4. Ambas líneas resultantes se redondean al múltiplo más cercano del paso del instrumento escalado por `10^RoundingDigits` para imitar el redondeo de dígitos de la versión MQL5.
5. Cuando el redondeo produce valores iguales mientras los valores brutos difieren, la línea más rápida se desplaza un paso en la dirección de la diferencia para que el cruce siga siendo detectable.

## Reglas de trading
- Las señales se evalúan únicamente en velas cerradas.
- `SignalBar` define cuántas barras atrás se usan para la detección del cruce (1 = usar la barra completada anterior contra la barra anterior a ella).
- Entrada larga: línea rápida anterior por encima de la lenta **y** la línea rápida de la barra seleccionada en o por debajo de la lenta (cruce hacia arriba). Las posiciones cortas se cierran opcionalmente al mismo tiempo.
- Entrada corta: línea rápida anterior por debajo de la lenta **y** la línea rápida de la barra seleccionada en o por encima de la lenta (cruce hacia abajo). Las posiciones largas se cierran simultáneamente de forma opcional.
- Salida larga: cuando la línea rápida anterior cae por debajo de la lenta.
- Salida corta: cuando la línea rápida anterior sube por encima de la lenta.
- Si aparece una señal de reversión mientras se mantiene la posición opuesta, la estrategia envía la orden de cierre seguida de una orden dimensionada para voltear la posición a la nueva dirección.

## Parámetros
- `OrderVolume` – volumen para entradas de mercado.
- `StopLoss` / `TakeProfit` – distancias de protección opcionales en pasos de precio (convertidas a `UnitTypes.Step` de StockSharp).
- `EnableBuyEntry`, `EnableSellEntry` – permitir o bloquear nuevas posiciones en cada dirección.
- `EnableBuyExit`, `EnableSellExit` – controlar las salidas automáticas para lados largos y cortos.
- `CandleType` – marco temporal usado para cálculos del indicador (marco temporal de 8 horas por defecto).
- `BaseLength` – longitud de suavizado base para el indicador (equivale a `XLength` en MQL5).
- `SignalLength` – longitud del suavizado Hull intermedio (`HLength` en MQL5).
- `PriceSource` – precio de vela usado para los cálculos (cierre/apertura/máximo/mínimo/típico/ponderado/mediano/promedio).
- `SmoothMethod` – tipo de media móvil para todas las etapas de suavizado (simple, exponencial, suavizado, ponderado).
- `Phase` – mantenido por compatibilidad; sin efecto con los tipos de suavizado soportados.
- `RoundingDigits` – número de ajustes de dígitos adicionales aplicados durante el redondeo.
- `SignalBar` – desplazamiento de barra para la evaluación de señales (0 = barra cerrada actual, 1 = barra anterior, etc.).

## Gestión de riesgo
- Stop loss y take profit opcionales manejados por el helper `StartProtection` incorporado usando distancias basadas en pasos.
- El volumen puede ajustarse a través de `OrderVolume` para coincidir con el tamaño del instrumento objetivo.

## Notas
- El indicador personalizado reproduce el comportamiento de redondeo del script original; asegúrese de que `Security.PriceStep` esté configurado para un redondeo preciso.
- Solo se implementan los suavizados SMA, EMA, SMMA (RMA) y LWMA porque la biblioteca estándar de StockSharp los proporciona de serie. Otros modos de suavizado exóticos de la fuente MQL5 pueden añadirse posteriormente si es necesario.
- Funciona en cualquier instrumento que entregue velas para el marco temporal seleccionado. Ajuste los dígitos de redondeo y la longitud base al cambiar entre activos con diferentes tamaños de tick.
