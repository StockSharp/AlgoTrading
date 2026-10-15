# Estrategia Exp XRSI Histograma Vol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una conversión en C# del asesor experto original MQL5 `Exp_XRSI_Histogram_Vol`. Negocia rupturas en el histograma RSI ponderado por volumen interpretando los cinco estados de color producidos por el indicador. El script se ejecuta en cualquier marco temporal proporcionado a través de la suscripción de velas y está construido sobre la API de estrategia de alto nivel de StockSharp.

## Lógica de la estrategia

1. Calcular un RSI en el marco temporal seleccionado y restar 50 para centrar el oscilador.
2. Multiplicar el valor RSI centrado por el flujo de volumen elegido (ticks o volumen real) para enfatizar las velas con fuerte actividad.
3. Suavizar tanto el RSI ponderado como el volumen bruto usando el mismo método de media móvil y longitud.
4. Construir umbrales adaptativos multiplicando el volumen suavizado por cuatro multiplicadores definidos por el usuario. El histograma resultante se clasifica en los siguientes estados de color:
   - **0** – impulso alcista fuerte (por encima de `HighLevel2`).
   - **1** – impulso alcista moderado (entre `HighLevel1` y `HighLevel2`).
   - **2** – zona neutral.
   - **3** – impulso bajista moderado (entre `LowLevel2` y `LowLevel1`).
   - **4** – impulso bajista fuerte (por debajo de `LowLevel2`).
5. Las reglas de entrada y salida reflejan la lógica MQL:
   - Entrar en el primer largo cuando el histograma cambia al estado **1** después de estar por encima del estado **1** (el color disminuye de bajista/neutral a alcista moderado).
   - Entrar en el segundo largo cuando el histograma cambia al estado **0** después de estar por encima del estado **0**.
   - Entrar en el primer corto cuando el histograma cambia al estado **3** después de estar por debajo del estado **3**.
   - Entrar en el segundo corto cuando el histograma cambia al estado **4** después de estar por debajo del estado **4**.
   - Cerrar posiciones cortas cuando el histograma está en los estados **0** o **1**.
   - Cerrar posiciones largas cuando el histograma está en los estados **3** o **4**.
6. La generación de señales puede desplazarse hacia atrás por `SignalBar` barras para imitar la indexación de búfer del indicador original.

Se admiten dos entradas de escala para cada dirección a través de los multiplicadores `Mm1` y `Mm2`. Los métodos auxiliares aplanan una posición opuesta antes de abrir una nueva, replicando el comportamiento del código de gestión de operaciones heredado.

## Gestión de dinero y protección

- `Mm1` y `Mm2` son multiplicadores de volumen aplicados a la propiedad `Volume` de la estrategia (se usa un valor predeterminado de 1 cuando `Volume` no está establecido).
- El stop-loss y take-profit globales se activan a través de `StartProtection` cuando tanto el paso de precio como los valores de puntos correspondientes son positivos. Se interpretan como un número de pasos de precio.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco temporal utilizado para velas y cálculos de indicadores. |
| `RsiPeriod` | Longitud del RSI. |
| `VolumeMode` | Elegir entre volumen de ticks y volumen real. El modo de ticks recurre a una unidad cuando faltan datos de volumen. |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multiplicadores que escalan el volumen suavizado para construir umbrales del histograma. |
| `MaMethod`, `MaLength`, `MaPhase` | Ajustes de suavizado. Los métodos no compatibles (Parabolic, T3, Vidya, Ama) recurren a la media móvil simple. `MaPhase` se mantiene para completitud pero solo afecta a métodos avanzados como Jurik. |
| `SignalBar` | Cuántas barras cerradas hacia atrás deben evaluarse al leer el color del histograma. |
| `Mm1`, `Mm2` | Multiplicadores de volumen para la primera y segunda posición en cada dirección. |
| `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` | Habilitar o deshabilitar la lógica de apertura y cierre para largos/cortos. |
| `StopLossPoints`, `TakeProfitPoints` | Desplazamientos de protección expresados en pasos de precio. |

## Valores predeterminados

- Tipo de vela: marco temporal de 4 horas.
- Longitud RSI: 14.
- Modo de volumen: volumen de ticks.
- Umbrales del histograma: `HighLevel2 = 17`, `HighLevel1 = 5`, `LowLevel1 = -5`, `LowLevel2 = -17`.
- Media móvil: SMA con longitud 12 y fase 15.
- Desplazamiento de barra de señal: 1 barra.
- Gestión de dinero: `Mm1 = 0.1`, `Mm2 = 0.2`.
- Stops: stop loss 1000 puntos, take profit 2000 puntos (aplicados solo cuando hay un paso de precio válido disponible).

## Notas

- La estrategia se basa en velas terminadas e ignora las actualizaciones no terminadas.
- El suavizado Jurik es compatible a través del `JurikMovingAverage` de StockSharp. Otros métodos heredados (ParMA, T3, VIDYA, AMA) revierten a SMA debido a la falta de equivalentes nativos.
- El indicador usa el `TotalVolume` de la vela. Cuando el volumen es cero, el modo de ticks usa un peso de respaldo de uno para evitar suprimir señales.
- Para análisis visual, el RSI en sí se muestra junto con velas y marcadores de operaciones. Puede adjuntar elementos de gráfico adicionales si se requieren diagnósticos más profundos.
