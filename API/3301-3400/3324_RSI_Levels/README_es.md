# Estrategia RSI Levels
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general

La **estrategia RSI Levels** es un port directo del asesor experto de MetaTrader 5 "RSI Levels". El sistema observa un solo símbolo en el marco seleccionado y actúa cuando el Relative Strength Index (RSI) cruza umbrales configurables de sobrecompra y sobreventa. La estrategia asume que el mercado revertirá a la media después de que el RSI entre en una zona extrema. Cuando el indicador cae por debajo del nivel de sobreventa se inicia una posición larga, y cuando sube por encima del nivel de sobrecompra se abre una corta. Solo se mantiene una posición a la vez y cualquier exposición opuesta se cierra antes de una nueva entrada.

## Lógica de negociación

1. **Cálculo RSI:** el RSI se calcula en el marco de trabajo con un periodo configurable. La barra actual debe estar terminada antes de evaluar señales.
2. **Entrada larga:** se dispara cuando el RSI actual cierra por debajo del nivel de sobreventa mientras el RSI previo estaba por encima. Si existe una posición corta, se cierra de inmediato; si no, se abre un nuevo largo usando dimensionamiento basado en riesgo.
3. **Entrada corta:** se dispara cuando el RSI actual cierra por encima del nivel de sobrecompra mientras el RSI previo estaba por debajo. Cualquier exposición larga se cierra primero y luego se crea una operación corta.
4. **Stop Loss:** se coloca un stop fijo a una distancia configurable en puntos del símbolo desde el precio de entrada. Si el stop se establece en cero, queda desactivado.
5. **Take Profit:** se coloca un take-profit fijo a una distancia configurable en puntos del símbolo desde el precio de entrada. Si el take-profit es cero, queda desactivado.
6. **Gestión de posición:** solo puede haber una posición abierta a la vez. Después de cerrar una posición, el estado interno se reinicia para que la siguiente señal empiece desde cero.

## Dimensionamiento de posición

El tamaño de posición se calcula desde el *Risk % per Trade* configurado. El algoritmo multiplica el patrimonio de la cartera por el porcentaje de riesgo y luego divide el capital en riesgo por el valor monetario de la distancia de stop (puntos de stop x precio de paso). El volumen resultante se redondea hacia abajo al paso de lote negociable más cercano y se limita por el volumen mínimo/máximo de la seguridad. Cuando falta información de mercado necesaria (price step o step price), la estrategia registra una advertencia y vuelve al volumen mínimo disponible.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | Marco de 1 hora | Marco usado para suscripción de velas y cálculo RSI. |
| `RsiPeriod` | 14 | Número de periodos del indicador RSI. |
| `OverboughtLevel` | 70 | Umbral RSI que define la zona de sobrecompra. |
| `OversoldLevel` | 30 | Umbral RSI que define la zona de sobreventa. |
| `RiskPercent` | 2 | Porcentaje del patrimonio de cartera arriesgado en cada operación. |
| `StopLossPoints` | 500 | Distancia de stop-loss expresada en puntos del símbolo. Cero desactiva. |
| `TakeProfitPoints` | 1000 | Distancia de take-profit expresada en puntos del símbolo. Cero desactiva. |

## Notas prácticas

- La estrategia requiere `PriceStep`, `StepPrice`, `MinVolume` y `VolumeStep` configurados en la seguridad para un dimensionamiento de riesgo preciso. Si falta cualquiera de estos valores, se usan valores predeterminados conservadores y se registran advertencias.
- La lógica usa `SubscribeCandles` y `Bind` para obtener valores de indicadores sin extraer datos manualmente, siguiendo las directrices de la API de alto nivel.
- Stops y objetivos se evalúan con datos de velas; slippage y gaps pueden causar ejecuciones lejos del precio previsto.
- Como el sistema reacciona solo cuando una vela termina, es adecuado para marcos como M15, H1 o H4. Marcos menores pueden requerir filtros adicionales para reducir ruido.

## Uso

1. Adjunte la estrategia al instrumento y cartera deseados.
2. Ajuste los umbrales RSI y controles de riesgo según la volatilidad del instrumento.
3. Inicie la estrategia y supervise el log para advertencias relacionadas con información faltante del símbolo.
4. Revise los resultados de operaciones y ajuste distancias de stop/take-profit o niveles RSI según el rendimiento.

Esta implementación StockSharp replica el comportamiento original de MetaTrader mientras expone configuración y gestión de riesgo mediante parámetros estándar de estrategia.
