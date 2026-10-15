# Diagrama de estrategia de una operación cada 24 horas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama negocia cruces estrictos de EMA(10) y EMA(30) en velas finalizadas de cuatro horas y en sentido inverso. Una puerta configurable de horario de trabajo controla cuándo se admiten candidatos, mientras Flag y un contador N values de seis velas permiten como máximo una decisión de entrada en cada intervalo móvil de 24 horas.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas finalizadas de cuatro horas alimentan EMA 10 y EMA 30. El estado del cruce evoluciona desde el inicio, pero los candidatos de entrada solo se habilitan después de diez velas finalizadas.
- Un cruce estricto ascendente de la EMA rápida sobre la lenta genera un candidato de venta. Un cruce estricto descendente genera un candidato de compra.
- Time y Working time solo admiten candidatos dentro del intervalo configurado. Combination reúne ambos flujos direccionales y Flag libera únicamente el primer candidato aceptado hasta su reinicio.
- La entrada aceptada inicia N values. Después de otras seis velas finalizadas de cuatro horas, el contador reinicia Flag y crea una limitación móvil de 24 horas.
- Desde una posición cero, Position modify envía una orden de mercado de Volume fijo. Contra una posición unitaria opuesta, primero la cierra y luego abre el lado nuevo con una segunda orden de mercado de Volume fijo.
- Position protection es el mecanismo de salida. Sigue las ejecuciones directas de entradas y giros y puede cerrar la posición con un take-profit del 3% o un stop-loss fijo del 2%.

## Reglas de entrada y salida

- **Entrada en largo**: Tras un cruce estricto descendente de las EMA, diez velas finalizadas de calentamiento, la puerta Working time abierta y el cerrojo móvil disponible, se compra Volume. Desde cero abre un largo; desde un corto unitario compra una vez para cerrar y otra para abrir el largo.
- **Entrada en corto**: Tras un cruce estricto ascendente de las EMA, diez velas finalizadas de calentamiento, la puerta Working time abierta y el cerrojo móvil disponible, se vende Volume. Desde cero abre un corto; desde un largo unitario vende una vez para cerrar y otra para abrir el corto.
- **Salida**: Position protection cierra la exposición seguida con un take-profit del 3% o un stop-loss fijo del 2%. Un cruce opuesto admisible posterior puede girar la posición mediante un cierre seguido de una nueva apertura.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 04:00:00 | Marco temporal de cuatro horas; solo las velas finalizadas controlan las EMA, el calentamiento, el contador móvil y las comprobaciones de precio de la protección. |
| Fast EMA Length | 10 | Longitud de la media móvil exponencial rápida. |
| Slow EMA Length | 30 | Longitud de la media móvil exponencial lenta. |
| Warmup Bars | 10 | Número de velas finalizadas necesario antes de permitir entradas por cruce. |
| Session From | 00:00:00 | Inicio de la sesión admisible en el tiempo de reproducción o de servidor de la estrategia. |
| Session Until | 23:59:59 | Fin de la sesión admisible en el tiempo de reproducción o de servidor de la estrategia. |
| Rolling Cooldown Bars | 6 | Número de velas finalizadas contado tras una entrada aceptada antes de permitir otra decisión; seis velas de cuatro horas equivalen a 24 horas. |
| Volume | 1 | Cantidad fija utilizada por cada acción de apertura o giro. |
| Take Profit % | 3 | Movimiento porcentual favorable utilizado por Position protection. |
| Stop Loss % | 2 | Movimiento porcentual adverso utilizado por el stop fijo, no dinámico. |

## Detalles del diagrama

- El bloque [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas finalizadas de cuatro horas a dos bloques [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html). Crossing identifica cambios entre EMA 10 y EMA 30; comparaciones estrictas de valores anteriores y actuales validan cada evento, y una rama NOT crea el pulso descendente.
- Una puerta de calentamiento de diez velas bloquea los candidatos tempranos. [Time](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/time.html) entrega el tiempo de reproducción o de servidor a [Working time](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/working_time.html); la reproducción del historial incluido usa UTC.
- Combination pasa candidatos accionables de compra y venta a [Flag](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/flag.html). El primer candidato verdadero se libera, inicia [N values](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/n_values.html) y bloquea los siguientes hasta que otras seis velas finalizadas reinician Flag.
- La posición actual selecciona la ruta de [Modificación de posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html). Una entrada desde cero usa una acción de mercado de lado fijo; un giro usa dos acciones consecutivas del mismo lado, primero para quedar a cero y después para abrir. Por ello la limitación cuenta decisiones de entrada aceptadas, aunque una decisión puede producir intencionadamente dos ejecuciones.
- Todas las ejecuciones directas de apertura y giro actualizan la [Protección de posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). Los cierres de velas finalizadas conducen sus comprobaciones de precio; su propia ejecución de cierre no vuelve a su entrada de operaciones.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
