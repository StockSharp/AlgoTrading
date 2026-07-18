# Estrategia Pinbar Reversión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida del asesor experto MQL original `PINBAR.mq4` (carpeta `MQL/22269`). La estrategia detecta reversiones de pin bar en el marco temporal primario y las confirma con momentum y filtros MACD de marcos temporales superiores. Reproduce el espíritu del sistema fuente mientras usa las características de la API de alto nivel de StockSharp.

## Lógica de trading

- **Marco temporal primario** – tipo de vela configurable usado para identificar patrones de acción del precio.
- **Marco temporal superior** – tipo de vela configurable usado para confirmar el sesgo de momentum y tendencia MACD.
- **Detección de pin bar** – una barra se acepta cuando el cuerpo real es pequeño en relación con el rango completo y una mecha domina la vela (ratios de cuerpo y mecha configurables).
- **Filtro de tendencia** – la EMA rápida debe estar por encima (o por debajo) de la EMA lenta para configuraciones largas (o cortas), reflejando los filtros LWMA del código original.
- **Confirmación de momentum** – el momentum en el marco temporal superior debe estar por encima (largo) o por debajo (corto) de un umbral configurable para al menos uno de los últimos tres bars del marco temporal superior.
- **Confirmación MACD** – el valor MACD debe estar por encima de su línea de señal para operaciones largas y por debajo de la línea de señal para cortos, coincidiendo con la confirmación MACD mensual usada en el experto MQL.
- **Confirmación fractal** – la estrategia mantiene una ventana deslizante de cinco barras y requiere la presencia del último fractal alcista/bajista antes de aceptar una nueva operación, similar al gate `FindFractals()` en la fuente.
- **Gestión de riesgo** – stop-loss, take-profit, disparador de break-even/offset y lógica de trailing stop configurables rastrean la posición abierta. La operación se cierra cuando cualquier nivel es tocado o cuando el nivel de trailing es violado.

## Reglas de entrada

### Configuración larga
1. La última vela en el marco temporal primario forma un pin bar alcista (mecha inferior larga, cuerpo pequeño).
2. EMA rápida > EMA lenta.
3. El último momentum del marco temporal superior (o uno de los dos valores anteriores) está por encima del umbral.
4. El MACD del marco temporal superior está por encima de su línea de señal.
5. Se ha detectado un fractal alcista recientemente y el precio no lo ha invalidado.
6. La estrategia está plana o corta (los cortos se revierten).

### Configuración corta
1. La última vela en el marco temporal primario forma un pin bar bajista (mecha superior larga, cuerpo pequeño).
2. EMA rápida < EMA lenta.
3. El último momentum del marco temporal superior (o uno de los dos valores anteriores) está por debajo del umbral negativo.
4. El MACD del marco temporal superior está por debajo de su línea de señal.
5. Se ha detectado un fractal bajista recientemente y el precio no lo ha invalidado.
6. La estrategia está plana o larga (los largos se revierten).

## Reglas de salida

- Stop-loss y take-profit se expresan en porcentaje relativo al precio de entrada.
- El break-even se activa una vez que el precio se mueve por el porcentaje de disparo; el stop se mueve a entrada más/menos un offset.
- El trailing stop se activa después de que se logra el porcentaje de activación y sigue al precio a la distancia configurada.
- Las señales opuestas también revierten la posición.

## Parámetros

| Nombre | Por defecto | Descripción |
| --- | --- | --- |
| `CandleType` | Velas de 15 minutos | Marco temporal primario para detección de patrones. |
| `TrendCandleType` | Velas de 1 hora | Marco temporal superior para filtros de momentum/MACD. |
| `FastMaLength` | 6 | Longitud EMA rápida (reemplaza LWMA rápida). |
| `SlowMaLength` | 85 | Longitud EMA lenta (reemplaza LWMA lenta). |
| `MomentumLength` | 14 | Longitud del indicador de momentum en marco temporal superior. |
| `MomentumThreshold` | 0.1 | Valor mínimo absoluto de momentum para confirmación. |
| `MacdFastLength` | 12 | Longitud EMA rápida MACD. |
| `MacdSlowLength` | 26 | Longitud EMA lenta MACD. |
| `MacdSignalLength` | 9 | Longitud EMA de señal MACD. |
| `BodyToRangeRatio` | 0.3 | Tamaño máximo del cuerpo relativo al rango de la vela. |
| `WickRatio` | 0.6 | Ratio mínimo de mecha dominante que define un pin bar. |
| `StopLossPercent` | 2 | Tamaño del stop protector en porcentaje. |
| `TakeProfitPercent` | 4 | Tamaño del objetivo de beneficio en porcentaje. |
| `BreakEvenTriggerPercent` | 1.5 | Beneficio requerido para mover el stop a break-even. |
| `BreakEvenOffsetPercent` | 0.2 | Offset adicional añadido al stop de break-even. |
| `TrailingActivationPercent` | 2.5 | Umbral de beneficio para habilitar el trailing stop. |
| `TrailingDistancePercent` | 1 | Distancia del trailing stop una vez activado. |

## Notas

- El volumen está fijado en 1 contrato por defecto; ajuste el volumen de la estrategia para diferentes tamaños de posición.
- La detección fractal se reinicia cuando el precio viola el nivel fractal registrado, requiriendo un nuevo patrón antes de una nueva operación.
- Los rangos de optimización se incluyen para los parámetros clave para facilitar el backtesting y ajuste en StockSharp Designer.
