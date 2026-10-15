# Estrategia de cuero cabelludo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Gonna Scalp es un asesor experto MetaTrader de alta frecuencia trasladado al StockSharp alto nivel API. El sistema busca entradas rápidas de reversión a la media en un gráfico a corto plazo respetando la tendencia dominante del mercado. La confirmación se produce mediante un mecanismo de votación que evalúa el impulso, CCI, ATR, el oscilador estocástico y los filtros MACD antes de permitir una operación. Solo se puede abrir una posición a la vez y cada operación está protegida por distancias fijas de stop-loss y take-profit expresadas en MetaTrader puntos.

## Lógica de trading

1. **Preparación de indicadores**
   - Promedios móviles ponderados (WMA) rápidos y lentos calculados sobre el precio típico.
   - El impulso (período 14) se evalúa en el período de negociación y se convierte en una distancia absoluta desde el valor neutral 100.
   - Índice de canal de productos básicos (período 20) y rango verdadero promedio (período 12) utilizados como filtros direccionales.
   - Stochastic oscilador %K/%D (5/3/3) y MACD (26/12/9) procesados en la misma serie de velas.
2. **Señal de votación**
   - Cada indicador aporta un voto para el lado alcista o bajista cuando su lectura actual respalda la tendencia identificada en el código original MetaTrader.
   - La estrategia recopila tres distancias de impulso recientes y requiere que al menos una de ellas supere un umbral configurable antes de permitir una nueva operación.
   - Las comprobaciones estructurales adicionales exigen que el mínimo de la barra de hace dos velas permanezca por debajo del máximo de la barra anterior para los largos (condición espejo para los cortos).
3. **Ejecución de orden**
   - Cuando los votos alcistas superan a los bajistas y todos los filtros coinciden, la estrategia abre una posición larga utilizando el tamaño de lote configurado.
   - Cuando los votos bajistas dominan a los votos alcistas y el filtro de impulso lo aprueba, se abre una posición corta.
4. **Gestión de riesgos**
   - Cada operación abierta va acompañada de distancias fijas de stop-loss y take-profit medidas en MetaTrader puntos y traducidas a incrementos del precio del instrumento.
   - La lógica protectora cierra la posición en la vela actual una vez que se ha superado cualquiera de los niveles.

## Parámetros clave

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `TradeVolume` | Tamaño base del pedido en lotes después de la alineación del volumen. | `0.01` |
| `FastMaPeriod` | Longitud del filtro WMA rápido. | `1` |
| `SlowMaPeriod` | Longitud del filtro WMA lento. | `5` |
| `MomentumPeriod` | Número de barras utilizadas por el indicador de impulso. | `14` |
| `MomentumBuyThreshold` | Desviación de impulso absoluta mínima requerida para entradas largas. | `0.3` |
| `MomentumSellThreshold` | Desviación de impulso absoluta mínima requerida para entradas cortas. | `0.3` |
| `StopLossSteps` | Distancia de stop-loss expresada en MetaTrader puntos. | `200` |
| `TakeProfitSteps` | Distancia de obtención de beneficios expresada en MetaTrader puntos. | `200` |
| `CandleType` | Marco de tiempo utilizado para todos los indicadores (el valor predeterminado es velas de 5 minutos). | `M5` |

## Notas de uso

- Alinear el volumen de la estrategia con el instrumento negociado ajustando `TradeVolume`; la implementación lo normaliza automáticamente al paso del lote de intercambio.
- Los parámetros stop-loss y take-profit operan en MetaTrader puntos. Se convierten a unidades de precio del instrumento según la precisión del instrumento.
- Se requieren al menos tres velas completas antes de que la lógica de votación pueda producir señales debido al búfer del historial de impulso.
- La estrategia evita deliberadamente la piramidalidad; no se abre una nueva operación hasta que la posición anterior haya sido cerrada mediante gestión de riesgos o una señal opuesta.
- Puede conectar la estrategia a gráficos StockSharp para visualizar las series WMA, estocásticas y MACD para la validación de señales.
