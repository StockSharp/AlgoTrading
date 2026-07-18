# Estrategia X-MAN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia X MAN recrea la lógica central del MetaTrader asesor experto `X_MAN.mq4` dentro dla API de alto nivel de StockSharp. El sistema negocia rupturas impulsadas por un promedio móvil ponderado lineal (LWMA) rápido y lento mientras filtra entradas con impulso de múltiples períodos de tiempo y una confirmación mensual MACD. Está diseñado para operaciones de continuación de tendencias que se activan sólo cuando el impulso y la estructura de la tendencia se alinean.

## Lógica de trading

1. **Filtro de tendencia principal**: dos LWMA calculadas en el período de tiempo principal seleccionado deben estar separadas por al menos el `DistancePoints` configurable. Una configuración larga requiere que el LWMA rápido esté por encima del LWMA lento por ese margen, mientras que una configuración corta necesita que el LWMA lento domine.
2. **Confirmación de impulso**: la estrategia se suscribe a una serie de velas con períodos de tiempo más altos y la introduce en un indicador de impulso. La distancia absoluta de las últimas tres lecturas de impulso desde el valor neutral (100) debe exceder el umbral de compra o venta correspondiente al menos una vez para permitir operar en esa dirección.
3. **MACD Filtro**: una serie de velas mensuales genera un estándar (12, 26, 9) MACD. Las operaciones largas se permiten solo cuando la línea MACD está por encima de la línea de señal, y las operaciones cortas requieren la relación opuesta.
4. **Ejecución de órdenes**: cuando todos los filtros coinciden, la estrategia ingresa utilizando órdenes de mercado. Las posiciones se invierten sólo si aparece la configuración opuesta y la posición actual es plana o está en la dirección opuesta.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Periodo de tiempo principal utilizado para los cálculos de LWMA. |
| `HigherCandleType` | Marco de tiempo más alto que alimenta el filtro de impulso. |
| `MacdCandleType` | Plazo para la confirmación MACD (mensual por defecto). |
| `FastMaPeriod` | Longitud de la LWMA rápida. |
| `SlowMaPeriod` | Duración de la LWMA lenta. |
| `MomentumPeriod` | Ventana retrospectiva del oscilador de impulso. |
| `MomentumBuyThreshold` | Se requiere una distancia mínima desde 100 para un impulso alcista. |
| `MomentumSellThreshold` | Se requiere una distancia mínima de 100 para el impulso bajista. |
| `DistancePoints` | Separación mínima entre LWMA rápida y lenta expresada en puntos de precio. |
| `TakeProfitPoints` | Distancia protectora opcional de toma de ganancias en puntos. |
| `StopLossPoints` | Distancia de parada de pérdida de protección opcional en puntos. |

Todos los parámetros se exponen a través de `StrategyParam<T>` para que puedan optimizarse dentro de StockSharp Designer o configurarse en tiempo de ejecución.

## Gestión del riesgo

Si `TakeProfitPoints` o `StopLossPoints` es mayor que cero, la estrategia habilita el módulo de protección integrado de StockSharp utilizando salidas de mercado. Aún no se ha implementado ninguna lógica adicional de seguimiento o equilibrio del experto original MQL.

## Diferencias con el experto original

- La implementación de MetaTrader manejó paradas de acciones, movimientos de equilibrio y opciones complejas de administración de dinero. Esta conversión se centra en los filtros direccionales principales y las entradas al mercado; Se omite intencionalmente la gestión del dinero a nivel de cartera.
- El tamaño del pedido se delega al entorno de alojamiento. La lógica original del exponente del lote no se reproduce.
- No se incluyen alertas, notificaciones por correo electrónico ni modificaciones manuales del trailing-stop.

Estos cambios mantienen la estrategia concisa y aprovechan el alto nivel API de StockSharp al tiempo que preservan el concepto comercial principal.
