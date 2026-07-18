# Estrategia combinada de múltiples estrategias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia combinada de múltiples estrategias** es una conversión de C# del asesor experto MetaTrader 4 "Multi-Strategy iFSF". El EA original combina múltiples indicadores (MA, RSI, MACD, Stochastic, SAR) y los envuelve con filtros de tendencia, Bollinger rango y ruido. El puerto StockSharp conserva la misma idea utilizando secuencias `SubscribeCandles().Bind(...)` de alto nivel y clases de indicadores. Cada indicador habilitado produce un voto de COMPRA/VENTA; Sólo cuando todos los votos están de acuerdo la estrategia ejecuta una orden. Los filtros adicionales emulan los modos combinados de EA.

## Lógica central
* **Motor de consenso**: las medias móviles RSI, MACD, Stochastic y Parabolic SAR proporcionan cada una una señal discreta. Si todos los indicadores habilitados coinciden en COMPRAR (o VENDER), el consenso se vuelve alcista (o bajista).
* **Factor combinado (1–3)**: refleja la lógica `Combo_Trader_Factor` del EA. Cada factor combina el consenso con ADX detección de tendencias y Bollinger confirmación de rango de manera diferente:
  * *El factor 1* prefiere las condiciones de tendencia. Los estados del área de distribución dependen de Bollinger inversiones cuando están habilitados.
  * *El factor 2* requiere una confirmación más sólida: los filtros de tendencia y rango deben coincidir con el consenso.
  * *El factor 3* es la variante más estricta y exige alineación entre todos los módulos activos.
* **Detección de tendencias**: ADX en un período de tiempo configurable etiqueta el mercado como con tendencia hacia arriba/abajo o con rango hacia arriba/abajo.
* Filtro **Bollinger**: utiliza bandas medias (2σ) y anchas (3σ). Las señales largas requieren un rebote desde la banda inferior confirmado por los recientes valores de sobreventa RSI; Los pantalones cortos reflejan el comportamiento de la banda superior.
* **Filtro de ruido**: verificación basada en ATR que bloquea nuevas operaciones cuando la volatilidad es demasiado pequeña (reemplazo del Damiani Volatmeter).
* **Cierre automático**: cuando está habilitada, la estrategia sale instantáneamente si el consenso cambia en la dirección opuesta.

## Indicadores y señales
* **Promedios móviles** – Tres MA configurables (método + longitud). Los modos 1 a 5 reproducen las combinaciones de cruce originales (lógica agregada rápida versus media, media versus lenta).
* **RSI** – Los modos 1 a 4 cubren sobrecompra/sobreventa, impulso, combinado y comprobaciones de zona. Todos los umbrales son ajustables.
* **MACD** – Cuatro modos imitan el EA: pendiente de tendencia, cruce del histograma por debajo o por encima de cero, confirmación combinada y cruce por cero de la línea de señal.
* **Stochastic oscilador**: ya sea un cruce simple de %K vs %D o un cruce con umbrales alto/bajo.
* **Parabolic SAR**: voto direccional opcional, que admite el comportamiento "recordar la última señal" para evitar múltiples activadores por tendencia.

## Gestión de riesgos
* Compensaciones opcionales de stop-loss/take-profit (distancia de precio absoluta) configuradas a través de `StopLossOffset` y `TakeProfitOffset`.
* Compatibilidad con trailing stop integrada a través del asistente StockSharp `StartProtection`.
* La protección de posición diaria sigue la mecánica básica `Strategy`; no se requiere gestión manual de lotes.

## Parámetros clave
* **General** – `ComboFactor`, `CandleType`.
* **Promedios móviles**: `UseMa`, `MaMode`, longitudes/métodos individuales, período de tiempo de la vela, indicador "recordar el último".
* **RSI** – `UseRsi`, `RsiMode`, `RsiPeriod`, niveles de sobrecompra/sobreventa, umbrales de zona, indicador "recordar el último".
* **MACD** – `UseMacd`, `MacdMode`, longitudes rápidas/lentas/de señal, período de tiempo de la vela, indicador "recordar el último".
* **Stochastic** – `UseStochastic`, parámetros de suavizado, umbrales y período de vela.
* **SAR** – `UseSar`, configuración de aceleración, período de tiempo de la vela.
* **Filtro de tendencias**: `UseTrendDetection`, `AdxPeriod`, `AdxLevel`, período de vela.
* **Bollinger filtro** – `UseBollingerFilter`, `BollingerPeriod`, desviaciones medias/anchas, RSI longitud del rango.
* **Filtro de ruido** – `UseNoiseFilter`, `NoiseAtrLength`, `NoiseThreshold`, período de tiempo de la vela.
* **Cierre automático y riesgo**: `UseAutoClose`, `AllowOppositeAfterClose`, `StopLossOffset`, `TakeProfitOffset`, `UseTrailingStop`.

Todos los parámetros están expuestos como `StrategyParam<T>` para admitir la optimización, validación y agrupación de UI.

## Diferencias vs el MT4 EA
* Solo se utilizan indicadores integrados StockSharp. La opción original entre ZeroLag y el MACD clásico se reemplaza con la implementación nativa MACD.
* Todos los promedios móviles y osciladores operan con precios de cierre de velas. Las compensaciones de tipo de precio y cambio de MT4 (por ejemplo, `FastMa_Price`, `FastMa_Shift`) no están disponibles.
* El filtro de ruido Damiani se aproxima con ATR; el comportamiento se puede ajustar a través de `NoiseThreshold`.
* La administración del dinero y los reintentos de pedidos son manejados por StockSharp (sin bucles manuales `OrderSend`). La estrategia funciona con posiciones agregadas (`BuyMarket`/`SellMarket`).
* Se omiten el panel de comentarios y los objetos del gráfico de EA; en cambio, el registro está disponible a través de `LogInfo`.

## Uso
1. Agregue la clase `MultiStrategyComboStrategy` a su solución StockSharp y compílela.
2. Cree una instancia de la estrategia, establezca `Security`, `Portfolio` y el `Volume` deseado.
3. Configure plazos para cada indicador si se requiere confirmación de múltiples plazos (los valores predeterminados siguen las entradas de EA).
4. Opcionalmente, ajuste las compensaciones de parada/toma, el comportamiento de seguimiento y los umbrales de filtrado.
5. Inicia la estrategia. Las operaciones se activarán con velas cerradas cuando todos los módulos habilitados coincidan según el factor combinado seleccionado.

## Notas de conversión
* La estrategia se basa exclusivamente en API de suscripción de alto nivel (`SubscribeCandles().Bind(...)`); no se utilizan buffers de indicadores manuales.
* Las pestañas se utilizan para la sangría según las pautas del repositorio.
* Los extensos comentarios en línea resaltan cómo los conceptos EA se asignan al código StockSharp.
