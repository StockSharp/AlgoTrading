# Estrategia ColorJMomentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia ColorJMomentum** opera basándose en los cambios de dirección de un indicador de Momentum suavizado con Jurik. El enfoque se deriva del asesor experto MQL5 original `Exp_ColorJMomentum` y se reproduce utilizando la API de alto nivel de StockSharp.

## Concepto

1. Calcular el *Momentum* estándar de la serie de precios seleccionada.
2. Suavizar los valores de Momentum con la **Jurik Moving Average (JMA)**.
3. Monitorear los últimos dos valores del Momentum suavizado:
   - Si el indicador estaba declinando y gira hacia arriba, se abre una posición **larga**.
   - Si el indicador estaba subiendo y gira hacia abajo, se abre una posición **corta**.
4. La protección de la posición se maneja mediante stop loss y take profit opcionales en términos porcentuales.

La estrategia nunca lee valores históricos del indicador directamente. En cambio, reacciona solo a las nuevas completaciones de velas y almacena valores anteriores internamente.

## Parámetros

- **Momentum Length** – período para el cálculo del Momentum.
- **JMA Length** – período de suavizado del Jurik Moving Average aplicado al Momentum.
- **Candle Type** – marco temporal utilizado para las suscripciones de velas.
- **Stop Loss %** – porcentaje para stop loss opcional.
- **Enable Stop Loss** – si activar el stop loss.
- **Take Profit %** – porcentaje para el take profit.
- **Enable Long** – permitir abrir posiciones largas.
- **Enable Short** – permitir abrir posiciones cortas.

Todos los parámetros se crean con `StrategyParam` para que puedan optimizarse en Designer.

## Uso

1. Adjuntar la estrategia al instrumento deseado.
2. Configurar los parámetros o dejar los valores predeterminados (Momentum de 8 períodos y JMA de 8 períodos en velas de 8 horas).
3. Ejecutar la estrategia. Las órdenes se emitirán vía `BuyMarket` y `SellMarket` cuando la dirección del Momentum se revierta.

## Notas

- La estrategia procesa únicamente velas finalizadas.
- No se establecen colores explícitos para los indicadores: Designer los elige automáticamente.
- El algoritmo evita cualquier LINQ o colecciones personalizadas, siguiendo las pautas del proyecto.
