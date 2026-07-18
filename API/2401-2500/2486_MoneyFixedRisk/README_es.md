# Estrategia de Riesgo Fijo de Capital
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Riesgo Fijo de Capital es un port directo del asesor experto de MetaTrader 5 **Money Fixed Risk.mq5**. El script original calcula periódicamente el tamaño máximo de posición que mantiene el riesgo por debajo de un porcentaje fijo del capital de la cuenta y luego abre una compra de mercado protegida con órdenes simétricas de stop-loss y take-profit. Esta versión de StockSharp preserva el mismo comportamiento utilizando la API de suscripción a ticks de alto nivel y los controles de riesgo proporcionados por el framework.

La estrategia escucha cada operación (tick) del instrumento seleccionado. Después de un número configurable de ticks, evalúa el valor actual del portafolio, convierte la distancia de stop configurada en pips a unidades de precio y calcula el mayor volumen que mantiene el riesgo dentro del porcentaje de capital especificado. Si el volumen calculado es válido, la estrategia abre una orden de compra de mercado y asigna niveles de stop-loss y take-profit exactamente a la distancia del stop desde el precio de ejecución. El stop y el objetivo se monitorean en cada tick posterior y la posición se cierra una vez que se toca cualquiera de los límites.

## Requisitos de datos
- Se requieren datos de ticks (operaciones) porque la condición de entrada cuenta ticks individuales. Los datos de velas no se utilizan.
- `PriceStep`, `StepPrice`, `VolumeStep`, `MinVolume` y el opcional `MaxVolume` deben estar correctamente configurados para el instrumento de modo que la fórmula de dimensionamiento de posición coincida con las especificaciones del contrato del broker.

## Cómo funciona la estrategia
1. Esperar actualizaciones de ticks a través de `SubscribeTrades()`.
2. Rastrear el último precio negociado e incrementar un contador interno.
3. Cada vez que el contador de ticks alcance el **Intervalo de Ticks**, reiniciar el contador y:
   - Determinar el tamaño del pip a partir de `PriceStep` y `Decimals` (las cotizaciones de 5 y 3 dígitos se escalan automáticamente por 10).
   - Convertir la distancia de stop-loss configurada de pips a unidades de precio.
   - Determinar el capital actual de la cuenta (intenta `Portfolio.CurrentValue`, recurre a `CurrentBalance`, luego a `BeginValue`).
   - Calcular el riesgo monetario por contrato usando la distancia del stop y `StepPrice`.
   - Derivar el volumen máximo que mantiene el riesgo monetario por debajo del `Risk %` del capital y normalizarlo al paso de volumen y límites del exchange.
4. Si el volumen calculado es positivo, enviar una orden de compra de mercado dimensionada para aplanar cualquier exposición corta existente y abrir una nueva posición larga.
5. Registrar los precios de stop-loss y take-profit alrededor del precio de entrada. En cada tick posterior monitorear el precio de la operación y cerrar la posición si se viola cualquier nivel.

## Parámetros
- **Stop Loss (pips)** – distancia del stop-loss expresada en pips. El take-profit se coloca a la misma distancia en dirección opuesta.
- **Risk %** – porcentaje del capital del portafolio arriesgado en cada operación.
- **Ticks Interval** – número de ticks a esperar antes de reevaluar y potencialmente abrir una nueva posición.

Todos los parámetros admiten optimización y validación (deben ser mayores que cero).

## Detalles de gestión monetaria
- Monto de riesgo = `Equity * (Risk % / 100)`.
- Distancia del stop en unidades de precio = `Stop Loss (pips) * pip size`, donde pip size equivale a `PriceStep * 10` para instrumentos de 3 y 5 decimales; de lo contrario `PriceStep`.
- Riesgo monetario por contrato = `(stop distance / PriceStep) * StepPrice`.
- Tamaño de posición = `Risk amount / monetary risk per contract`, redondeado hacia abajo al `VolumeStep` más cercano y restringido por `MinVolume`/`MaxVolume`. Las órdenes se omiten cuando el tamaño normalizado está por debajo del volumen mínimo.

## Diferencias con el asesor experto original
- Se ejecuta completamente dentro de StockSharp sin llamar a librerías de MetaTrader.
- Usa `StartProtection()` para que las protecciones a nivel de plataforma permanezcan activas.
- Se apoya en el portafolio de la estrategia para información del capital actual en lugar de consultar objetos de saldo del terminal.
- Utiliza monitoreo continuo de ticks para salir de posiciones, eliminando la necesidad de órdenes de stop explícitas en este ejemplo educativo.

## Notas de uso
- Este ejemplo abre solo posiciones largas igual que el archivo original. Extender `ProcessTrade` si se requieren operaciones cortas.
- Al hacer backtesting, asegúrese de que los datos de ticks incluyan suficiente profundidad para alcanzar el intervalo de ticks configurado; de lo contrario no se dispararán operaciones.
- Dado que el dimensionamiento de posición depende de los metadatos del broker, verificar la corrección de `PriceStep`, `StepPrice` y las restricciones de volumen antes de operar en vivo.
- La implementación evita usar colecciones de indicadores para respetar las directrices de conversión y mantiene la lógica con estado a través de campos privados.
