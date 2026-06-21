# Estrategia de Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia implementa la lógica de trailing stop del script MQL original `TRAILING.mq4`. Gestiona una posición abierta existente y la cierra cuando el mercado alcanza un objetivo de beneficio especificado o llega a un stop loss. Cuando el parámetro de trailing está habilitado, el nivel de stop sigue al precio para asegurar las ganancias.

## Parámetros
- **TakeProfit** – distancia de beneficio desde el precio de entrada en unidades absolutas de precio.
- **StopLoss** – distancia adversa máxima permitida desde el precio de entrada.
- **Trailing** – distancia utilizada para el trailing dinámico del nivel de stop.
- **CandleType** – serie de velas utilizada para obtener actualizaciones de precio.

## Cómo Funciona
1. La estrategia se suscribe a la serie de velas elegida.
2. Después de cada vela terminada se evalúa la posición actual.
3. Para posiciones largas, la estrategia cierra la posición cuando el beneficio supera *TakeProfit* o la pérdida supera *StopLoss*.
4. Si *Trailing* es mayor que cero, el nivel de stop se mueve hacia arriba con el precio. Cuando el precio cae por debajo del trailing stop, la posición se cierra.
5. Las posiciones cortas siguen la misma lógica pero en dirección opuesta.
6. El precio de entrada se registra desde la primera operación ejecutada y se reinicia cuando la posición se cierra.

## Notas
- La estrategia utiliza la API de alto nivel con `Bind` para procesar velas.
- No abre nuevas posiciones por sí sola; solo gestiona una posición ya abierta.
- Los parámetros se exponen a través de `StrategyParam` y pueden optimizarse.
