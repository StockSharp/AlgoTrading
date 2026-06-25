# Estrategia Blau Ergodic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia traduce el asesor experto **Exp_BlauErgodic** de MQL5 a StockSharp. Reconstruye el oscilador Blau Ergodic mediante
el triple suavizado del momentum y su valor absoluto con filtros EMA, genera un oscilador normalizado y una línea de señal, y
ofrece tres modos de señal distintos que reflejan el EA original.

La configuración predeterminada evalúa velas completas de 4 horas. Puede cambiar el precio aplicado (cierre, apertura, promedios basados en alto/bajo),
cada profundidad de suavizado, y el índice de barra (`SignalBar`) usado para leer señales. Las operaciones se dimensionan mediante la propiedad
`Volume` de la estrategia; las entradas/salidas largas/cortas pueden deshabilitarse individualmente mediante parámetros booleanos. Los niveles de stop loss y
take profit protectores se definen en puntos y se convierten en precios absolutos a través de `Security.PriceStep`.

## Modos de señal

- **Breakdown** – reacciona al cruce del oscilador por la línea cero. Los largos se abren en transiciones de negativo a positivo y los cortos en
  transiciones de positivo a negativo. Las posiciones se cierran cuando el oscilador permanece en el lado opuesto de cero.
- **Twist** – busca reversiones de pendiente. Aparece una configuración larga cuando el oscilador estaba bajando en la barra anterior pero sube en
  la barra más reciente; una configuración corta requiere el patrón inverso.
- **CloudTwist** – monitorea el cruce del oscilador por su línea de señal. Los largos se activan cuando el oscilador sube a través de la nube de señal,
  y los cortos cuando cae de nuevo por debajo de ella.

Todos los modos leen los valores del indicador de la barra especificada por `SignalBar` (por defecto `1`, es decir, la última barra completada) y se apoyan en
valores más antiguos para la confirmación. Configure `SignalBar` en al menos `1` porque la conversión procesa solo velas terminadas.

## Reglas de entrada y salida

- **Entradas largas:** habilitadas cuando `AllowBuyEntry` es verdadero, no hay posición larga existente (`Position <= 0`), y el modo activo
  genera una condición de compra. La estrategia revierte cualquier exposición corta comprando `Volume + |Position|`.
- **Entradas cortas:** habilitadas cuando `AllowSellEntry` es verdadero, no hay posición corta existente (`Position >= 0`), y el modo activo
  emite una condición de venta. Cubre cualquier exposición larga antes de establecer el corto.
- **Salidas largas:** activadas por la condición específica del modo, o cuando se alcanzan `StopLossPoints` / `TakeProfitPoints`. Las
  salidas forzadas evitan el indicador `AllowBuyExit` para que los stops protectores siempre se respeten.
- **Salidas cortas:** análogas a la lógica de salida larga con `AllowSellExit` y niveles de stop para operaciones cortas.

## Parámetros

- `CandleType` – marco temporal para las suscripciones de velas (por defecto velas de 4 horas).
- `Mode` – uno de `Breakdown`, `Twist`, o `CloudTwist`.
- `MomentumLength` – lookback para la diferencia de momentum bruto.
- `First/Second/ThirdSmoothingLength` – profundidades de EMA para los filtros de momentum en cascada.
- `SignalSmoothingLength` – profundidad de EMA para la línea de señal.
- `SignalBar` – índice de la barra completada usado para leer señales (mínimo `1`).
- `AppliedPrices` – fuente de precio que alimenta el oscilador (cierre, apertura, mediana, típica, ponderada, etc.).
- `AllowBuyEntry`, `AllowSellEntry`, `AllowBuyExit`, `AllowSellExit` – habilitar o deshabilitar operaciones específicas.
- `StopLossPoints`, `TakeProfitPoints` – distancias protectoras en puntos (convertidas mediante `Security.PriceStep`).

La conversión mantiene el comportamiento del experto MQL5, aprovechando la API de alto nivel de StockSharp (`SubscribeCandles`,
`Bind`) y adhiriéndose a las convenciones de estrategia de StockSharp con indentación de tabulaciones y comentarios en inglés.
