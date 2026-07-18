# Estrategia Triple SMA Spread
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
Esta estrategia es un port en C# del asesor experto de MetaTrader 5 `3sma.mq5` (id 21495). Sigue la misma idea de tradear cuando tres medias móviles simples se separan entre sí por un spread configurable. La implementación utiliza la API de alto nivel de StockSharp con suscripciones de velas y vinculación de indicadores para que no se requiera ninguna gestión manual de series.

## Comportamiento original de MT5
El experto MT5 se basa en tres medias móviles simples con diferentes períodos y desplazamientos de visualización. La media rápida usa la barra actual, mientras que las medias media y lenta están desplazadas uno y dos barras hacia el pasado. En cada tick:

1. Convierte el spread definido por el usuario de pips a unidades de precio basándose en la precisión del símbolo.
2. Cierra posiciones largas cuando la SMA rápida cae por debajo de la SMA media al menos la mitad del spread, y cierra posiciones cortas cuando la SMA rápida sube por encima de la SMA media en la mitad del spread.
3. Abre nuevas posiciones largas si `MA1 > MA2 + spread` y `MA2 > MA3 + spread` mientras no queden otros trades largos del experto. Análogamente, abre posiciones cortas cuando las tres medias están alineadas en el orden opuesto.
4. Utiliza solo órdenes de mercado con un tamaño de lote fijo y no aplica niveles explícitos de stop-loss o take-profit.

## Implementación en StockSharp
* Indicadores – tres instancias de `SimpleMovingAverage` se suscriben a la misma fuente de velas. Los buffers de historial compactos reproducen los parámetros de "shift" de MT5 para que cada comparación use valores de barras terminadas con los desplazamientos solicitados.
* Manejo del spread – el parámetro de spread se ingresa en pips. La estrategia deriva un tamaño de pip de `Security.PriceStep` (o `Security.Step`) y lo multiplica por diez para símbolos FX de 3/5 dígitos, coincidiendo con el ajuste de MT5 para cotizaciones fraccionales.
* Flujo de órdenes – las órdenes se envían con `BuyMarket`/`SellMarket`. Cuando aparece una condición de reversión, la estrategia suma el valor absoluto de la posición neta actual al volumen base para aplanar la exposición opuesta y establecer la nueva dirección con una única orden de mercado.
* Visualización – si hay gráficos disponibles, la estrategia traza las velas fuente junto con las tres medias móviles y los trades ejecutados.

## Parámetros
| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `Volume` | Volumen de orden usado para cada entrada de mercado. | `0.1` |
| `FastMaPeriod` | Período de la SMA rápida (equivalente a MA1 en MT5). | `9` |
| `FastMaShift` | Número de barras terminadas usadas para desplazar la SMA rápida. | `0` |
| `MiddleMaPeriod` | Período de la SMA media (MA2). | `14` |
| `MiddleMaShift` | Desplazamiento en barras terminadas para la SMA media. | `1` |
| `SlowMaPeriod` | Período de la SMA lenta (MA3). | `29` |
| `SlowMaShift` | Desplazamiento en barras terminadas para la SMA lenta. | `2` |
| `MaSpreadPips` | Spread mínimo requerido entre SMAs consecutivas medido en pips. | `10` |
| `CandleType` | Serie de velas usada para cálculos. | Marco temporal de `1 minuto` |

## Lógica de trading
1. Esperar a que las tres medias móviles estén formadas y que los buffers de historial contengan valores para los desplazamientos solicitados.
2. Convertir el parámetro de spread de pips a unidades de precio y calcular el medio-spread para filtros de salida.
3. **Filtros de salida** –
   * Cerrar exposición larga si la SMA rápida desplazada cae por debajo de la SMA media desplazada en al menos la mitad del spread.
   * Cerrar exposición corta si la SMA rápida desplazada sube por encima de la SMA media desplazada en al menos la mitad del spread.
4. **Condiciones de entrada** –
   * Entrar largo (o revertir de corto a largo) cuando la SMA rápida es mayor que la SMA media más el spread **y** la SMA media es mayor que la SMA lenta más el spread.
   * Entrar corto (o revertir de largo a corto) cuando la SMA rápida es menor que la SMA media menos el spread **y** la SMA media es menor que la SMA lenta menos el spread.

## Diferencias con la versión MT5
* StockSharp trabaja con una única posición neta por instrumento. Cuando aparece una señal de reversión la estrategia emite una única orden de mercado dimensionada para aplanar la exposición neta anterior y establecer la nueva. El experto MT5 podía mantener posiciones largas y cortas independientes.
* La conversión de pips usa los mejores metadatos de `Security` disponibles. Si el broker no proporciona ni `PriceStep` ni `Step`, se usa un valor de `1` como respaldo.
* Las órdenes se envían en velas terminadas en lugar de cada tick porque la API de alto nivel opera en suscripciones de velas.
* La estrategia no implementa los helpers de registro verboso del código MT5; se puede usar el registro integrado de StockSharp si es necesario.

## Notas de uso
* Asegurarse de que la serie de velas seleccionada coincida con el marco temporal usado en la configuración original de MT5.
* Ajustar el parámetro de spread cuando el instrumento use tamaños de pip no estándar.
* Dado que la estrategia trabaja con velas terminadas, la ejecución se retrasará hasta que la vela actual cierre.
