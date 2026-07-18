# Estrategia del arco iris del seguidor de tendencias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Trend Follower Rainbow Strategy es una adaptación de C# del asesor experto MetaTrader 4 "TrendFollowerRainbowMethodkyast773". La estrategia combina varias capas de confirmación para operar en la dirección de tendencias fuertes mientras filtra períodos dentro de un rango. Se basa en la alineación de un arco iris de promedios móviles exponenciales, impulso MACD, umbrales del oscilador de Laguerre, lecturas del índice de flujo de dinero y un cruce EMA rápido/lento para activar posiciones.

## Lógica de trading
1. **Ventana de negociación**: las señales se evalúan solo cuando el tiempo de cierre de la vela actual está estrictamente entre las horas de inicio y finalización configurables. Esto imita el filtro de tiempo del EA original que evitaba la primera y la última hora de negociación de la sesión.
2. **EMA Activador de cruce**: una configuración larga requiere que el EMA rápido (longitud predeterminada 4) cruce por encima del EMA lento (longitud predeterminada 8). Una configuración corta requiere el cruce opuesto.
3. **MACD Confirmación**: la línea MACD y la línea de señal (predeterminada 5/35/5) deben estar por encima de cero para operaciones largas o por debajo de cero para operaciones cortas para confirmar la alineación del impulso.
4. **Filtro Laguerre**: el valor del filtro Laguerre debe superar 0,15 para operaciones largas o menos de 0,75 para operaciones cortas, reproduciendo las comprobaciones de umbral originales realizadas en el indicador personalizado.
5. **Alineación del arco iris**: se deben ordenar monótonamente cinco paquetes de medias móviles exponenciales (cuatro EMA por paquete) para confirmar la estructura del arco iris. Los paquetes se evalúan en busca de un orden no creciente en escenarios alcistas y un orden no decreciente en escenarios bajistas.
6. **Filtro de índice de flujo de dinero**: el índice de flujo de dinero (período predeterminado 14) debe estar por debajo de 40 para entradas largas y por encima de 60 para entradas cortas para evitar operar contra el flujo impulsado por el volumen.
7. **Gestión de posiciones**: se utilizan órdenes de mercado. Cuando aparece una señal opuesta, la exposición existente se cierra y se abre una nueva posición en la dirección opuesta.

## Gestión del riesgo
La estrategia admite protecciones integradas a través del asistente `StartProtection` de StockSharp:
- Las distancias de **Take Profit** y **Stop Loss** se expresan en pasos de precio para reflejar la configuración basada en puntos de EA.
- La distancia **Trailing Stop** también utiliza pasos de precio y se activa una vez que se inicia el bloqueo de protección.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Volumen de orden de mercado base. | 1 |
| `TakeProfitPoints` | Tome la distancia de beneficio en pasos de precio. | 17 |
| `StopLossPoints` | Distancia de stop loss en pasos de precio. | 30 |
| `TrailingStopPoints` | Distancia del trailing stop en pasos de precio. | 45 |
| `TradingStartHour` | Primera hora (inclusive) que se salta antes de evaluar señales. | 1 |
| `TradingEndHour` | Última hora (inclusive) que se salta tras evaluar señales. | 23 |
| `FastEmaLength` | Longitud del EMA rápido utilizado en el activador de cruce. | 4 |
| `SlowEmaLength` | Longitud del EMA lento utilizado en el activador de cruce. | 8 |
| `MacdFastLength` | MACD EMA rápida longitud. | 5 |
| `MacdSlowLength` | MACD EMA lenta longitud. | 35 |
| `MacdSignalLength` | MACD señal EMA longitud. | 5 |
| `LaguerreGamma` | Factor de suavizado del filtro de Laguerre. | 0,7 |
| `LaguerreBuyThreshold` | El umbral de Laguerre cruzó hacia arriba para operaciones largas. | 0,15 |
| `LaguerreSellThreshold` | El umbral de Laguerre cruzó a la baja para operaciones cortas. | 0,75 |
| `MfiPeriod` | Período de cálculo del Índice de Flujo de Dinero. | 14 |
| `MfiBuyLevel` | Nivel máximo de IMF que aún permite entradas largas. | 40 |
| `MfiSellLevel` | Nivel mínimo de IMF que aún permite entradas cortas. | 60 |
| `RainbowGroup{1..5}Base` | Longitud base EMA para cada paquete de arcoíris. Se crean cuatro EMA consecutivos a partir de cada valor base agregando compensaciones (0, 2, 4, 6). | 5 / 13 / 21 / 34 / 55 |
| `CandleType` | Serie de velas primaria utilizada por la estrategia. El valor predeterminado es velas de 5 minutos. | marco de tiempo de 5 minutos |

## Trazar
La estrategia dibuja automáticamente:
- Velas de precio para la serie suscrita.
- EMA rápidas y lentas para confirmación visual de cruces.
- Laguerre filtra valores para observar cruces de umbrales.
- Operaciones propias trazadas en el área del gráfico.

## Notas
- La lógica del arco iris se aproxima a los indicadores personalizados originales de RainbowMMA mediante la creación de paquetes EMA configurables. Ajuste las longitudes de la base para que coincidan con una plantilla de arcoíris específica si es necesario.
- Todos los comentarios de código, registros y documentación se proporcionan en inglés según sea necesario.
- La estrategia se centra únicamente en la implementación de C#. No se genera ningún puerto Python en esta tarea.
