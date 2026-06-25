# Estrategia de Oscilación MACD 1H EUR/USD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto MetaTrader "1H EUR_USD" a la API de alto nivel de StockSharp. Opera el par EUR/USD en velas horarias usando medias móviles duales y detección de oscilaciones MACD. Las entradas requieren tanto un filtro de tendencia (MA rápida por encima/por debajo de la MA lenta) como un patrón de doble fondo/doble techo MACD combinado con una ruptura de máximos o mínimos recientes. El riesgo se controla con stop loss, take profit basados en pips y un trailing stop incremental que refleja la lógica del EA original.

## Detalles

- **Mercado**: Diseñado para EUR/USD en el marco temporal de 1 hora, pero puede aplicarse a cualquier instrumento que produzca velas estándar.
- **Criterios de entrada**:
  - **Largo**:
    - La MA rápida está por encima de la MA lenta (tipo seleccionable entre SMA, EMA, SMMA, LWMA).
    - La línea principal MACD forma cualquiera de las siguientes oscilaciones alcistas completamente por debajo de la línea cero:
      - `MACD[-1] > MACD[-2] < MACD[-3]` con `MACD[-2] < 0` y el cierre actual rompe el máximo de la vela anterior.
      - `MACD[-2] > MACD[-3] < MACD[-4]` con `MACD[-3] < 0` y el cierre actual rompe el máximo de hace dos velas.
  - **Corto**:
    - La MA rápida está por debajo de la MA lenta.
    - La línea principal MACD forma las oscilaciones bajistas reflejadas completamente por encima de la línea cero y el precio cierra por debajo del mínimo previo relevante.
- **Criterios de salida**:
  - El take profit y el stop loss basados en pips se adjuntan inmediatamente después de la entrada.
  - El trailing stop se activa solo después de que el precio se mueva a favor por `TrailingStop + TrailingStep` pips y luego sigue el precio a una distancia de `TrailingStop` pips, siguiendo la lógica de modificación gradual del EA.
  - Las órdenes de protección se activan en el máximo/mínimo intraperiodo de la vela.
- **Gestión de posición**:
  - Usa el volumen de operación configurado; revertir posiciones cierra el lado opuesto antes de abrir el nuevo.
  - Las operaciones largas y cortas comparten los mismos cálculos de pip (el tamaño de pip se adapta automáticamente a cotizaciones de 4/5 dígitos).
- **Indicadores**:
  - Medias móviles rápida y lenta con tipo seleccionable (Simple, Exponencial, Suavizado, Ponderado Lineal) y desplazamiento horizontal opcional.
  - MACD clásico (longitudes de EMA rápida/lenta/señal).
- **Parámetros**:
  - `TradeVolume` – tamaño de lote base enviado con cada orden.
  - `StopLossPips`, `TakeProfitPips` – distancias de protección en pips (establezca en cero para deshabilitar).
  - `TrailingStopPips`, `TrailingStepPips` – configuración de seguimiento; el paso de seguimiento debe permanecer positivo cuando el seguimiento está activo.
  - `FastMaLength`, `FastMaShift`, `FastMaType` – configuración de la MA rápida.
  - `SlowMaLength`, `SlowMaShift`, `SlowMaType` – configuración de la MA lenta.
  - `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – parámetros MACD.
  - `CandleType` – marco temporal para procesamiento (predeterminado a 1 hora).
  - `LookbackPeriod` – preservado por compatibilidad con las entradas MQL; no altera la lógica porque el EA original también lo dejó sin usar.

## Notas

- El comportamiento del trailing stop refleja la versión MQL: no ocurre ningún ajuste hasta que tanto la distancia de seguimiento como el paso de seguimiento son superados por la ganancia no realizada.
- La estrategia asume que el paso de precio es igual al punto de cotización; si el instrumento tiene 3 o 5 dígitos decimales, el código escala automáticamente el tamaño del pip por 10.
- Los comentarios dentro del fuente C# explican cada bloque clave en inglés para facilitar el mantenimiento y la extensión.
