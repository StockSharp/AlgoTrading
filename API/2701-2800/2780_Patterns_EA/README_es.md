# Estrategia de Patrones EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Patrones EA es un sistema de acción del precio que escanea las tres velas completadas más recientes en busca de una amplia gama de formaciones de una, dos y tres barras. La lógica es un port de StockSharp del asesor experto original de MQL5 "Patterns_EA" y preserva su catálogo configurable de 30 configuraciones de velas japonesas. Cada patrón puede habilitarse o deshabilitarse de forma independiente y puede asignarse a ejecución larga o corta, permitiendo que la estrategia imite las reglas discrecionales del script fuente.

## Grupos de patrones
El detector evalúa la vela actual y hasta las dos velas anteriores dependiendo del grupo de patrones:

- **Grupo 1 – Patrones de una barra:** Neutral Bar, Force Bar Up, Force Bar Down, Hammer, Shooting Star.
- **Grupo 2 – Patrones de dos barras:** Inside, Outside, Double Bar High Lower Close, Double Bar Low Higher Close, Mirror Bar, Bearish Harami, Bearish Harami Cross, Bullish Harami, Bullish Harami Cross, Dark Cloud Cover, Doji Star, Engulfing Bearish Line, Engulfing Bullish Line, Two Neutral Bars.
- **Grupo 3 – Patrones de tres barras:** Double Inside, Pin Up, Pin Down, Pivot Point Reversal Up, Pivot Point Reversal Down, Close Price Reversal Up, Close Price Reversal Down, Evening Star, Morning Star, Evening Doji Star, Morning Doji Star.

Un parámetro de tolerancia (Equality Pips) controla qué tan de cerca deben coincidir dos precios para satisfacer las verificaciones de igualdad, reproduciendo la configuración de "distancia máxima en pips" del EA original.

## Parámetros
- **Candle Type** – Marco temporal utilizado para la detección de patrones.
- **Opened Mode** – Lógica de gestión de posición (Any, Swing, Buy One, Buy Many, Sell One, Sell Many) replicada de la versión MQL.
- **Equality Pips** – Distancia en pips que define la igualdad de precios; ajustada por el paso de precio del instrumento.
- **Enable One-Bar Patterns / Enable Two-Bar Patterns / Enable Three-Bar Patterns** – Interruptores para cada grupo de patrones.
- **Enable {Pattern}** – Interruptores individuales para las 30 formaciones.
- **{Pattern} Order** – Dirección de la operación (compra o venta) asignada al patrón correspondiente.

Todos los parámetros están expuestos a través de objetos `StrategyParam`, lo que permite la optimización o vinculación de UI cuando se usa dentro de aplicaciones StockSharp.

## Lógica de trading
1. La estrategia se suscribe a la serie de velas configurada y espera velas completadas.
2. Cuando se cierra una nueva barra, las últimas tres velas se almacenan en caché y el detector evalúa los grupos de patrones habilitados.
3. Cada patrón replica las reglas condicionales de la fuente MQL5, incluyendo comparaciones tolerantes y relaciones de sombra/cuerpo.
4. Cuando se confirma un patrón, `TriggerPattern` verifica si el grupo y el patrón individual están habilitados, verifica la dirección seleccionada y aplica el modo de posición configurado.
5. La estrategia envía una orden de mercado en la dirección asignada. En modo Swing, primero cierra cualquier posición abierta en la dirección opuesta.

## Modos de posición
- **Any:** Permite señales en ambas direcciones sin restricciones adicionales.
- **Swing:** Mantiene una sola posición neta; las señales opuestas aplastan la posición existente antes de entrar a la nueva.
- **Buy One / Sell One:** Restringen las operaciones a una sola posición larga o corta respectivamente.
- **Buy Many / Sell Many:** Permiten múltiples entradas en la dirección especificada ignorando señales en la dirección opuesta.

## Notas
- El algoritmo usa `Security.PriceStep` para traducir la tolerancia de igualdad en distancia de precio absoluta. Si el instrumento no define un paso de precio, se aplica un valor predeterminado de 0.0001.
- No se requieren indicadores adicionales; toda la lógica depende únicamente de la geometría de las velas, coincidiendo con la intención del asesor experto original.
