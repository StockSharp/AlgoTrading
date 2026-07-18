# Estrategia Triple Top Triple Bottom
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **estrategia Triple Top Triple Bottom** es un port del asesor experto de MetaTrader con el mismo nombre. El sistema original combina varias capas de confirmación (dirección de tendencia, fuerza de momentum y filtro MACD) antes de entrar al mercado. Esta implementación StockSharp conserva las mismas ideas centrales y expone los umbrales importantes como parámetros de estrategia.

## Lógica principal

1. **Filtro de tendencia:** dos medias móviles ponderadas lineales (LWMA) calculadas sobre el precio típico (H+L+C)/3 definen la dirección de negociación. La LWMA rápida debe estar por encima de la lenta para permitir largos y por debajo para permitir cortos.
2. **Confirmación de momentum:** el indicador momentum integrado, con longitud de retrospección configurable, debe desviarse del nivel neutral 100 al menos por el umbral definido por el usuario dentro de las tres últimas velas completadas. El EA requería el mismo comportamiento al analizar valores previos de momentum, y esta validación se replica para evitar entradas en mercados planos.
3. **Filtro MACD:** un filtro clásico de línea de señal MACD 12/26/9 evita operar contra una tendencia fuerte. La estrategia solo compra cuando la línea MACD está por encima de la señal y vende cuando está por debajo.
4. **Gestión de riesgo:** las órdenes de mercado se protegen con objetivos de stop-loss y take-profit medidos en unidades absolutas de precio. Los parámetros son opcionales; ponerlos en cero desactiva la orden respectiva. El código también cierra la posición si el umbral de riesgo opuesto se alcanza durante el procesamiento de velas.

## Parámetros

- **Entry Candle:** `DataType` que define el marco temporal de las velas de trabajo.
- **Fast LWMA / Slow LWMA:** longitudes de los filtros de tendencia rápido y lento.
- **Momentum Period / Momentum Threshold:** retrospección del indicador momentum y desviación mínima desde 100 que confirma una idea de operación.
- **Stop Loss / Take Profit:** distancias protectoras en unidades absolutas de precio; también se envían como órdenes protectoras nativas mediante `SetStopLoss` y `SetTakeProfit` para que el control de riesgo se aplique incluso si la sesión de estrategia se detiene.

## Diferencias frente a la versión MQL

- Todos los extras de gestión monetaria (multiplicadores de lote, protección de patrimonio, trailing por vela, break-even y comprobaciones manuales de líneas de tendencia) se omitieron porque la API de alto nivel de StockSharp ya ofrece utilidades de dimensionamiento de posición y porque los objetos gráficos usados en el EA original son específicos de MetaTrader.
- Los umbrales de riesgo se expresan en unidades absolutas de precio en lugar de pips. Esto mantiene la implementación neutral respecto al broker; los usuarios pueden convertir fácilmente su distancia de pip preferida multiplicando el tamaño de pip del broker por el número deseado de pips.
- La salida gráfica usa áreas de StockSharp para velas de precio, medias móviles, momentum e indicadores MACD.

## Notas de uso

1. Adjunte la estrategia a un instrumento y configure el tipo de vela deseado antes de iniciar.
2. Ajuste el umbral de momentum y las distancias de stop según la volatilidad del instrumento.
3. La estrategia opera una sola posición neta. Cuando aparece una señal opuesta, la exposición actual se cierra primero, evitando operaciones superpuestas.

El código está completamente comentado en inglés y sigue las directrices de la API StockSharp de alto nivel proporcionadas en el repositorio.
