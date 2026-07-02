# Estrategia Eliot Wave (adaptada desde MQL4 "Eliot Wave I")
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **estrategia Eliot Wave** es una adaptación a la API de StockSharp del asesor experto original de MetaTrader 4 "Eliot Wave I". El sistema combina un cruce rápido/lento de media móvil ponderada lineal (LWMA) con confirmación de momentum multitemporal y un filtro MACD muy lento. El objetivo es identificar movimientos impulsivos en la dirección de la tendencia predominante, manteniendo el riesgo limitado mediante reglas de protección incorporadas.

## Indicadores principales

- **LWMA rápida (predeterminado 6)** - rastrea la dirección de corto plazo usando el precio típico `(High + Low + Close) / 3`.
- **LWMA lenta (predeterminado 85)** - mide la tendencia más amplia en el mismo marco temporal.
- **Momentum (período predeterminado 14)** - se evalúa en un marco temporal superior y se convierte en una desviación respecto al nivel neutral `100`. Una lectura por encima del umbral configurado indica un impulso suficientemente fuerte.
- **MACD (12, 26, 9)** - se calcula en un marco temporal muy lento (mensual por defecto) y se usa como filtro de largo plazo. La estrategia solo compra cuando la línea principal MACD está por encima de la línea de señal y vende cuando está por debajo.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | --------------- |
| `Base Candle` | Marco temporal principal para el procesamiento LWMA. | Velas de 15 minutos |
| `Momentum Candle` | Marco temporal superior usado para la confirmación de momentum. | Velas de 1 hora |
| `MACD Candle` | Marco temporal muy lento para el filtro de tendencia MACD. | Velas de 30 días |
| `Fast LWMA` | Longitud de la media móvil ponderada lineal rápida. | 6 |
| `Slow LWMA` | Longitud de la media móvil ponderada lineal lenta. | 85 |
| `Momentum Period` | Período retrospectivo del indicador de momentum en el marco temporal de confirmación. | 14 |
| `Momentum Buy Threshold` | Desviación mínima por encima de 100 necesaria para validar una configuración larga. | 0.3 |
| `Momentum Sell Threshold` | Desviación mínima por encima de 100 necesaria para validar una configuración corta. | 0.3 |
| `Stop Loss (pts)` | Distancia del stop de protección expresada en puntos del instrumento. | 20 |
| `Take Profit (pts)` | Distancia del objetivo expresada en puntos del instrumento. | 50 |
| `Trade Volume` | Tamaño de orden para cada entrada. | 1 lote |
| `Max Position` | Exposición neta absoluta permitida; evita que la estrategia supere el límite `Max_Trades` del EA MQL. | 10 lotes |

Todos los parámetros están implementados como `StrategyParam<T>` para que puedan optimizarse directamente en Designer o Runner.

## Reglas de trading

1. **Filtro de tendencia y estructura**
   - La LWMA rápida debe permanecer por encima de la LWMA lenta para considerar operaciones largas.
   - La LWMA rápida debe permanecer por debajo de la LWMA lenta para considerar cortos.
   - Las dos últimas velas completadas deben solaparse (`Low[2] < High[1]` para compras, `Low[1] < High[2]` para ventas), replicando el requisito de consolidación del EA.
2. **Confirmación de momentum**
   - El momentum del marco temporal superior se transforma en valores `abs(momentum - 100)`.
   - Si cualquiera de los tres últimos valores supera el umbral configurado, el impulso se considera válido.
3. **Filtro de tendencia macro**
   - Las compras requieren que la línea principal MACD esté por encima de la línea de señal en el marco temporal lento.
   - Las ventas requieren que la línea principal MACD esté por debajo de la línea de señal.
4. **Ejecución de órdenes**
   - Cuando todas las condiciones coinciden, la estrategia envía una orden de mercado dimensionada para invertir la posición actual y añadir el volumen de operación configurado.
   - Se admiten giros de posición para que el comportamiento coincida con la lógica de promediado del EA original.

## Gestión de riesgos

- `StartProtection` aplica automáticamente distancias de stop-loss y take-profit en puntos del instrumento.
- La lógica adicional de salida cierra posiciones largas cuando la LWMA rápida cae por debajo de la LWMA lenta o cuando el filtro MACD se vuelve bajista (y viceversa para cortos). Esto refleja los bloques de salida MQL.
- El parámetro `Max Position` evita que la estrategia acumule exposición más allá del límite configurado, respetando la restricción `Max_Trades` del EA.

## Diferencias con el EA original

- Las comprobaciones gráficas de líneas de tendencia y las notificaciones manuales de operación se eliminaron porque son específicas de MetaTrader y no tienen equivalente en StockSharp.
- Las variantes de break-even y trailing-stop complejo del script MQL se sustituyen por el mecanismo más simple `StartProtection`. Los usuarios pueden ampliar la estrategia si necesitan esos comportamientos.
- La protección de equity basada en dinero no está implementada; el riesgo se controla mediante stops fijos y el límite de posición.

## Notas de uso

1. Conecte la estrategia a un instrumento líquido y asegúrese de que los tres flujos de velas estén disponibles.
2. Ajuste `Trade Volume`, las distancias de stop/objetivo y los umbrales según la volatilidad del mercado operado.
3. Optimice los umbrales por separado para impulsos alcistas y bajistas si el instrumento muestra comportamiento asimétrico.
4. Considere habilitar las visualizaciones incorporadas del gráfico (velas, LWMAs, marcadores de operaciones) para depurar con mayor facilidad.

Esta adaptación se centra en reproducir la lógica de señales del EA original usando la API de alto nivel de StockSharp, manteniendo una implementación idiomática y fácil de mantener.
