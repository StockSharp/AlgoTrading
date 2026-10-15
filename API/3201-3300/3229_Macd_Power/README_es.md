# Estrategia de MACD Power
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia MACD Power es un sistema de momentum multitemporal convertido del experto asesor MetaTrader original. La lógica combina un par de medias móviles linealmente ponderadas (LWMA) calculadas en el marco temporal principal, dos variantes de MACD, un filtro de momentum en un marco temporal superior y un sesgo MACD mensual. La estrategia intenta participar en movimientos impulsivos una vez que el momentum y las condiciones de tendencia del marco temporal superior se alinean.

## Lógica principal
- **Medias móviles principales** – Una LWMA rápida y una lenta del precio típico de la vela (\((High + Low + Close) / 3\)). La estrategia requiere que la media rápida esté por debajo de la lenta antes de considerar cualquier señal, imitando el código original que espera retrocesos dentro de una pendiente bajista dominante antes de entrar en la dirección del sesgo mensual.
- **Confirmación dual de MACD** – Dos indicadores MACD con parámetros `(12, 26, 1)` y `(6, 13, 1)` deben estar ambos por encima de cero para operaciones largas o por debajo de cero para cortas. Estos valores reproducen las condiciones `MacdMAIN1` y `MacdMAIN2` del experto MQL que miden la aceleración a corto plazo.
- **Filtro de momentum** – El Momentum (longitud 14) se calcula en un marco temporal superior derivado del tamaño de la vela principal (p.ej., base de 15 minutos → momentum de 1 hora). La distancia absoluta desde 100 se monitoriza en las tres últimas lecturas de momentum; al menos una debe superar el umbral configurado para confirmar que el precio se mueve decisivamente.
- **Sesgo MACD mensual** – Un MACD mensual `(12, 26, 9)` (idéntico a `MacdMAIN0`/`MacdSIGNAL0` en el EA) debe tener su línea principal por encima de la línea de señal para operaciones largas y por debajo para cortas. Esto protege contra operar en contra de la tendencia macro dominante.

## Gestión de operaciones
- **Dimensionamiento de entrada** – El parámetro `OrderVolume` define el tamaño base de la orden. Cuando se requiere una reversión de posición, el motor añade automáticamente la magnitud de la posición opuesta para que el volumen neto se invierta en una sola orden de mercado.
- **Take profit / stop loss** – Las distancias absolutas se expresan en puntos del instrumento y se convierten a precio usando `Security.PriceStep` (con un respaldo seguro de `1`).
- **Trailing stop** – Una vez que el precio se mueve a favor en `TrailingActivationPoints`, el stop rastrea el precio más alto (largo) o más bajo (corto) con un offset definido por `TrailingOffsetPoints`.
- **Punto de equilibrio** – Cuando el precio alcanza `BreakEvenTriggerPoints`, se activa un stop de punto de equilibrio sintético en `Entrada ± BreakEvenOffsetPoints`. Si el precio retrocede a ese nivel, la posición se cierra.
- **Límite de operaciones** – `MaxTrades` limita el número de inicios de posición por ejecución; una vez alcanzado el umbral, no se emiten nuevas entradas.

## Parámetros
| Nombre | Descripción | Por defecto |
| --- | --- | --- |
| `CandleType` | Marco temporal principal para la generación de señales. | Velas de 15 minutos |
| `FastMaLength` | Longitud de la LWMA rápida (precio típico). | 6 |
| `SlowMaLength` | Longitud de la LWMA lenta (precio típico). | 85 |
| `MomentumLength` | Período de retroceso del Momentum en el marco temporal superior. | 14 |
| `MomentumBuyThreshold` | Distancia mínima absoluta desde 100 requerida para momentum alcista. | 0.3 |
| `MomentumSellThreshold` | Distancia mínima absoluta desde 100 requerida para momentum bajista. | 0.3 |
| `TakeProfitPoints` | Distancia del take-profit en puntos del instrumento. | 50 |
| `StopLossPoints` | Distancia del stop-loss en puntos del instrumento. | 20 |
| `TrailingActivationPoints` | Beneficio (puntos) requerido antes de que active el trailing. | 40 |
| `TrailingOffsetPoints` | Brecha (puntos) entre el trailing stop y el precio extremo. | 40 |
| `BreakEvenTriggerPoints` | Beneficio (puntos) que activa la protección de punto de equilibrio. | 30 |
| `BreakEvenOffsetPoints` | Desplazamiento (puntos) aplicado al mover el stop al punto de equilibrio. | 30 |
| `MaxTrades` | Número máximo de operaciones permitidas por sesión. | 10 |
| `OrderVolume` | Volumen base de la orden. | 1 |

## Diferencias respecto al experto MQL
- La estrategia usa la API de alto nivel de StockSharp (`SubscribeCandles` + `Bind/BindEx`) en lugar del sondeo directo de ticks. Los valores de los indicadores solo se procesan tras el cierre de las velas.
- Los bloques de trailing basado en dinero y stop por capital del código original no están portados porque la gestión monetaria a nivel de cuenta se maneja normalmente por el framework de riesgo de StockSharp. En su lugar, se mantienen el trailing y el punto de equilibrio basados en puntos y pueden configurarse para emular el comportamiento del EA.
- Las alertas, notificaciones y asistentes de modificación manual de órdenes de MQL se omiten; el motor de StockSharp gestiona las órdenes directamente mediante llamadas de mercado.

## Notas de uso
1. Elija el marco temporal principal configurando `CandleType`. El momentum del marco temporal superior y el MACD mensual se derivan automáticamente según el mapeo implementado en `GetMomentumCandleType()`.
2. Alinee `TakeProfitPoints`, `StopLossPoints` y los parámetros de trailing/punto de equilibrio con el tamaño de tick del instrumento. Los valores por defecto reflejan la configuración Forex de 5 dígitos del EA pero pueden adaptarse a otros mercados.
3. Monitorice el contador `MaxTrades` al ejecutar backtests automatizados; establézcalo en un número grande si se desea el comportamiento de apilamiento tipo martingala del EA original.
4. Para análisis visual, active los gráficos en la GUI – la implementación dibuja velas y las dos curvas LWMA por defecto.
