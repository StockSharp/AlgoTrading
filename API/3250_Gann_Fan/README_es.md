# Estrategia de Gann Fan
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de StockSharp reproduce el experto MetaTrader **GANN_FAN** usando la API de alto nivel. Combina filtros de tendencia de medias móviles ponderadas linealmente con confirmación de Momentum, una puerta de dirección MACD y una reconstrucción basada en fractales del Gann Fan para determinar el sesgo alcista o bajista. La gestión de riesgos refleja el robot original con entradas apiladas de estilo martingala, stops fijos, protección de trailing y movimientos de punto de equilibrio opcionales.

## Lógica de trading

1. **Filtro de tendencia** – Dos medias móviles ponderadas linealmente (LWMA) construidas sobre el precio típico (H+L+C)/3 definen la tendencia rápida y lenta. Los trades largos requieren que la LWMA rápida permanezca por encima de la LWMA lenta; los trades cortos necesitan el cruce inverso.
2. **Confirmación de Momentum** – La estrategia calcula el oscilador de Momentum clásico como `100 * Close / Close(n)` y evalúa la desviación del nivel neutro 100 durante los últimos tres velas cerradas. Al menos una desviación debe superar el umbral configurado para confirmar la fortaleza en la dirección del trade.
3. **Dirección MACD** – Una señal MACD configurable (períodos de EMA rápida, lenta y de señal) debe estar de acuerdo con la tendencia. Las entradas largas requieren que la línea MACD sea mayor que la línea de señal, mientras que los cortos necesitan que la línea MACD permanezca por debajo de la línea de señal.
4. **Orientación del Gann Fan** – Los fractales confirmados de Bill Williams reconstruyen los rayos del Gann Fan alcista y bajista. Los dos fractales descendentes más recientes forman el rayo alcista; su pendiente debe ser positiva para permitir largos. Los dos últimos fractales ascendentes definen el rayo bajista; su pendiente debe ser negativa para autorizar ventas en corto.
5. **Apilamiento de posiciones** – Cuando llega una nueva señal, la estrategia puede añadir a una posición existente hasta el máximo configurado. Cada orden adicional aumenta el volumen multiplicando el lote base por el exponente de lote, emulando el dimensionamiento de martingala utilizado en la versión MQL.

## Gestión de riesgos

- **Stop-loss y take-profit fijos** – Expresados en pasos de precio del instrumento, convertidos automáticamente por la estrategia usando `Security.PriceStep`.
- **Control de punto de equilibrio** – Cuando está habilitado, una vez que el beneficio alcanza la distancia de disparo, el stop se adelanta a la entrada más/menos el offset configurado.
- **Stop de seguimiento** – Se activa después de alcanzar la distancia de disparo. El stop puede seguir al mercado ya sea por una distancia fija del cierre o bloqueando el valor más bajo (para largos) / más alto (para cortos) de las velas más recientes más un factor de margen.
- **Interruptor de salida forzada** – Establecer `Force Exit` en `true` liquida inmediatamente cualquier exposición abierta en la próxima vela terminada.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **Volume** | Tamaño base de la orden utilizado para la primera entrada. |
| **Fast LWMA / Slow LWMA** | Períodos de las medias móviles ponderadas linealmente utilizadas para el filtro de tendencia. |
| **Momentum Period / Threshold** | Retrospectiva del cálculo de Momentum y desviación mínima de 100 requerida para operar. |
| **MACD Fast / Slow / Signal** | Períodos de EMA para el filtro de confirmación MACD. |
| **Fractal History** | Número máximo de puntos de fractal confirmados almacenados para construir los rayos del Gann Fan. |
| **Max Trades** | Número máximo de entradas apiladas permitidas en una sola dirección. |
| **Lot Exponent** | Multiplicador aplicado al volumen base para cada entrada adicional. |
| **Stop Loss / Take Profit** | Distancias de protección en pasos de precio. |
| **Enable Trailing** | Habilita la gestión del stop de seguimiento. |
| **Trail Trigger / Distance / Padding** | Disparador de beneficio, distancia de trailing y margen extra (en pasos de precio) utilizado al hacer trailing mediante extremos de vela. |
| **Use Candle Trail** | Habilita el trailing basado en velas además del trailing de distancia fija. |
| **Trailing Candles** | Número de velas terminadas recientes consideradas al calcular los niveles de trailing basados en velas. |
| **Enable Break-even** | Activa o desactiva la lógica de punto de equilibrio. |
| **Break-even Trigger / Offset** | Disparador de beneficio y offset (en pasos de precio) para mover el stop al punto de equilibrio. |
| **Use Gann Filter** | Impone la orientación alcista/bajista del Gann Fan para las entradas. |
| **Force Exit** | Obliga a la estrategia a cerrar todas las posiciones en la siguiente barra. |
| **Candle Type** | Series de velas utilizadas para cálculos y generación de órdenes. |

## Notas

- Todos los cálculos de indicadores funcionan exclusivamente en velas terminadas proporcionadas por `SubscribeCandles` y `Bind` para cumplir con las mejores prácticas de la API de alto nivel de StockSharp.
- Las distancias de trailing y punto de equilibrio se adaptan automáticamente al tamaño del tick del instrumento. Cuando `PriceStep` no está disponible, las características de protección permanecen inactivas hasta que el conector lo proporcione.
- La estrategia mantiene estados separados para posiciones largas y cortas, asegurando que los niveles de trailing y punto de equilibrio se reinicien cuando la exposición cambia de dirección.
- Para imitar el experto MetaTrader de cerca, las alertas, notificaciones y objetos de gráfico explícitos del código original son reemplazados por la reconstrucción nativa del Gann Fan de StockSharp usando fractales.
