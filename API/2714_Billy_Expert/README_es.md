# Billy Expert Comprador de Retrocesos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Billy Expert es una estrategia de retrocesos solo larga convertida del Asesor Experto MetaTrader 5 «Billy expert». Espera una secuencia de máximos decrecientes y abre en el marco temporal base, luego verifica confirmaciones alcistas de dos osciladores Estocásticos calculados en diferentes marcos temporales superiores. Cuando ambos osciladores coinciden en que hay impulso alcista presente, el sistema agrega una nueva posición larga, hasta un límite configurable.

La conversión sigue las pautas de la API de alto nivel de StockSharp. El volumen de la operación, el máximo de entradas simultáneas, los stops protectores y los take profits se controlan a través de los parámetros de la estrategia para que el comportamiento coincida con la lógica MQL original.

## Cómo funciona
1. Suscribirse a la serie de velas primaria (por defecto 1 minuto) y dos marcos temporales superiores para los osciladores Estocásticos (por defecto 5 y 6 minutos).
2. Rastrear las últimas cuatro velas completadas en el marco temporal base. Un retroceso válido requiere máximos *y* aperturas estrictamente decrecientes a lo largo de esas cuatro barras.
3. Evaluar los osciladores Estocásticos rápido y lento. La estrategia exige que para cada oscilador tanto el último como el anterior valor de %K se mantenga por encima de %D, señalando que el impulso ya se ha invertido al alza en ambos marcos temporales.
4. Si el retroceso y los filtros de impulso confirman y el número de operaciones largas abiertas está por debajo de `MaxPositions`, enviar una orden de compra a mercado con tamaño `TradeVolume`.
5. Los niveles opcionales de stop-loss y take-profit, expresados en pips, se convierten en distancias de precio absolutas usando el `PriceStep` del instrumento. Si cualquier distancia se establece en cero, la orden protectora correspondiente se omite.
6. Las posiciones se cierran solo a través de esos niveles protectores, imitando el comportamiento del asesor experto original.

## Parámetros
- `TradeVolume` – tamaño de la orden para cada entrada (predeterminado `0.01`).
- `StopLossPips` – distancia de stop en pips (predeterminado `0`, deshabilitado).
- `TakeProfitPips` – objetivo de ganancia en pips (predeterminado `32`).
- `MaxPositions` – máximo de operaciones largas simultáneas (predeterminado `6`).
- `Signal Candle` – marco temporal base usado para patrones de precio (predeterminado `1` minuto).
- `Fast Stochastic TF` – marco temporal para el oscilador rápido (predeterminado `5` minutos).
- `Slow Stochastic TF` – marco temporal para el oscilador lento (predeterminado `6` minutos). Debe ser más largo que el marco temporal rápido.

## Filtros y comportamiento
- **Dirección**: Solo largos.
- **Disparador de entrada**: Retroceso de cuatro barras con aperturas y máximos decrecientes.
- **Filtro de impulso**: Doble oscilador Estocástico con %K por encima de %D en las lecturas actual y anterior.
- **Gestión de riesgos**: Stop-loss y take-profit opcionales basados en pips. Sin lógica de trailing.
- **Dimensionamiento de posición**: `TradeVolume` fijo por entrada, limitado por `MaxPositions`.
- **Mercados**: Diseñado para pares de forex cotizados con pips fraccionarios, pero funciona con cualquier instrumento que proporcione un `PriceStep` válido.

## Notas de uso
- Asegúrese de que `Fast Stochastic TF` sea estrictamente más corto que `Slow Stochastic TF`, de lo contrario la estrategia se detiene al iniciar.
- Dado que las salidas dependen únicamente de las órdenes protectoras, ajuste `StopLossPips` y `TakeProfitPips` a la volatilidad del instrumento.
- La estrategia ignora las señales bajistas y no reduce parcialmente; use controles de riesgo a nivel de cartera para protección adicional.
- Para backtesting, proporcione suficientes velas de calentamiento para que ambos osciladores Estocásticos puedan formarse antes de la primera operación.
