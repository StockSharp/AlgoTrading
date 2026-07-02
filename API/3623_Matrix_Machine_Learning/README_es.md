# Estrategia de aprendizaje automático matricial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Matrix Machine Learning es un enfoque basado en redes neuronales publicado originalmente en MetaTrader 5 dentro del proyecto educativo "MQL5Book". El script experto recopila una ventana de precios de ticks, convierte diferencias de precios consecutivas en una secuencia binaria y entrena una red neuronal recurrente de Hopfield. La red entrenada se evalúa en un segmento dentro de la muestra, se valida en un segmento fuera de la muestra y finalmente se utiliza para inferir la dirección de los siguientes movimientos. Las posiciones se abren cuando el primer elemento del vector binario previsto muestra una dirección alcista (`+1`) o bajista (`-1`).

Esta versión de C# traslada la lógica original al API de alto nivel de StockSharp y reemplaza el procesamiento de ticks con velas terminadas para garantizar un comportamiento multiplataforma estable. Cada cierre de vela actualiza el patrón de precios binario, vuelve a entrenar la red Hopfield, evalúa la precisión histórica y produce un pronóstico en línea para los próximos pasos.

## Detalles del algoritmo
1. Recoge los últimos cierres de velas `HistoryDepth`. Los puntos `ForwardDepth` más recientes forman el conjunto fuera de muestra, mientras que los valores restantes crean el segmento de entrenamiento.
2. Convierta diferencias consecutivas cercanas a cercanas en una secuencia binaria: los deltas positivos o cero se convierten en `+1`, los deltas negativos se convierten en `-1`.
3. Entrene una matriz de ponderación de Hopfield sumando los productos externos de cada par de predictor/salida donde la longitud del predictor es igual a `PredictorLength` y la longitud de la respuesta es igual a `ForecastLength`.
4. Evalúe la matriz entrenada en las series de entrenamiento y avance. La métrica de precisión coincide con el guión original: el producto escalar entre los vectores de respuesta previstos y reales se promedia y se reescala a un porcentaje.
5. Cree el último patrón binario en línea y ejecute el bucle de inferencia de Hopfield (activación tanh con un umbral de convergencia). El primer componente de pronóstico impulsa la decisión comercial.

## Parámetros
- **Profundidad del historial**: número de cierres de velas recientes almacenados para la red Hopfield. Debe ser mayor que `ForwardDepth` y al menos `PredictorLength + ForecastLength + 1`.
- **Profundidad de avance**: tamaño de la ventana de validación reservada para verificaciones de avance. Requiere al menos `ForecastLength + 1` cierres.
- **Longitud del predictor**: longitud del vector de entrada binaria utilizado por la red neuronal.
- **Duración del pronóstico**: número de pasos futuros predichos por el vector de salida de la red.
- **Tipo de vela**: StockSharp `DataType` que describe la serie de velas solicitada desde el conector.
- **Registro de depuración**: cuando está habilitado, imprime vectores intermedios detallados, comparaciones de muestras y pronósticos en línea.

## Lógica de trading
- Si el primer elemento del pronóstico de Hopfield es positivo y la estrategia es plana o corta, se envía una orden de compra de mercado para que `Volume + |Position|` pase a una posición larga.
- Si el primer elemento es negativo y la estrategia es plana o larga, se envía una orden de venta de mercado para que `Volume + |Position|` pase a una posición corta.
- Se ignoran los pronósticos cero para evitar una rotación innecesaria.

La estrategia traza automáticamente velas y operaciones propias cuando hay un área del gráfico disponible. La red Hopfield se vuelve a entrenar en cada vela terminada para mantener los pesos neuronales sincronizados con la estructura de mercado más reciente.
