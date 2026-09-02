# Diagrama de la estrategia MACD con la banda media de Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos indicadores muy comunes se reparten el trabajo: el MACD decide de qué lado del mercado estar y la banda media de Bollinger indica cuándo el precio se ha alejado lo suficiente del valor justo como para tomar ese lado barato. Las bandas exteriores no se usan a propósito: la estrategia original compra retrocesos por debajo de la línea media, no rupturas del canal.

![schema](schema.svg)

## Resumen de la estrategia

- El único filtro de tendencia es la línea MACD frente a su señal: por encima solo largos, igual o por debajo solo cortos.
- El precio de entrada debe estar a una décima de punto porcentual de la banda media, del lado contrario a la tendencia: en subida se compran las caídas, en bajada se venden los repuntes.
- El margen se expresa como fracción del valor de la banda y no en puntos fijos, así que el mismo diagrama sirve para cualquier instrumento.
- Las salidas no esperan al precio en absoluto: en cuanto las dos líneas del MACD se intercambian, la posición se cierra.

## Reglas de entrada y salida

- **Entrada en largo**: La línea MACD está por encima de su señal, la vela cierra por debajo de la banda media menos el margen y la posición no es larga. La orden compra un lote: abre un largo desde plano o cubre un corto.
- **Entrada en corto**: La línea MACD está igual o por debajo de su señal, la vela cierra por encima de la banda media más el margen y la posición no es corta. La orden vende un lote: abre un corto desde plano o cierra un largo.
- **Salida**: El largo se cierra en cuanto la línea MACD cae a su señal o por debajo, y el corto en cuanto sube por encima; ambos bloques están en modo de cierre de posición, así que solo actúan si hay posición.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| MACD fast period | 12 | Longitud de la media rápida dentro del MACD. |
| MACD slow period | 26 | Longitud de la media lenta dentro del MACD. |
| MACD signal period | 9 | Longitud de la línea de señal del MACD. |
| Bollinger period | 20 | Periodo de suavizado de BollingerBands; solo se lee su línea media. |
| Bollinger width | 2.0 | Multiplicador de desviación estándar de BollingerBands; no afecta a las reglas, porque las bandas exteriores no se usan. |
| Middle band gap | 0.001 | Distancia a la que debe llegar el precio de entrada respecto a la banda media, como fracción de su valor. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Un bloque de velas alimenta el MACD, las BollingerBands y un conversor del cierre; otros tres conversores extraen la línea MACD, la de señal y la banda media de los valores de indicador.
- Una sola constante de margen y dos bloques de fórmula convierten la banda media en un nivel de compra y otro de venta, de modo que un parámetro expuesto mueve ambos umbrales a la vez.
- Cada entrada es una Y lógica de tres señales: la comparación del MACD, la de la banda y la posición contrastada con una constante cero.
- Los dos bloques de salida cuelgan directamente de las comparaciones del MACD y están en modo de cierre; los cuatro bloques de órdenes toman su tamaño de la misma constante de volumen.
- Simplificaciones deliberadas: el original también suscribe un AverageTrueRange que nunca utiliza, así que no se dibuja bloque de ATR, y bloquea las entradas durante 100 barras tras cada operación, algo que ningún bloque expresa: este diagrama vuelve a entrar en cuanto se repiten las condiciones.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
