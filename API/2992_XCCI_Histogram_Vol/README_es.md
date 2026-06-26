# Estrategia de Histograma Vol XCCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port de StockSharp del asesor experto de MetaTrader `Exp_XCCI_Histogram_Vol`. Reproduce la lógica codificada por colores del indicador personalizado "XCCI Histogram Vol": un Commodity Channel Index (CCI) multiplicado por el volumen, suavizado por una media móvil seleccionable y comparado con umbrales dinámicos. La implementación sigue las directrices de la API de alto nivel, procesa solo velas cerradas y mantiene la estructura de posición dual original exponiendo volúmenes separados para las entradas primaria y secundaria.

## Flujo de trabajo del indicador
1. Calcular el valor CCI con el período configurable.
2. Multiplicar el valor CCI por el volumen de la vela.
3. Suavizar tanto la serie CCI×Volumen como el volumen bruto con la media móvil elegida (`Simple`, `Exponential`, `Smoothed`, `Weighted`, `Hull`, o `VolumeWeighted`).
4. Escalar cuatro multiplicadores de umbral definidos por el usuario (HighLevel2/1 y LowLevel1/2) por el volumen suavizado.
5. Clasificar el valor suavizado de CCI×Volumen en una de cinco zonas: `0` extremadamente alcista, `1` alcista, `2` neutral, `3` bajista, `4` extremadamente bajista.

La estrategia almacena la zona para cada vela terminada. El parámetro `SignalBarOffset` controla cuántas velas completamente cerradas esperar antes de usar la zona en las decisiones de trading (reflejando la entrada original `SignalBar`).

## Reglas de trading
- **Salidas largas**: si la zona evaluada es `3` o `4`, se cierra cada posición larga abierta.
- **Salidas cortas**: si la zona evaluada es `1` o `0`, se cierra cada posición corta abierta.
- **Entrada larga primaria**: se activa cuando la zona actual se convierte en `1` y la zona anterior (vela más antigua) estaba por encima de `1`. Esto refleja la transición de territorio neutral/bajista a la banda alcista. El volumen de la orden es `PrimaryEntryVolume` y cierra cualquier exposición corta existente antes de invertirse.
- **Entrada larga secundaria**: se activa cuando la zona actual se convierte en `0` y la zona anterior estaba por encima de `0`. Esto representa un aumento en la región extremadamente alcista y utiliza `SecondaryEntryVolume`.
- **Entrada corta primaria**: se activa cuando la zona actual se convierte en `3` y la zona anterior estaba por debajo de `3`, indicando un movimiento nuevo hacia territorio bajista. Usa `PrimaryEntryVolume` y cierra largos primero si es necesario.
- **Entrada corta secundaria**: se activa cuando la zona actual se convierte en `4` y la zona anterior estaba por debajo de `4`, señalando una aceleración bajista extrema. Usa `SecondaryEntryVolume`.

Los indicadores de entrada se restablecen cada vez que la posición neta cruza cero para que el comportamiento coincida con el diseño de "dos números mágicos" de MetaTrader: solo se permite una orden por nivel hasta que la señal opuesta o el módulo de riesgo cierre el trade.

## Gestión de riesgos
- `UseStopLoss` / `UseTakeProfit` habilitan distancias de protección absolutas (expresadas en puntos de precio) mediante el ayudante integrado `StartProtection`. Los stops son opcionales, igual que en el código original.
- La estrategia utiliza órdenes de mercado para cada acción y por tanto respeta el manejo de slippage configurado globalmente en StockSharp.
- Las llamadas de registro describen cada entrada y salida, lo que facilita auditar por qué se ejecutó un trade.

## Parámetros
- **CciPeriod** – longitud del Commodity Channel Index.
- **MaLength** – longitud aplicada a ambas medias móviles de suavizado.
- **HighLevel2 / HighLevel1 / LowLevel1 / LowLevel2** – multiplicadores aplicados al volumen suavizado para crear umbrales adaptativos.
- **SignalBarOffset** – número de velas cerradas a esperar antes de actuar en una zona (0 = última vela cerrada, 1 = vela anterior, etc.).
- **Smoothing** – tipo de media móvil utilizado para el suavizado (subconjunto de las opciones originales: SMA, EMA, SMMA, WMA, Hull MA, VWMA).
- **AllowLongEntries / AllowShortEntries / AllowLongExits / AllowShortExits** – habilitar o deshabilitar cada lado de forma independiente.
- **PrimaryEntryVolume / SecondaryEntryVolume** – volúmenes para los dos niveles de entrada (utilizados tanto para trades largos como cortos).
- **UseStopLoss / StopLossPoints** – stop-loss absoluto opcional.
- **UseTakeProfit / TakeProfitPoints** – take-profit absoluto opcional.
- **CandleType** – marco temporal (o cualquier otro tipo de datos de velas) solicitado al conector.

## Diferencias con la versión de MetaTrader
- Solo se exponen los métodos de suavizado que existen en StockSharp; filtros exóticos como JJMA, JurX, Parabolic MA, VIDYA y AMA no están incluidos. Elige la alternativa disponible más cercana si necesitas un comportamiento similar.
- El volumen de la vela se toma de `ICandleMessage.TotalVolume`. El volumen de ticks no se emula. Si el conector subyacente proporciona solo conteos de transacciones, el resultado diferirá del terminal original.
- La gestión de órdenes es neteada (posición única) en lugar de dos números mágicos independientes. Indicadores de entrada primaria/secundaria separados emulan la misma intención mientras permanecen compatibles con el modelo de ejecución de StockSharp.
