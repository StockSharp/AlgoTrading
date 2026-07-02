# Estrategia MACD No Sample
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción
MACD No Sample es un port del asesor experto de MetaTrader 5 `MACD No Sample`. La estrategia combina una verificación de pendiente de media móvil con cruces de línea de señal MACD mientras aplica una amplitud mínima de MACD expresada en pips. Cuando se confirma una configuración alcista, la exposición corta existente se cierra antes de entrar en largo; las configuraciones bajistas hacen lo contrario. La gestión de riesgo refleja el EA original con lógica de stop-loss, take-profit y trailing basados en pips, más un modo opcional de dimensionamiento de posición por porcentaje de riesgo.

## Lógica de la estrategia
### Preparación del indicador
* **Filtro de media móvil** – una media móvil configurable (SMA, EMA, SMMA o LWMA) aplicada a un precio de vela seleccionable (cierre, apertura, máximo, mínimo, mediano, típico o ponderado). La pendiente (`MA[0] > MA[1]` o `<`) define la dirección de tendencia.
* **Señal MACD** – el MACD se calcula a partir de longitudes de EMA rápida/lenta y longitud de señal independientes, usando el precio aplicado elegido. Las líneas MACD y señal brutas se monitorean para detectar cruces frescos y la magnitud absoluta del MACD se compara contra un umbral basado en pips.

### Reglas de entrada
* **Entradas largas**
  * La media móvil está subiendo en la última vela terminada.
  * El MACD está por debajo de cero pero acaba de cruzar por encima de la línea de señal (MACD actual > señal actual mientras MACD anterior < señal anterior).
  * El valor absoluto del MACD supera el umbral pip configurado (convertido a unidades de precio mediante el tamaño de pip detectado).
  * Las posiciones cortas existentes se cierran antes de colocar una orden larga.
* **Entradas cortas**
  * La media móvil está bajando en la última vela terminada.
  * El MACD está por encima de cero pero acaba de cruzar por debajo de la línea de señal (MACD actual < señal actual mientras MACD anterior > señal anterior).
  * El valor absoluto del MACD supera el umbral pip.
  * Las posiciones largas existentes se cierran antes de colocar una orden corta.

### Gestión de salida
* **Stop-loss / take-profit fijo** – distancias de pips opcionales convertidas a desplazamientos de precio desde el precio de entrada. Establecer cualquier parámetro en `0` desactiva el nivel correspondiente.
* **Trailing stop** – se activa cuando la distancia del trailing stop es positiva. La estrategia rastrea el mejor precio alcanzado desde la entrada, desplazando el stop al menos la distancia del paso de trailing (ambos expresados en pips) sin aflojarlo nunca.
* **Dimensionamiento basado en riesgo (opcional)** – cuando está habilitado, el volumen de la orden se deriva del valor del portafolio, la distancia del stop-loss y el porcentaje de riesgo configurado. Los volúmenes se alinean al `VolumeStep` del instrumento y se restringen por `MinVolume`/`MaxVolume` cuando están disponibles.

## Notas de implementación
* Usa la API de alto nivel a través de `SubscribeCandles()` con una tubería de indicadores manual dentro del callback `ProcessCandle`; no se usan llamadas `GetValue` de indicadores.
* Las entradas del indicador respetan las selecciones de precio aplicado y dependen de las implementaciones de media móvil y MACD de StockSharp.
* La detección del tamaño de pip refleja la lógica original del EA multiplicando el paso de precio por diez en instrumentos de tres y cinco dígitos.
* La lógica de stop y trailing cierra la posición mediante órdenes de mercado cuando se superan los niveles calculados; no se registran órdenes de stop separadas.
* Solo se proporciona la implementación en C#; no hay versión en Python para esta estrategia.

## Parámetros
* **Volume** – volumen de operación fijo para órdenes de mercado.
* **Stop Loss (pips)** – distancia de stop protector; `0` lo desactiva.
* **Take Profit (pips)** – distancia de objetivo de beneficio; `0` lo desactiva.
* **Trailing Stop (pips)** – distancia de trailing; `0` desactiva el trailing.
* **Trailing Step (pips)** – mejora mínima en pips antes de que se ajuste el trailing stop.
* **Position Sizing** – elegir entre dimensionamiento de volumen fijo y por porcentaje de riesgo.
* **Risk Percent** – porcentaje del portafolio usado cuando el dimensionamiento por riesgo está activo.
* **MA Period / Method / Price** – configuración para el filtro de media móvil.
* **MACD Fast / Slow / Signal** – longitudes de EMA para el MACD.
* **MACD Price** – precio aplicado usado para el cálculo del MACD.
* **MACD Level (pips)** – magnitud mínima absoluta del MACD para validar una operación.
* **Candle Type** – marco temporal que impulsa las actualizaciones del indicador.
