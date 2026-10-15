# Estrategia de Vela de Centro de Gravedad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el experto MetaTrader "Exp_CenterOfGravityCandle" utilizando la API de alto nivel de StockSharp. El experto opera con velas sintéticas generadas por el indicador Center of Gravity Candle. Cada vela sintética se construye aplicando el cálculo del Centro de Gravedad de John Ehlers a los precios de apertura, máximo, mínimo y cierre, y luego suavizando los resultados con una media móvil configurable. El color de la vela sintética (alcista, bajista o neutral) es la única señal de trading.

## Lógica del indicador

1. Cada vela de mercado entrante se procesa después de que esté completamente cerrada.
2. Para cada componente de precio (apertura, máximo, mínimo, cierre) la estrategia calcula dos medias móviles: una MA simple y una MA ponderada linealmente con el período definido por **Period**.
3. El producto de estas dos medias se divide por el paso de precio del instrumento y se suaviza con el método configurado (**Ma Method**) y la longitud (**Smooth Period**).
4. Los máximos y mínimos sintéticos se fuerzan para incluir la apertura/cierre sintéticos de modo que los cuerpos de las velas sean consistentes con la implementación de MetaTrader.
5. El color de la vela se determina comparando la apertura y el cierre sintéticos: apertura por debajo del cierre = alcista (color 2), apertura por encima del cierre = bajista (color 0), de lo contrario neutral (color 1).

## Reglas de trading

1. La estrategia mantiene un historial de colores de velas sintéticas e inspecciona la barra definida por **Signal Bar** (predeterminado = barra finalizada anterior).
2. Cuando la vela sintética inspeccionada se vuelve alcista y la vela anterior no era alcista:
   - Cerrar cualquier posición corta existente si **Enable Sell Close** es `true`.
   - Abrir una nueva posición larga si **Enable Buy Open** es `true`.
3. Cuando la vela sintética inspeccionada se vuelve bajista y la vela anterior no era bajista:
   - Cerrar cualquier posición larga existente si **Enable Buy Close** es `true`.
   - Abrir una nueva posición corta si **Enable Sell Open** es `true`.
4. Las entradas de mercado utilizan el volumen calculado a partir de **Money Management** y **Margin Mode**. Los valores negativos para **Money Management** se tratan como un tamaño de lote fijo. Para los modos basados en pérdidas, el algoritmo aproxima el riesgo por operación usando la distancia de stop-loss configurada.
5. `StartProtection` se activa para colocar automáticamente órdenes de take-profit y stop-loss según las distancias **Take Profit** y **Stop Loss** expresadas en pasos de precio.

## Parámetros

- **Money Management** – fracción del valor de la cuenta utilizada para derivar el volumen de la orden (valores negativos = lote fijo). Optimizable.
- **Margin Mode** – interpretación del parámetro de gestión del dinero (basado en equity, basado en balance, basado en pérdida o lote fijo).
- **Stop Loss** – distancia del stop-loss en pasos de precio. Se usa tanto para órdenes de protección como para el dimensionamiento de posiciones basado en riesgo.
- **Take Profit** – distancia del take-profit en pasos de precio. Aplicado a través de `StartProtection`.
- **Open Long / Open Short** – permitir abrir posiciones largas/cortas en sus respectivas señales.
- **Close Long / Close Short** – permitir cerrar posiciones largas/cortas cuando aparece la señal opuesta.
- **Candle Type** – marco temporal de las velas utilizadas para el cálculo del indicador.
- **Center of Gravity Period** – período base para las medias móviles simple y ponderada linealmente. Optimizable.
- **Smoothing Period** – longitud de la media móvil de suavizado aplicada a las velas sintéticas. Optimizable.
- **Smoothing Method** – tipo de media móvil usado en la etapa de suavizado (SMA, EMA, SMMA o LWMA).
- **Signal Bar** – índice de la vela sintética usada para generar señales (0 = actual, 1 = anterior, etc.).

## Notas

- El cálculo del indicador está implementado en C# para reproducir la lógica original de MetaTrader, evitando buffers manuales o colecciones históricas.
- El cálculo del volumen utiliza información del portafolio de StockSharp y puede diferir ligeramente de los resultados de MetaTrader debido a diferencias de plataforma.
- La estrategia opera completamente sobre velas terminadas y nunca opera en barras parciales.
