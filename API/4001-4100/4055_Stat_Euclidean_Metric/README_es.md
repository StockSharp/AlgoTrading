# Estrategia métrica euclidiana estadística
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia reproduce el comportamiento del MetaTrader asesor experto `Stat_Euclidean_Metric.mq4`. Supervisa las reversiones de MACD en un solo instrumento y período de tiempo. Cuando la línea MACD forma un punto de inflexión local, la estrategia abre una posición inmediatamente (modo de entrenamiento) o valida la configuración con un clasificador de k vecinos más cercanos (k-NN) que compara la estructura actual del mercado con vectores de características históricas almacenados en archivos binarios.

## Lógica comercial
1. Suscríbase al tipo de vela configurado y calcule el indicador MACD sobre el precio típico ((Máximo + Mínimo + Cierre) / 3).
2. Detecte una reversión bajista cuando los últimos tres valores completos de MACD cumplan con `MACD[2] <= MACD[1]` y `MACD[1] > MACD[0]`.
3. Detectar una reversión alcista cuando `MACD[2] >= MACD[1]` y `MACD[1] < MACD[0]`.
4. Dependiendo del modo seleccionado:
   - **Modo de entrenamiento (`TrainingMode = true`)**: abra una orden de mercado en la dirección de la reversión después de cerrar opcionalmente la posición actual. Esto imita el comportamiento original de EA cuando recopila nuevas muestras.
   - **Modo clasificador (`TrainingMode = false`)**: calcula cinco proporciones de promedios móviles simples del precio típico y evalúa la probabilidad de éxito con un modelo k-NN. Realice pedidos solo si la probabilidad cruza los umbrales configurados.
5. Aplique el módulo integrado `StartProtection` para adjuntar niveles de stop-loss y take-profit en pasos del instrumento.

## Vector de características para clasificación
El modelo k-NN utiliza los siguientes ratios calculados sobre la vela recién cerrada:
- SMA(89) / SMA(144)
- SMA(144) / SMA(233)
- SMA(21) / SMA(89)
- SMA(55) / SMA(89)
- SMA(2) / SMA(55)

Cada muestra almacenada en los archivos del conjunto de datos contiene seis valores `double`: las cinco proporciones anteriores y una etiqueta (`0` para un resultado desfavorable, `1` para una operación exitosa). Durante la evaluación, la estrategia selecciona las muestras `NeighborCount` más cercanas, promedia sus etiquetas e interpreta el resultado como la probabilidad de éxito.

## Archivos de conjunto de datos
- `BuyDatasetPath`: ruta al archivo binario con vectores recopilados después de operaciones alcistas.
- `SellDatasetPath`: ruta al archivo binario con vectores recopilados después de operaciones bajistas.

Si una ruta es relativa, se resolverá con `Environment.CurrentDirectory`. Los archivos faltantes se informan en el registro y se tratan como un conjunto de datos vacío. Esta implementación lee conjuntos de datos pero no actualiza ni agrega nuevas muestras automáticamente; La exportación de nuevos vectores debe manejarse externamente cuando se ejecuta en modo de entrenamiento.

## Parámetros
- **TrainingMode**: cambie entre el comercio MACD puro y el comercio asistido por clasificador.
- **BuyThreshold / SellThreshold**: probabilidad mínima devuelta por el clasificador para abrir operaciones en la dirección principal.
- **AllowInverseEntries**: permite operaciones contrarias cuando la probabilidad es extremadamente baja.
- **InverseBuyThreshold / InverseSellThreshold**: probabilidad máxima que aún activa una operación en dirección opuesta.
- **FastLength / SlowLength / SignalLength** – MACD EMA longitudes.
- **TakeProfitPoints / StopLossPoints**: niveles de protección expresados en pasos del instrumento.
- **ClosePositionsOnSignal**: cierra la posición neta actual antes de enviar una nueva orden.
- **BuyDatasetPath / SellDatasetPath**: archivos binarios que almacenan vectores históricos.
- **NeighborCount**: número de vecinos utilizados en la votación k-NN.
- **CandleType** – serie de velas utilizadas para todos los indicadores.

## Recomendaciones de uso
- Proporcione rutas absolutas o relativas al directorio de trabajo a los archivos del conjunto de datos antes de habilitar el modo clasificador.
- Recopile muestras de alta calidad ejecutando la estrategia en modo de entrenamiento con datos históricos y exportando vectores manualmente.
- Optimice los umbrales y el recuento de vecinos para adaptar el clasificador a nuevos mercados o instrumentos.
- Mantenga el parámetro `Volume` del instrumento alineado con el modelo de riesgo porque la estrategia siempre abre `Volume + |Position|` lotes para revertir la posición neta cuando sea necesario.

## Diferencias con la versión MQL4
- Los conjuntos de datos del clasificador solo se leen; el EA original escribe nuevas muestras durante la desinicialización. Aquí el usuario debe actualizar los archivos manualmente después de analizar el historial comercial.
- Todas las órdenes de protección se adjuntan mediante parámetros StockSharp `StartProtection` en lugar de parámetros manuales `OrderSend`.
- El cierre de órdenes en el modo clasificador siempre sale de la posición completa cuando `ClosePositionsOnSignal` está habilitado, mientras que el script MQL4 cierra solo las órdenes rentables antes de recibir nuevas señales.
