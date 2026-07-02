# Estrategia Reversals With Pin Bars
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Esta estrategia es un port C# del asesor experto de MetaTrader **"Reversals With Pin Bars"**. El EA original busca velas de rechazo con mechas largas (pin bars) y las confirma con un filtro de momentum, una comprobación de tendencia mediante media móvil ponderada lineal (LWMA) de marco superior y un filtro direccional MACD. El port conserva esta estructura multimarco, se apoya exclusivamente en indicadores StockSharp y expone los controles de riesgo más importantes como parámetros.

La implementación se centra en la API StockSharp de alto nivel: las velas del marco principal impulsan las entradas, mientras suscripciones adicionales alimentan indicadores de marcos superiores. La gestión de riesgo se expresa en pips y admite automatización opcional de trailing-stop y break-even.

## Lógica de entrada
- **Detección de pin bar**: la vela anterior terminada debe tener una mecha que represente al menos el 50% de su rango completo.
  - Configuración larga: la sombra superior es dominante (coincide con la comprobación original de "hanging man").
  - Configuración corta: la sombra inferior es dominante.
- **Filtro de tendencia**: la LWMA rápida (longitud = `FastMaPeriod`) debe estar por encima/debajo de la LWMA lenta (`SlowMaPeriod`) en el marco superior.
- **Filtro de momentum**: la distancia absoluta del valor de momentum frente a 100 en cualquiera de las tres últimas barras del marco superior debe superar `MomentumThreshold`.
- **Filtro MACD**: la línea principal MACD debe estar por encima/debajo de la línea de señal en el marco MACD.
- **Límites de posición**: la exposición neta no puede superar `MaxTrades * Volume`. Las nuevas operaciones usan el ajuste alineado `Volume`.

## Gestión de riesgo
- **Stop-loss / Take-profit**: distancias fijas en pips (`StopLossPips`, `TakeProfitPips`) desde el cierre de entrada.
- **Break-even**: si está activado, el stop se mueve a `entry +/- BreakEvenOffsetPips` cuando el precio avanza `BreakEvenTriggerPips`.
- **Trailing stop**: si está activado, el trailing mantiene una distancia de `TrailingStopPips` desde el último cierre.
- **Aplanamiento automático**: alcanzar el stop o el objetivo calculado sale de toda la posición con una orden de mercado.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `TradeVolume` | Volumen usado para cada nueva entrada, alineado al paso del instrumento. |
| `MaxTrades` | Número máximo de entradas en la misma dirección (límite de volumen agregado). |
| `StopLossPips` | Distancia de stop-loss en pips. |
| `TakeProfitPips` | Distancia de take-profit en pips. |
| `EnableTrailing` / `TrailingStopPips` | Activa y configura la distancia del trailing-stop. |
| `EnableBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Activación de break-even y ajustes de búfer. |
| `FastMaPeriod` / `SlowMaPeriod` | Longitudes de las LWMA del marco superior. |
| `MomentumPeriod` / `MomentumThreshold` | Longitud de momentum y distancia absoluta mínima desde 100. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Configuración MACD para el filtro de largo plazo. |
| `CandleType` | Serie principal de velas para detectar pin bars. |
| `HigherCandleType` | Serie de velas usada para LWMAs y momentum. |
| `MacdCandleType` | Serie de velas usada para MACD. |

## Diferencias frente a la versión MetaTrader
- Se omitieron opciones de take-profit monetario, trailing monetario y stop de patrimonio; el riesgo se expresa mediante pips.
- Las confirmaciones de líneas fractales que requerían objetos de gráfico se reemplazaron por condiciones basadas en indicadores.
- Se eliminaron todas las notificaciones (alertas, correos, mensajes push); la versión StockSharp se centra en la lógica de trading.

## Notas de uso
1. Asigne la estrategia a una cartera e instrumento, luego ajuste los tres tipos de vela para su configuración multimarco deseada.
2. Asegúrese de que el paso de precio del instrumento refleje la definición de pip (valor alternativo predeterminado: 0.0001).
3. Inicie la estrategia; stops, objetivos, trailing y gestión de break-even se realizan automáticamente al cierre de vela.
4. Supervise los resultados; ajuste las longitudes de momentum y LWMA al perfil de volatilidad del instrumento.
