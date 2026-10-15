# Estrategia Bulls Bears Eyes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia evalúa el equilibrio entre la presión alcista y bajista utilizando los indicadores **Bulls Power** y **Bears Power**. Los dos indicadores se combinan en un único oscilador escalado de 0 a 100. Los valores altos indican el dominio de los compradores, mientras que los valores bajos señalan la fortaleza de los vendedores.

Las decisiones de trading se basan en niveles de umbral similares al experto original *BullsBearsEyes*. Cuando el oscilador cruza por encima del nivel de sobrecompra después de haber estado por debajo, se abre una posición larga y cualquier posición corta se cierra. Por el contrario, cruzar por debajo del nivel de sobreventa desencadena una entrada corta y cierra los largos existentes. Los valores neutrales entre los umbrales mantienen la posición actual pero cierran las operaciones opuestas.

## Parámetros
- **Period** – período de promediado para Bulls/Bears Power (por defecto: 13).
- **High Level** – umbral de sobrecompra que genera señales largas (por defecto: 75).
- **Middle Level** – nivel de referencia medio utilizado para la interpretación de la tendencia (por defecto: 50).
- **Low Level** – umbral de sobreventa que genera señales cortas (por defecto: 25).
- **Candle Type** – marco temporal de las velas procesadas por la estrategia (por defecto: velas de 4 horas).

## Reglas de entrada y salida
1. Calcular Bulls Power y Bears Power para cada vela y derivar el valor del oscilador entre 0 y 100.
2. **Entrada larga**: el oscilador cruza por encima de *High Level* tras haber estado por debajo. Cualquier posición corta se cierra antes de abrir el largo.
3. **Entrada corta**: el oscilador cruza por debajo de *Low Level* tras haber estado por encima. Cualquier posición larga existente se cierra antes de abrir el corto.
4. **Salida de posición**: cuando el oscilador cambia de lado (por encima/por debajo de la zona central), la posición opuesta se cierra.

El oscilador también se traza junto con las velas para el análisis visual.

## Notas
- La estrategia utiliza la API de alto nivel `SubscribeCandles` y `Bind` para el procesamiento de indicadores.
- Los mecanismos de protección se activan mediante `StartProtection()` al inicio.
- Solo se evalúan las velas completadas para evitar señales prematuras.
