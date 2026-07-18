# Estrategia FXF Safe Trend Scalp V1 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia FXF Safe Trend Scalp V1 opera con rupturas de líneas de tendencia basadas en ZigZag y refleja el comportamiento del asesor experto original MetaTrader 4. Observa la distancia entre el precio actual y las líneas dinámicas de resistencia/soporte construidas a partir de pivotes recientes en ZigZag y alinea las operaciones con un par de promedios móviles simples. El stop-loss protector, la toma de ganancias y la salida de ganancias flotantes reproducen las reglas de administración del dinero del código fuente.

## Lógica de trading

1. **Líneas de tendencia en zigzag**
   - Un detector manual de ZigZag busca alternancia de máximos y mínimos de oscilación utilizando los parámetros configurables de profundidad, desviación y retroceso.
   - Los últimos cuatro máximos definen la línea de resistencia activa, mientras que los últimos cuatro mínimos definen la línea de soporte activa. La estrategia extrapola continuamente esas líneas a la barra actual.
   - Se prepara una señal de entrada cuando el precio de cierre se acerca a una línea dentro de un desplazamiento fijo (10 puntos por defecto).
2. **Filtro de media móvil**
   - Una media móvil simple rápida (longitud 2) y una media móvil simple lenta (longitud 50) filtran la tendencia.
   - Las posiciones cortas requieren la MA rápida por debajo de la MA lenta, mientras que las posiciones largas requieren la MA rápida por encima de la MA lenta.
3. **Ejecución de órdenes**
   - Las señales se almacenan y activan en la siguiente vela terminada, coincidiendo con la lógica de "nueva barra" de la versión MetaTrader.
   - Antes de abrir una posición, la estrategia verifica que el diferencial no supere el máximo configurado y que no haya ninguna posición abierta actualmente.
4. **Gestión de riesgos**
   - Las distancias de stop-loss y take-profit se expresan en puntos y se aplican inmediatamente después de que se ejecuta la orden.
   - Un objetivo de beneficio flotante cierra la posición una vez que el beneficio no realizado (en unidades de precio multiplicado por el volumen) supera la recompensa configurada por lote.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `Candle Type` | Marco de tiempo utilizado para la generación de la señal. |
| `Volume` | Volumen comercial presentado con cada entrada. |
| `ZigZag Depth` | Número mínimo de barras entre pivotes confirmados. |
| `ZigZag Deviation (pts)` | El precio mínimo se mueve en puntos antes de que cambie la dirección. |
| `ZigZag Backstep` | Se requieren barras antes de aceptar un pivote opuesto. |
| `Trend Offset (pts)` | Distancia desde la línea de tendencia que activa una señal. |
| `Fast MA Length` | Longitud de la media móvil simple rápida. |
| `Slow MA Length` | Longitud de la media móvil simple lenta. |
| `Max Spread (pts)` | Spread máximo permitido, expresado en puntos. |
| `Stop Loss (pts)` | Distancia de parada de protección medida desde el precio de entrada. |
| `Take Profit (pts)` | Distancia objetivo de beneficio medida a partir del precio de entrada. |
| `Profit Target per Lot` | Se requiere beneficio flotante (unidades de precio × volumen) para cerrar la posición. |

## Notas

- Sólo se ocupa un puesto a la vez. Las señales se ignoran mientras una operación está abierta.
- El filtro de diferencial se basa en las mejores cotizaciones de oferta y demanda, por lo que la estrategia debe estar conectada a una fuente de datos que proporcione información de nivel 1.
- La versión Python de la estrategia se omite intencionalmente según lo solicitado.

## Archivos

- `CS/FXFSafeTrendScalpV1Strategy.cs` – StockSharp implementación del asesor experto.
