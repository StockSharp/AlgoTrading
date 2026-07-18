# PSAR Estrategia de múltiples plazos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica al asesor experto MetaTrader **EA_PSar_002B**. Evalúa los valores Parabolic SAR en tres períodos de tiempo (M15, M30 y H1) mientras gestiona posiciones en una secuencia de un minuto. El comercio es direccional: solo puede haber una posición neta activa a la vez y las nuevas operaciones aparecen solo cuando la exposición anterior es plana. El experto original fue diseñado para EURUSD en el gráfico M1 y el puerto mantiene el mismo contexto.

## Lógica comercial
1. **Parabolic SAR filtro de convergencia**: los últimos valores SAR de M15, M30 y H1 deben estar dentro de 19 pasos de precio mínimo entre sí. Esto mantiene las tres curvas "apretadas" antes de que se permita una ruptura.
2. **Entrada larga**: debe ocurrir una de las siguientes secuencias:
   - Los valores de M15, M30 y H1 SAR están por debajo de sus respectivos mínimos actuales, el H1 anterior SAR estuvo por encima del máximo del H1 anterior y el nuevo H1 SAR cae por debajo del mínimo del H1 actual.
   - M15 y H1 SAR están por debajo de sus mínimos actuales, mientras que el M30 anterior SAR estaba por encima del máximo anterior del M30 y el nuevo M30 SAR cae por debajo del mínimo actual del M30.
   - M30 y H1 SAR están por debajo de sus mínimos actuales, mientras que el M15 anterior SAR estaba por encima del máximo anterior de M15 y el nuevo M15 SAR cae por debajo del mínimo actual de M15.
3. **Entrada corta**: refleja las condiciones de la configuración larga con máximos y mínimos invertidos.
4. **Take Profit / Stop Loss** – los límites se expresan en puntos (incrementos mínimos de precio). Por defecto, el objetivo equivale a 999 puntos y el stop de protección equivale a 399 puntos, que corresponden a los valores MQL después de normalizar cotizaciones de 4/5 dígitos.
5. **Salida dinámica**: mientras una posición está abierta, se monitorea la M30 SAR.
   - Los largos cierran cuando el SAR anterior estaba por debajo del mínimo M1 anterior pero el SAR actual salta por encima del máximo M1 actual.
   - Los cortos se cierran cuando el SAR anterior estaba por encima del máximo anterior de M1, pero el SAR actual cae por debajo del mínimo actual de M1.
   - Cuando el M30 actual SAR cruza más allá del precio de entrada, el stop se arrastra hasta ese nivel SAR.

## gestión del dinero
`UseMoneyManagement` reproduce el interruptor de administración de dinero del EA. Cuando está deshabilitado, se utiliza el parámetro `FixedVolume`. Cuando está habilitado, el porcentaje solicitado del capital de la cartera se convierte a un tamaño de "lote" sintético utilizando la misma fórmula que la versión MQL (porcentaje del capital libre dividido por 100.000). El importe se alinea con `Security.VolumeStep` y se recorta según los límites del corredor (`VolumeMin`/`VolumeMax`).

## Parámetros
- `BaseCandleType`: período de tiempo utilizado para la gestión comercial (el valor predeterminado es M1).
- `FastSarCandleType`, `MediumSarCandleType`, `SlowSarCandleType`: plazos para los filtros SAR (predeterminados: 15 m, 30 m, 60 m).
- `EnableParabolicFilter` – refleja la bandera `sar2` de MQL; apagarlo deja de operar por completo.
- `TakeProfitPoints`, `StopLossPoints` – compensaciones en puntos (incrementos mínimos de precio). El tamaño del pip se deriva de `Security.PriceStep` y `Security.Decimals` para manejar correctamente las cotizaciones de divisas de 3/5 dígitos.
- `UseMoneyManagement`, `PercentMoneyManagement`, `FixedVolume`: controles de volumen descritos anteriormente.

## Notas de conversión
- Sólo se utiliza el StockSharp API de alto nivel. Todas las series de precios se suscriben a través de `SubscribeCandles().Bind(...)` y los datos del indicador se reciben a través de enlaces en lugar de buffers manuales.
- Las órdenes de protección se implementan mediante salidas explícitas del mercado, exactamente como el script original que llamaba `OrderClose`.
- El coeficiente de dígitos del corredor de MQL se reemplaza por la detección automática del tamaño del pip (`PriceStep` × 10 para instrumentos de 3/5 dígitos).
- El EA prohibió operar con símbolos que no sean EURUSD o gráficos que no sean M1 mediante la impresión de mensajes. En StockSharp los registros de estrategia permanecen silenciosos, pero el comportamiento está documentado aquí.

## Consejos de uso
1. Adjunte la estrategia al EURUSD con velas de un minuto para la suscripción base. Los plazos de los indicadores aún se pueden cambiar si se desea experimentar.
2. Asegúrese de que los metadatos de seguridad expongan `PriceStep`/`Decimals`. Sin ellos, las distancias de parada y objetivo vuelven a ser un tamaño unitario de 1.
3. Mantenga `EnableParabolicFilter` habilitado; es equivalente al interruptor maestro del EA. Deshabilítelo sólo cuando desee intencionalmente que la estrategia permanezca inactiva.
