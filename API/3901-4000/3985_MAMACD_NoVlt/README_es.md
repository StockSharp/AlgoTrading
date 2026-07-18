# MAMACD Estrategia sin volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
MAMACD No Volatility es un puerto directo del asesor experto MetaTrader 4 `MAMACD_novlt.mq4`. La estrategia combina tres promedios móviles calculados sobre los mínimos de las velas y cierra con un filtro de impulso MACD. Espera hasta que el EMA rápido caiga por debajo (para largos) o suba por encima (para cortos) dos filtros LWMA de base baja, arma una configuración pendiente y activa una entrada solo después de que la línea principal MACD confirme el cambio de impulso.

## Indicadores
- **EMA rápida** (`FastEmaPeriod`) calculado sobre precios de cierre.
- **Primera LWMA** (`FirstLowWmaPeriod`) calculada sobre precios bajos.
- **Segunda LWMA** (`SecondLowWmaPeriod`) calculada sobre precios bajos.
- **MACD línea principal** con período rápido `FastSignalEmaPeriod` y período lento `SlowEmaPeriod`.

Todos los indicadores operan en el marco de tiempo definido por `CandleType` (predeterminado: velas de 5 minutos).

## Parámetros
| Parámetro | Descripción | Predeterminado |
| --- | --- | --- |
| `FirstLowWmaPeriod` | Período de la primera LWMA construida a partir de mínimos de velas. | 85 |
| `SecondLowWmaPeriod` | El período de la segunda LWMA se construyó a partir de los mínimos de las velas. | 75 |
| `FastEmaPeriod` | Período del EMA rápida construido a partir del cierre de velas. | 5 |
| `SlowEmaPeriod` | EMA período lento para el cálculo MACD. | 26 |
| `FastSignalEmaPeriod` | Periodo EMA rápida para el cálculo MACD. | 15 |
| `StopLossPoints` | Distancia del stop-loss en pasos de precio (0 desactiva el stop-loss). | 15 |
| `TakeProfitPoints` | Distancia de toma de ganancias en pasos de precio (0 desactiva la toma de ganancias). | 15 |
| `TradeVolume` | Volumen de órdenes utilizado para las entradas al mercado. | 0.1 |
| `CandleType` | Serie de velas utilizadas para todos los indicadores. | plazo de 5 minutos |

## Reglas de trading
1. **Configuración de brazo largo**: EMA rápida está debajo de ambos filtros LWMA.
2. **Configuración de armado corto**: EMA rápida está por encima de ambos filtros LWMA.
3. **Ingrese largo**:
   - El EMA rápida vuelve a cruzar por encima de ambas LWMA,
   - Previamente se armó un largo setup,
   - MACD la línea principal es positiva o ha aumentado en comparación con el valor anterior,
   - La posición neta actual no es larga.
4. **Ingrese breve**:
   - Fast EMA vuelve a cruzar por debajo de ambas LWMA,
   - Previamente se armó un setup corto,
   - MACD la línea principal es negativa o ha disminuido en comparación con el valor anterior,
   - La posición neta actual no es corta.
5. **Gestión de riesgos**: las opciones de take-profit y stop-loss se aplican automáticamente a través del servicio de protección integrado.

La estrategia no implementa una señal de salida dedicada; las posiciones se gestionan mediante los niveles configurados de stop-loss/take-profit o intervención manual.

## Notas
- La confirmación MACD replica la lógica MQL: la línea principal debe estar por encima de cero o subiendo (para largos) o por debajo de cero o bajando (para cortos).
- Los cálculos de la LWMA utilizan precios mínimos de velas para reflejar la configuración original del indicador.
- La escala de volumen refleja el EA original utilizando el parámetro `TradeVolume` para cada pedido.
