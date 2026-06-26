# Estrategia de Accelerator Trailing TP & SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Accelerator Trailing TP & SL porta el Asesor Experto "Accelerator Trailing TP&SL" de MetaTrader a la API de alto nivel de StockSharp. El sistema mezcla el Oscilador Acelerador de Bill Williams con la confirmación de impulso multitemporal y un filtro de tendencia MACD mensual. Las entradas se diseñan con dimensionamiento de posición geométrico mientras que las salidas combinan distancias clásicas de stop/objetivo, seguimiento adaptativo y lógica de break-even.

## Lógica de trading
- **Filtro de impulso** – un indicador de Momentum de 14 períodos calculado en un marco temporal superior debe desviarse del nivel neutro de 100 al menos por el umbral configurado en cualquiera de las últimas tres barras completadas.
- **Oscilador Acelerador** – las operaciones largas requieren una lectura positiva del acelerador, las operaciones cortas requieren una lectura negativa en el marco temporal de señal.
- **Medias móviles** – una media móvil ponderada lineal rápida (LWMA) debe estar por encima de la LWMA lenta para largos y por debajo de ella para cortos, aproximando el filtro de tendencia rápido/lento original.
- **Tendencia MACD mensual** – de forma predeterminada el filtro observa velas mensuales. Las operaciones largas demandan que la línea MACD esté por encima de la línea de señal (incluso cuando ambos valores son negativos), mientras que las operaciones cortas requieren la condición opuesta.
- **Entradas escalonadas** – la estrategia puede piramidizar hasta el número máximo configurado de posiciones por dirección. Cada entrada adicional se multiplica por el exponente de lote, recreando el dimensionamiento de estilo martingala utilizado en el programa MQL.

## Gestión del riesgo
- **Stop-loss / take-profit estático** – las distancias en pips reflejan los ajustes originales de Stop Loss y Take Profit.
- **Trailing stop** – cuando está habilitado, la estrategia sigue el precio más favorable por el número de pips configurado.
- **Movimiento de break-even** – después de que una operación alcanza la distancia de disparo, el stop avanza por el desplazamiento especificado, protegiendo las ganancias acumuladas.
- **Salida MACD** – cuando el filtro MACD se vuelve en contra de la posición activa, la estrategia puede cerrar todas las posiciones de inmediato, coincidiendo con el ayudante de salida manual en el código MQL.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `FastMaLength` / `SlowMaLength` | Períodos de las LWMAs rápida y lenta en el marco temporal de trading. |
| `MomentumThreshold` | Desviación absoluta mínima del momentum del valor neutro de 100 en el marco temporal superior. |
| `StopLossPips` / `TakeProfitPips` | Distancias de stop de protección y objetivo en pips. |
| `TrailingStopPips` | Distancia utilizada por el gestor de trailing stop opcional. |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Define cuándo y cómo se mueve el stop al break-even. |
| `MaxTrades` | Número máximo de entradas escalonadas por dirección. |
| `BaseVolume` | Volumen de la primera orden en una secuencia. |
| `LotExponent` | Multiplicador aplicado a cada entrada escalonada adicional. |
| `EnableTrailing` | Habilita o deshabilita la gestión del trailing stop. |
| `UseBreakEven` | Habilita o deshabilita el movimiento del stop de break-even. |
| `CloseOnMacdFlip` | Cierra todas las operaciones si el MACD del marco temporal superior se invierte. |
| `CandleType` | Serie de velas principal para señales (predeterminado: 15 minutos). |
| `MomentumCandleType` | Velas de marco temporal superior utilizadas por el filtro de impulso (predeterminado: 1 hora). |
| `MacdCandleType` | Serie de velas utilizada para el filtro de tendencia MACD (predeterminado: velas mensuales). |

## Notas
- La estrategia depende del `PriceStep` del instrumento para convertir los ajustes de riesgo basados en pips a distancias de precio. Asegúrese de que los metadatos del valor estén poblados al ejecutar la estrategia.
- Dado que StockSharp usa posiciones netas, las entradas escalonadas adicionales se abren enviando órdenes de mercado repetidamente hasta que se alcanza el máximo configurado. Las salidas cierran toda la posición neta, coincidiendo con las rutinas "cerrar todo" en el experto original.
- El marco temporal MACD mensual puede ajustarse a través del parámetro `MacdCandleType` para adaptarse a diferentes instrumentos o backtests.
