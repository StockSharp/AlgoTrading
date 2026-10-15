# Estrategia de alerta cruzada de TenKijun (ID 3562)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una versión API de alto nivel de StockSharp del asesor experto MetaTrader **TenKijun.mq4**. El EA original solo observa el indicador Ichimoku y envía notificaciones automáticas cuando el Tenkan-sen (línea de conversión) cruza el Kijun-sen (línea de base). La versión C# mantiene la naturaleza de solo alerta pero actualiza la implementación con infraestructura StockSharp, enlaces de gráficos, parametrización y manejo seguro de sesiones.

La lógica funciona en velas completadas de un período de tiempo configurable. Cuando se cierra una nueva vela dentro del horario comercial activo, la estrategia evalúa el indicador Ichimoku calculado con los períodos clásicos del 26/09/52 y registra los últimos valores de Tenkan/Kijun. Si el Tenkan cruza por encima del Kijun, se registra un mensaje informativo que indica un cruce alcista; si Tenkan cruza por debajo de Kijun se registra una alerta bajista. No se ejecutan operaciones: la estrategia está destinada a generar señales o combinarse con automatización externa.

## Indicador y flujo de datos

- **Indicador**: indicador StockSharp `Ichimoku` con longitudes Tenkan, Kijun y Senkou Span B parametrizadas por separado. Sólo las líneas Tenkan y Kijun se utilizan para la toma de decisiones, reflejando el EA original.
- **Suscripción de datos**: utiliza `SubscribeCandles` con un `CandleType` configurable. De forma predeterminada, se solicitan velas con un período de tiempo de 30 minutos.
- **Encuadernación**: `BindEx` se emplea para que el `IchimokuValue` escrito se entregue al controlador sin llamadas manuales a `GetValue`.
- **Gráficos**: las velas y el indicador Ichimoku se adjuntan automáticamente al gráfico de estrategia para una validación visual rápida de las alertas.

## Filtro de sesión de negociación

El script MetaTrader restringió las alertas a una ventana de sesión definida por el usuario. El puerto expone la misma característica a través de dos parámetros:

- `StartHour` – inicio inclusivo de la ventana activa (predeterminado 0). Acepta 0-23.
- `LastHour` – final inclusivo de la ventana activa (predeterminado 20). Acepta 0-23.

Si `StartHour` es menor o igual a `LastHour`, las alertas se producen entre esas dos horas del día. Si el inicio es mayor que el final, la ventana se trata como si fuera durante la noche (por ejemplo, 20 → 6 cubre la sesión desde tarde hasta temprano en la mañana).

## Parámetros

| Parámetro | Descripción | Predeterminado | Notas |
|-----------|-------------|---------|-------|
| `StartHour` | Hora en la que pueden comenzar las alertas. | 0 | Inclusive, rango 0-23. |
| `LastHour` | Hora en la que cesan las alertas. | 20 | Inclusive, rango 0-23. |
| `TenkanPeriod` | Línea de conversión retrospectiva. | 9 | Optimizable. |
| `KijunPeriod` | Una mirada retrospectiva a la línea de base. | 26 | Optimizable. |
| `SenkouSpanBPeriod` | Vista retrospectiva del tramo B principal. | 52 | Se proporciona para que esté completo, aunque las alertas no dependen de la nube. |
| `CandleType` | Serie de velas utilizadas para el indicador. | plazo de 30 minutos | Elija cualquier período de tiempo basado en `TimeSpan`. |

## Lógica de alerta

1. Espere a que la primera vela terminada inicialice la historia de Tenkan y Kijun.
2. En cada vela finalizada posterior dentro de la ventana de negociación:
   - Extraiga los valores Tenkan y Kijun del indicador Ichimoku.
   - Detectar un cruce alcista cuando el Tenkan anterior era menor o igual que el Kijun anterior y el Tenkan actual es mayor que el Kijun actual.
   - Detectar un cruce bajista cuando el Tenkan anterior era mayor o igual que el Kijun anterior y el Tenkan actual es menor que el Kijun actual.
   - Emita una entrada de registro informativa que describa la dirección, el precio y la marca de tiempo del cruce.

## Consejos de uso

- Combine esta estrategia con StockSharp adaptadores de notificación (correo electrónico, Telegram, sonido) suscribiéndose al registro de estrategia o ampliando el método `ProcessCandle` con un código de notificación personalizado.
- Para impulsar el comercio automatizado, herede de `TenKijunCrossStrategy` y anule `ProcessCandle` para realizar pedidos en lugar de (o además de) registrar mensajes.
- Ajuste el período de tiempo de la vela para que coincida con el gráfico MetaTrader original utilizado por EA para mantener las alertas alineadas.

## Diferencias con el original EA

- Utiliza el registro StockSharp en lugar de MetaTrader `SendNotification`. El comportamiento sigue siendo de solo alerta, pero depende del canal de mensajes de la plataforma.
- Agrega metadatos de parámetros completos (`SetDisplay`, rangos, indicadores de optimización) que preparan la estrategia para las herramientas Designer/Optimizer.
- Dibuja automáticamente velas y el indicador Ichimoku en la ventana del gráfico StockSharp cuando esté disponible.

## Archivos

- `CS/TenKijunCrossStrategy.cs`: implementación principal de C# de la lógica de alerta.
