# Estrategia de Harami
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
HaramiStrategy convierte el asesor experto "Harami" de MetaTrader en la API de alto nivel de StockSharp. La estrategia combina un patrón Harami alcista/bajista detectado en un marco temporal superior con expansión de momentum y un filtro MACD de largo plazo. Solo se procesan velas completadas y toda la gestión de operaciones se realiza a través del motor de protección integrado de StockSharp.

## Datos e indicadores
- **Marco temporal base:** configurable (velas de 15 minutos por defecto) para la detección de tendencia mediante medias móviles.
- **Marco temporal superior:** configurable (por defecto una hora) para el reconocimiento de patrones y la confirmación de momentum.
- **Marco temporal MACD:** configurable (por defecto velas de 30 días) para emular el filtro MACD mensual original.
- **Indicadores:**
  - Media Móvil Ponderada Linealmente (`FastMaLength`) en el marco temporal base.
  - Media Móvil Exponencial (`SlowMaLength`) en el marco temporal base.
  - Momentum (`MomentumPeriod`) en el marco temporal superior. La estrategia utiliza la distancia absoluta desde el valor neutral (100) para las últimas tres barras del marco temporal superior.
  - Convergencia/Divergencia de Medias Móviles (12/26/9) en el marco temporal MACD.

## Configuración larga
1. La EMA lenta está por encima de la LWMA rápida en el marco temporal base, señalando una tendencia alcista.
2. El marco temporal superior forma una secuencia Harami alcista: hace dos velas fue bajista, la vela anterior fue alcista y su cuerpo es más pequeño que el cuerpo bajista anterior.
3. Cualquiera de las últimas tres desviaciones de momentum del marco temporal superior supera `MomentumBuyThreshold`.
4. La línea principal MACD está por encima de la línea de señal en el marco temporal MACD.
5. No hay posición larga abierta (`Position <= 0`).
6. La estrategia envía una orden de compra a mercado dimensionada para revertir cualquier exposición corta y añadir `Volume` lotes.

## Configuración corta
1. La EMA lenta está por debajo de la LWMA rápida en el marco temporal base.
2. El marco temporal superior forma un Harami bajista: hace dos velas fue alcista, la vela anterior fue bajista y el último cuerpo es más pequeño.
3. Cualquiera de las últimas tres desviaciones de momentum del marco temporal superior supera `MomentumSellThreshold`.
4. La línea principal MACD está por debajo de la línea de señal.
5. No hay exposición corta abierta (`Position >= 0`).
6. La estrategia envía una orden de venta a mercado suficientemente grande para cerrar posiciones largas y abrir una nueva posición corta de tamaño `Volume`.

## Gestión de riesgo
`StartProtection` instala niveles de stop-loss y take-profit (expresados en puntos). Las características adicionales de trailing, break-even y gestión monetaria del EA original se omiten intencionalmente para mantener la versión StockSharp concisa. Los cambios de dirección de operación aplanan automáticamente la exposición opuesta.

## Parámetros
| Nombre | Descripción | Valor predeterminado |
| ---- | ----------- | ------- |
| `CandleType` | Marco temporal primario para medias móviles y ejecución de señales. | Velas de 15 minutos |
| `HigherCandleType` | Marco temporal utilizado para la confirmación de Harami y momentum. | Velas de 1 hora |
| `MacdCandleType` | Marco temporal para el filtro de tendencia MACD. | Velas de 30 días |
| `FastMaLength` | Longitud de la MA ponderada linealmente rápida. | 6 |
| `SlowMaLength` | Longitud de la MA exponencial lenta. | 85 |
| `MomentumPeriod` | Período de retroceso del Momentum en el marco temporal superior. | 14 |
| `MomentumBuyThreshold` | Desviación mínima de momentum para confirmación larga. | 0.3 |
| `MomentumSellThreshold` | Desviación mínima de momentum para confirmación corta. | 0.3 |
| `StopLossPoints` | Distancia de stop-loss en puntos. | 40 |
| `TakeProfitPoints` | Distancia de take-profit en puntos. | 100 |

## Consejos de uso
- Alinear `CandleType`, `HigherCandleType` y `MacdCandleType` con los datos históricos disponibles; asegurarse de que el marco temporal superior sea más largo que el marco temporal base.
- Ajustar los umbrales de momentum para que coincidan con la volatilidad del instrumento negociado.
- Usar el optimizador de StockSharp a través de los rangos de parámetros proporcionados para ajustar las longitudes de MA y los umbrales de momentum.
- Siempre realizar backtesting con configuraciones realistas de comisión/latencia antes de implementar en vivo.
