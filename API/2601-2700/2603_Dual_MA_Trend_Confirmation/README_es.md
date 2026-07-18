# Estrategia de Confirmación de Tendencia de MA Dual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Confirmación de Tendencia de MA Dual** replica el experto original de MetaTrader que combina una media móvil exponencial lenta (EMA) con una media móvil ponderada linealmente rápida (LWMA). El sistema espera a que ambas medias móviles se alineen en la misma dirección y usa el cierre de la vela anterior como confirmación adicional antes de entrar en una posición. La idea es participar solo en fuertes oscilaciones de impulso cuando el filtro de tendencia lenta y el filtro de confirmación rápida simultáneamente se inclinan hacia arriba o hacia abajo.

La implementación de StockSharp procesa solo las velas completamente terminadas, rastrea la pendiente de cada media móvil durante las últimas tres barras, y gestiona automáticamente las órdenes protectoras mediante el mecanismo integrado `StartProtection`. La estrategia es agnóstica al instrumento: puede operar en cualquier valor y marco temporal que proporcionen velas y soporten el concepto de "puntos" mediante el paso de precio del instrumento.

## Indicadores
- **EMA lenta** – Período predeterminado 57. Representa la dirección de la tendencia dominante. La estrategia requiere que la EMA aumente (o disminuya) durante dos velas consecutivas antes de operar.
- **LWMA rápida** – Período predeterminado 3. Actúa como filtro de confirmación de impulso. Su pendiente debe coincidir con la EMA lenta, reforzando que el impulso apoya la tendencia.

## Parámetros
| Parámetro | Valor predeterminado | Descripción |
|-----------|---------|-------------|
| `SlowMaLength` | 57 | Período del filtro de tendencia EMA lenta. |
| `FastMaLength` | 3 | Período del filtro de confirmación LWMA rápida. |
| `StopLossPoints` | 100 | Distancia de stop protector expresada en puntos del instrumento (multiplicado por `Security.PriceStep`). |
| `TakeProfitPoints` | 100 | Distancia de take-profit expresada en puntos del instrumento (multiplicado por `Security.PriceStep`). |
| `CandleType` | Marco temporal de 15 minutos | Tipo de datos de vela usado para todos los cálculos. |

Todos los parámetros se exponen como valores `StrategyParam<T>` para que puedan modificarse en tiempo de ejecución o optimizarse a través de las herramientas de optimización de StockSharp.

## Reglas de trading
### Setup largo
1. La EMA lenta está subiendo: valor actual > valor anterior > valor hace dos velas.
2. La LWMA rápida está subiendo: valor actual > valor anterior > valor hace dos velas.
3. El cierre de la vela anterior está por encima del valor anterior de la EMA lenta.
4. El valor actual de la EMA lenta está por encima del valor actual de la LWMA rápida.
5. La posición actual es plana o corta.
6. Cuando todas las condiciones se cumplen, la estrategia envía una orden de compra de mercado por `Volume + |Position|` para volcar a una posición larga.

### Setup corto
1. La EMA lenta está cayendo: valor actual < valor anterior < valor hace dos velas.
2. La LWMA rápida está cayendo: valor actual < valor anterior < valor hace dos velas.
3. El cierre de la vela anterior está por debajo del valor anterior de la EMA lenta.
4. El valor actual de la EMA lenta está por debajo del valor actual de la LWMA rápida.
5. La posición actual es plana o larga.
6. Cuando todas las condiciones se cumplen, la estrategia envía una orden de venta de mercado por `Volume + |Position|` para volcar a una posición corta.

### Lógica protectora
- `StartProtection` convierte `StopLossPoints` y `TakeProfitPoints` en desplazamientos de precio absoluto multiplicándolos con `Security.PriceStep`. Las órdenes de stop-loss y take-profit se emiten como salidas de mercado para que el motor pueda cerrar la posición incluso si las órdenes límite no están soportadas.
- Cuando aparece la señal opuesta, la estrategia invierte inmediatamente la posición independientemente de las órdenes protectoras.

## Detalles de implementación
- Solo se procesan las velas terminadas, emulando la comprobación de nueva barra de la versión MQL original.
- La estrategia mantiene los últimos dos valores de la media móvil y el precio de cierre anterior en campos privados para evitar búsquedas en el historial del indicador.
- `IsFormedAndOnlineAndAllowTrading()` asegura que el trading ocurra solo cuando todos los flujos de datos estén activos y se permita el trading.
- Los registros de dirección de trade (`LogInfo`) proporcionan transparencia para depuración y monitoreo en vivo.
- La renderización de gráficos (si está disponible) dibuja las velas y ambas medias móviles para una rápida validación visual.

## Notas de uso
- Elija `Volume` según el tamaño del lote del instrumento. La estrategia siempre envía órdenes de mercado de tamaño `Volume + |Position|` para revertir eficientemente.
- Al ejecutar en instrumentos sin un `PriceStep` definido, el código recurre a un valor de `1`. Ajuste los parámetros en consecuencia si el tamaño del tick difiere.
- La optimización puede enfocarse en los períodos de la media móvil y las distancias protectoras para adaptar la estrategia a diferentes mercados.
- Combine con filtros adicionales (volatilidad, horarios de sesión, etc.) si es necesario. La estructura modular facilita su extensión.

## Rangos de optimización sugeridos
- `SlowMaLength`: 20 – 120 con paso 5–10.
- `FastMaLength`: 2 – 10 con paso 1.
- `StopLossPoints` / `TakeProfitPoints`: 50 – 200 dependiendo de la volatilidad del instrumento.

Estos rangos reflejan de cerca la configuración original del experto mientras brindan flexibilidad para otros instrumentos.
