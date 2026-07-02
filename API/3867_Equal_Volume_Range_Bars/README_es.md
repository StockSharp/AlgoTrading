# Barras de igual volumen y rango
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Las barras de volumen y rango iguales transfieren el script MetaTrader 4 `equalvolumebars.mq4` a StockSharp. El script original generaba gráficos fuera de línea cuyas velas se cerraban después de un número fijo de ticks o después de que el precio había atravesado un rango de puntos configurable. La estrategia reproduce la misma lógica de creación de velas dentro del entorno StockSharp: escucha ticks en vivo, opcionalmente precarga velas M1 históricas y emite entradas de registro detalladas cada vez que se completa una barra sintética.

## Lógica de construcción de velas
* **Modos de funcionamiento duales**: `EqualVolumeBars` cierra la barra una vez que el volumen de ticks acumulado supera el umbral configurado, mientras que `RangeBars` requiere que el rango alto-bajo de la vela (medido en pasos de precio de seguridad) supere el mismo umbral numérico.
* **Actualizaciones basadas en ticks**: cada actualización comercial actualiza el volumen máximo, mínimo, de cierre y de tick de la vela actual. Cuando se excede el umbral, la estrategia finaliza la vela anterior con las estadísticas existentes e inmediatamente comienza una nueva barra con el tick actual como primera entrada.
* **Semilla de historial de minutos (opcional)**: cuando `FromMinuteHistory` está habilitado, la estrategia reproduce velas M1 terminadas como una secuencia de ticks sintéticos (apertura → extremos intermedios → cierre). Esto se aproxima al paso de inicialización del gráfico fuera de línea sin necesidad de archivos CSV externos.
* **Marcas de tiempo monótonas**: el constructor aplica marcas de tiempo estrictamente crecientes para que los consumidores de registros o los módulos posteriores puedan cargar los datos sin encontrar claves de tiempo duplicadas.

## Parámetros
* **Modo de trabajo**: selecciona entre `EqualVolumeBars` y `RangeBars` construcción de velas.
* **Ticks In Bar**: número de ticks por vela (modo de igual volumen) o rango de puntos medido en pasos de precio (modo de rango).
* **Usar historial de minutos**: permite la reproducción sintética de velas M1 terminadas antes de que lleguen los ticks activos.
* **Tipo de vela de minuto**: suscripción de vela utilizada para el paso de siembra histórico (el valor predeterminado es un período de tiempo de un minuto).

## Notas adicionales
* La estrategia infiere el tamaño en puntos de `Security.PriceStep` (recurriendo a `Security.MinPriceStep` o `0.0001` cuando no hay metadatos disponibles) para reflejar la constante `_Point` utilizada por MetaTrader.
* En lugar de escribir archivos `.hst` y actualizar una ventana de gráfico, el puerto C# registra cada vela terminada con datos OHLCV completos, lo que facilita alimentar otro componente o comparar resultados con el generador de gráficos sin conexión MT4.
* Nunca se envían pedidos; la clase se centra exclusivamente en la transformación de datos al igual que el guión original.
* Sólo se proporciona la versión C#. Una versión y una carpeta de Python se omiten intencionalmente según los requisitos de conversión.
