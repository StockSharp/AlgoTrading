# Estrategia FXF de entrada rápida y salida rápida
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **FXF Fast in Fast out** es un sistema de ruptura impulsado por la volatilidad que convierte el asesor experto original MetaTrader 4 en una estrategia StockSharp de alto nivel. Observa un período de tiempo configurable para velas grandes, mide el diferencial y reacciona colocando órdenes de parada pendientes que intentan capturar la continuación inmediata del impulso. La lógica utiliza sólo velas terminadas para la generación de señales, mientras que las cotizaciones (datos de nivel 1) se utilizan para filtros de diferenciales, colocación de órdenes y gestión de trailing stop.

Cuando la vela actual se expande más allá de un umbral de volatilidad, la estrategia evalúa el precio medio en relación con la apertura de la vela. Si el precio medio cierra por encima de la apertura, se coloca un stop de compra por encima de la mejor demanda; si cierra por debajo, se coloca un stop de venta bajo la mejor oferta. Se adjuntan niveles protectores de stop-loss y take-profit a las órdenes pendientes, y la lógica de seguimiento opcional protege las posiciones abiertas una vez que se completan. La gestión del dinero puede dimensionar dinámicamente las órdenes en función del valor de la cartera y la distancia de parada.

## Lógica de trading
- **Detección de señal**: en cada vela terminada, la estrategia verifica si el rango de la vela expresado en pasos de precio excede `VolatilitySizePoints`. Si el rango es lo suficientemente grande, calcula el precio medio utilizando la última instantánea de la mejor oferta/demanda.
- **Sesgo direccional** – Un precio medio por encima de la apertura de la vela produce un sesgo alcista (orden de parada de compra), mientras que un precio medio por debajo de la apertura produce un sesgo bajista (orden de parada de venta). No se realiza ninguna orden si el precio medio es igual al de apertura o no se cumple el requisito de volatilidad.
- **Filtro de propagación**: las cotizaciones se controlan continuamente. Las órdenes pendientes se crean solo cuando el diferencial actual es inferior a `MaxSpreadPoints`. Si el diferencial se amplía más allá de ese límite, cualquier orden pendiente existente se cancela hasta que el diferencial vuelva a niveles aceptables.
- **Gestión de órdenes pendientes**: solo puede haber una orden pendiente activa por barra. Cada pedido se compensa con la mejor cotización en `EnterOffsetPoints`. Las distancias de stop-loss y take-profit se definen en puntos y se convierten automáticamente en precios.
- **Control de riesgo**: con `UseMoneyManagement` habilitado, el volumen de la orden se dimensiona a partir del valor de la cartera, el porcentaje de riesgo y la distancia de límite de pérdidas utilizando el precio escalonado del instrumento. De lo contrario, se utiliza la propiedad `Volume` predeterminada.
- **Trailing stop**: cuando `EnableTrailing` es verdadero, la estrategia mantiene un trailing stop interno para la posición activa en función de `TrailingStopPoints` más el diferencial actual. Si el precio de mercado cruza el trailing stop, la posición se cierra en el mercado.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `EnterOffsetPoints` | Distancia en pasos de precio entre la mejor cotización y el precio de la orden stop pendiente. |
| `MaxSpreadPoints` | Spread máximo permitido (en pasos de precio). Un diferencial por encima de este límite bloquea nuevas entradas y cancela las órdenes pendientes activas. |
| `TakeProfitPoints` | Distancia de obtención de beneficios en los pasos de precio aplicada a las órdenes pendientes. Establezca en cero para omitir la colocación de obtención de beneficios. |
| `StopLossPoints` | Distancia de stop-loss en pasos de precio. Requerido para dimensionar la administración del dinero. Establezca en cero para deshabilitar la colocación de stop-loss. |
| `VolatilitySizePoints` | Rango de vela mínimo (en pasos de precio) requerido para generar una nueva señal de ruptura. |
| `EnableTrailing` | Activa o desactiva la lógica de trailing stop para posiciones abiertas. |
| `TrailingStopPoints` | Distancia de seguimiento base en incrementos de precios. El nivel final real también incluye el diferencial actual para imitar el comportamiento original de EA. |
| `UseMoneyManagement` | Permite dimensionar las posiciones basadas en la cartera utilizando el valor `RiskPercent`. |
| `RiskPercent` | Porcentaje de riesgo por operación utilizado cuando la gestión del dinero está activa. |
| `MaxOrdersPerBar` | Número máximo de órdenes pendientes permitidas durante una sola barra. Normalmente se establece en 1 para reflejar el asesor experto original. |
| `CandleType` | El marco temporal de las velas utilizadas para los cálculos de señales. El valor predeterminado es 15 minutos. |

## Flujo de trabajo de pedidos
1. **Detección**: una vela terminada que cumple con el criterio de volatilidad establece la dirección comercial deseada.
2. **Validación**: las cotizaciones deben estar disponibles, se debe permitir la negociación, no debe existir ninguna posición abierta y no debe haber ninguna otra orden activa presente.
3. **Colocación**: la estrategia coloca un stop de compra o un stop de venta con la compensación calculada, adjuntando niveles de stop-loss y take-profit.
4. **Seguimiento y salida**: después de que se completa una orden, el módulo de seguimiento observa las cotizaciones más recientes. Al superar el nivel final, la posición se cierra con una orden de mercado. Las órdenes de take-profit y stop-loss permanecen adjuntas a la posición para su ejecución automática por parte del broker o simulador.

## Notas
- La estrategia requiere suscripciones de datos de vela y de nivel 1 para funcionar correctamente.
- El tamaño basado en el riesgo vuelve al `Volume` configurado si los parámetros de límite de pérdidas o los metadatos de seguridad (escalón de precio o precio de escalón) no están disponibles.
- Los trailingstops se gestionan internamente a través de salidas de mercado para que coincidan con el comportamiento de MetaTrader, lo que garantiza la compatibilidad entre diferentes lugares de ejecución.
