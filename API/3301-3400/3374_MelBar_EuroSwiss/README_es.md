# Estrategia MelBar EuroSwiss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia MelBar EuroSwiss reproduce la lógica del asesor experto "MelBar EuroSwiss M30 500 1.85x 2Y". Combina Bollinger entradas de ruptura de banda con un filtro de salida basado en el índice de vigor relativo (RVI). La plantilla predeterminada está ajustada para el par EUR/CHF en el marco temporal M30, pero los parámetros se pueden optimizar para otros símbolos.

Al comienzo de cada vela terminada, la estrategia lee las bandas Bollinger y los valores RVI calculados sobre los precios de cierre. Las nuevas posiciones se abren cuando la barra actual se abre más allá de la envolvente mientras que la barra anterior se abre dentro del canal. Este comportamiento imita la lógica de ruptura estilo espacio del robot MQL5 original. Las operaciones largas utilizan la banda inferior como disparador, mientras que las operaciones cortas reaccionan a la banda superior. Las posiciones existentes se cierran cuando el RVI retrasado cruza por encima o por debajo de un nivel absoluto, lo que indica el agotamiento del impulso en la dirección de la operación. Las órdenes de protección opcionales se establecen utilizando distancias de puntos fijas.

El volumen predeterminado es 0,2 lotes, pero el parámetro `TradeVolume` permite un control preciso sobre el tamaño de la posición. Tanto el stop loss como el takeprofit se expresan en pips y se convierten en compensaciones de precios a través del parámetro configurable `PipSize`. El mismo tamaño de pipa se reutiliza para armar el módulo de protección en el arranque. Todos los cálculos se basan en velas terminadas para evitar el sesgo de anticipación.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Vela actual abierta < banda inferior anterior Bollinger Y vela anterior abierta > banda inferior de hace dos velas.
  - **Corto**: Vela actual abierta > banda superior anterior Bollinger Y vela anterior abierta <banda superior de hace dos velas.
- **Criterios de salida**:
  - **Largo**: Cerrar cuando el valor histórico de RVI excede +`RviLevel`.
  - **Corto**: Cerrar cuando el valor histórico de RVI cae por debajo de -`RviLevel`.
- **Stops**: Distancias de stop loss fijas opcionales y toma de ganancias en pips.
- **Indicadores**: Bollinger Bandas (período `BollingerPeriod`, desviación `BollingerDeviation`) e Índice de Vigor Relativo (`RviPeriod`).
- **Valores predeterminados**:
  - `TradeVolume` = 0,2 lotes
  - `BollingerPeriod` = 18
  - `BollingerDeviation` = 2,75
  - `RviPeriod` = 15
  - `RviLevel` = 0,30
  - `StopLossPips` = 13
  - `TakeProfitPips` = 61
  - `PipSize` = 0,0001
  - `CandleType` = Intervalo de tiempo.DesdeMinutos(30)
- **Otras notas**:
  - Categoría: Reversión de ruptura
  - Dirección: tanto larga como corta.
  - Plazo: Intradiario (M30 por defecto)
  - Nivel de riesgo: Medio debido a controles de riesgo fijos basados en pips
  - Trailing stop: no habilitado de forma predeterminada (se puede implementar externamente)

Los parámetros proporcionados reflejan la configuración original y sirven como un sólido punto de partida para pruebas de avance o ejecuciones de optimización en StockSharp.
