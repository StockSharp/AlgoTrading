# Estrategia PowerHouse SwiftEdge AI v2.10
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura basada en pivotes con filtro de momentum y gestión de riesgo simple.
La entrada larga ocurre cuando el precio rompe el máximo de pivote reciente con momentum suficiente y distancia mínima desde la señal anterior.
La entrada corta se activa en una ruptura del mínimo de pivote con momentum negativo.
Las posiciones usan take-profit y stop-loss especificados en puntos.

## Cálculo y ejecución

Es un ejemplo determinista; el título histórico no implica un modelo de IA entrenado. C# y Python comparten esta interpretación explícita de la descripción anterior.

Use velas base terminadas. Un máximo pivote supera estrictamente los máximos de `PivotPeriod` barras vecinas a cada lado; un mínimo es estrictamente inferior a sus mínimos. La igualdad excluye el pivote. Se confirma solo tras terminar las barras de la derecha, sin señales retroactivas. Conserve por separado el último máximo y mínimo confirmados.

Largo requiere `Close anterior <= máximo pivote` y `Close actual > máximo pivote`; corto exige el cruce inverso del mínimo pivote. Momentum es `Close actual - Close de hace MomentumPeriod barras`: estrictamente mayor que `MomentumThreshold` para largo o menor que su negativo para corto. Ambos lados requieren al menos `MinSignalBars` barras terminadas desde la última entrada aceptada; cero desactiva el intervalo. No se aplazan señales rechazadas. No hay filtro EMA ni espera de seis horas.

Entre con `Volume` desde cero o invierta completamente con `Volume + abs(Position)`. Una orden pendiente bloquea nuevas órdenes. Las distancias son unidades absolutas del precio cotizado: un punto equivale a una diferencia de 1, no a un tick, múltiplo de `PriceStep` o porcentaje. Ajústelas al instrumento. La base es la ejecución real, ponderada por volumen al aumentar parcialmente el mismo lado; reducir conserva la base e invertir la reinicia.

La protección local cierra toda la posición a mercado cuando el bid de largos o ask de cortos alcanza stop o take. Los cierres de velas también verifican protección; tocar el máximo/mínimo no simula eventos intrabar ordenados. La protección ignora el intervalo de entrada y tiene prioridad sobre una nueva entrada en la misma vela. Ante saltos, la ejecución puede diferir del disparador.

Valores iniciales: `PivotPeriod=2`, `MomentumPeriod=10`, `MomentumThreshold=0`, `MinSignalBars=5`, `StopLossPoints=50`, `TakeProfitPoints=100`, `CandleType=TimeSpan.FromMinutes(5).TimeFrame()`. Períodos y distancias de salida positivos; umbral e intervalo no negativos.
