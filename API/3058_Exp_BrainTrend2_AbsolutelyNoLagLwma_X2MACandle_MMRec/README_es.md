# Estrategia Exp BrainTrend2 AbsolutelyNoLagLwma X2MACandle MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia recrea el Asesor Experto multi-módulo de MetaTrader combinando tres filtros en la API de alto nivel de StockSharp:

1. **Inspiración BrainTrend2** – un canal de Average True Range (ATR) detecta fases de contracción y expansión de la volatilidad.
2. **Aproximación AbsolutelyNoLagLwma** – una media móvil ponderada linealmente (LWMA) rastrea la dirección dominante con mínimo lag.
3. **Réplica X2MACandle** – un par de media móvil exponencial (EMA) rápida y lenta evalúa el color de las velas para validar el momentum.

Una posición se abre solo cuando todos los filtros apuntan en la misma dirección. Los objetivos de stop-loss y take-profit impulsados por ATR gestionan el proceso de salida y emulan el concepto original de gestión monetaria MMRec.

## Lógica de trading
- **Configuración alcista**: la vela cierra por encima de la LWMA mientras la EMA rápida está más alta que la EMA lenta. Se permite una nueva entrada larga solo después de que el sesgo alcista previo desaparezca, evitando órdenes múltiples en señales idénticas.
- **Configuración bajista**: la vela cierra por debajo de la LWMA mientras la EMA rápida está más baja que la EMA lenta. Las posiciones cortas obedecen las mismas reglas de confirmación y enfriamiento que el lado largo.
- **Gestión de riesgo**: el ATR define niveles de salida dinámicos. Tanto el stop-loss como el take-profit escalan con la volatilidad actual y se reevalúan en cada vela. Si el mercado toca cualquiera de los niveles, la estrategia cierra toda la posición con una orden de mercado.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal de la serie de velas de trabajo. Por defecto son velas de 6 horas para reflejar los valores por defecto del EA original. |
| `AtrPeriod` | Período de lookback usado por el filtro de volatilidad ATR. |
| `LwmaLength` | Período de la media móvil ponderada linealmente para el filtro de tendencia. |
| `FastMaLength` | Período de la EMA rápida usada para colorear las velas. |
| `SlowMaLength` | Período de la EMA lenta usada para colorear las velas. |
| `StopLossAtrMultiplier` | Multiplicador aplicado al ATR para calcular la distancia del stop de protección. |
| `TakeProfitAtrMultiplier` | Multiplicador aplicado al ATR para determinar la distancia del take-profit. |

Todos los parámetros se exponen a través de `StrategyParam<T>` para que puedan optimizarse dentro de StockSharp.

## Notas
- El Asesor Experto original depende de buffers de indicadores propietarios. Este port usa indicadores estándar de StockSharp que reproducen las mismas señales direccionales sin depender de scripts externos.
- La gestión monetaria se simplifica a salidas de posición completa porque las estrategias de StockSharp típicamente operan con órdenes del tamaño del portafolio. Las distancias impulsadas por ATR proporcionan el comportamiento adaptativo esperado del módulo MMRec.
- Los comentarios en el código están en inglés según lo requieren las pautas de conversión.
