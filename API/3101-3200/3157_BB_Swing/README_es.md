# Estrategia de BB Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia BB Swing** es un port fiel del asesor experto de MetaTrader "BB SWING". Opera retrocesos de las Bandas de Bollinger alineados con la tendencia predominante definida por dos medias móviles ponderadas linealmente (LWMAs). Un filtro de Momentum de marco temporal superior y un MACD muy lento ayudan a confirmar la fortaleza de la reversión antes de abrir cualquier posición.

## Lógica de trading

1. Trabajar únicamente con velas terminadas del marco temporal `CandleType`.
2. Rastrear las últimas cuatro velas completadas para inspeccionar extremos recientes y cuerpos de velas.
3. Esperar que la LWMA rápida se mantenga por encima (para largos) o por debajo (para cortos) de la LWMA lenta.
4. Verificar que uno de los últimos tres mínimos toca la banda inferior de Bollinger (configuración larga) o uno de los máximos toca la banda superior (configuración corta).
5. Requerir que la vela anterior tenga un cuerpo más fuerte que su predecesora, señalando Momentum alejándose de la banda.
6. Confirmar la fortaleza de la tendencia con el Momentum calculado en `MomentumCandleType`. La estrategia mide la distancia absoluta entre la lectura de Momentum y 100; la distancia debe superar los umbrales de compra/venta configurados en cualquiera de los últimos tres valores de Momentum.
7. Validar la dirección a largo plazo con un MACD calculado en el marco temporal `MacdCandleType`. Las entradas largas se permiten mientras la línea principal del MACD se mantiene por encima de la línea de señal; los cortos requieren la relación opuesta.
8. Cuando todas las condiciones se alinean, entrar en una posición de mercado usando el volumen del paso martingala actual.

## Dimensionamiento de posición y escalado

- `InitialVolume` define el volumen de la primera entrada.
- Cada add-on adicional multiplica el volumen base por `LotExponent` (`volume = InitialVolume * LotExponent^n`).
- `MaxTrades` limita el número de add-ons secuenciales para que el tamaño total de la posición nunca exceda `InitialVolume * MaxTrades`.

## Reglas de salida y protección

- Valores fijos de `StopLoss` y `TakeProfit` expresados en pasos de precio.
- Lógica opcional de punto de equilibrio (`EnableBreakEven`) que mueve el stop a `BreakEvenOffset` una vez que el precio avanza `BreakEvenTrigger` pasos.
- Trailing stop clásico (`EnableTrailingStop`) que sigue el precio extremo por `TrailingStop` pasos.
- Herramientas de gestión de capital:
  - `UseMoneyTakeProfit` cierra posiciones cuando la ganancia no realizada en moneda de cuenta alcanza `MoneyTakeProfit`.
  - `UsePercentTakeProfit` cierra posiciones cuando la ganancia equivale a `PercentTakeProfit` porcentaje del capital inicial.
  - `UseMoneyTrailing` activa un trailing de ganancia: una vez que la ganancia supera `MoneyTrailTarget`, un retroceso de `MoneyTrailStop` activa una salida.
- `UseEquityStop` monitorea el drawdown de capital relativo al pico de capital registrado durante la sesión. Un drawdown mayor que `EquityRiskPercent` cierra todas las posiciones.
- `CloseOnMacdCross` opcional sale siempre que la línea principal del MACD cruce la línea de señal contra la dirección de la posición actual.

Todas las acciones de protección dependen de órdenes de mercado (`BuyMarket` / `SellMarket`) para neutralizar toda la posición.

## Parámetros

| Nombre | Descripción |
|------|-------------|
| `InitialVolume` | Volumen base de trading usado para la primera entrada. |
| `LotExponent` | Multiplicador aplicado al volumen de cada entrada adicional al escalar. |
| `MaxTrades` | Número máximo de add-ons secuenciales permitidos en cualquier momento. |
| `TakeProfit` | Take profit expresado en pasos de precio. |
| `StopLoss` | Stop loss expresado en pasos de precio. |
| `FastMaPeriod` | Período de la LWMA rápida calculada en precios típicos. |
| `SlowMaPeriod` | Período de la LWMA lenta calculada en precios típicos. |
| `MomentumLength` | Número de barras usadas en el cálculo del Momentum. |
| `MomentumBuyThreshold` | Distancia mínima desde 100 para que el Momentum de marco temporal superior valide operaciones largas. |
| `MomentumSellThreshold` | Distancia mínima desde 100 para que el Momentum de marco temporal superior valide operaciones cortas. |
| `EnableBreakEven` | Habilita el movimiento de stop al punto de equilibrio. |
| `BreakEvenTrigger` | Pasos de precio requeridos para activar el movimiento de punto de equilibrio. |
| `BreakEvenOffset` | Offset aplicado al stop una vez que se activa el punto de equilibrio. |
| `EnableTrailingStop` | Habilita el trailing stop clásico en pasos de precio. |
| `TrailingStop` | Tamaño del trailing stop expresado en pasos. |
| `UseMoneyTakeProfit` | Habilita toma de ganancias fija en moneda de cuenta. |
| `MoneyTakeProfit` | Ganancia en moneda que cierra la posición cuando `UseMoneyTakeProfit` está activo. |
| `UsePercentTakeProfit` | Habilita toma de ganancias basada en porcentaje de capital. |
| `PercentTakeProfit` | Porcentaje del capital inicial que activa una salida cuando `UsePercentTakeProfit` está activo. |
| `UseMoneyTrailing` | Habilita trailing basado en capital tras alcanzar una ganancia objetivo. |
| `MoneyTrailTarget` | Nivel de ganancia que activa la lógica de trailing monetario. |
| `MoneyTrailStop` | Máximo retroceso permitido en moneda tras la activación. |
| `UseEquityStop` | Habilita el cierre de posiciones cuando el drawdown flotante supera un umbral. |
| `EquityRiskPercent` | Máximo drawdown de capital permitido en porcentaje. |
| `CloseOnMacdCross` | Habilita el filtrado de salida basado en MACD. |
| `CandleType` | Marco temporal primario usado para los cálculos de señales. |
| `MomentumCandleType` | Marco temporal superior usado para el filtro de Momentum. |
| `MacdCandleType` | Marco temporal muy lento usado por el filtro de salida MACD. |

## Notas

- La estrategia procesa únicamente velas terminadas; no reacciona dentro de una barra.
- Todos los cálculos de stop y objetivo utilizan el paso de precio del instrumento reportado por el exchange conectado. Asegúrate de que `PriceStep` esté configurado correctamente para un control preciso del riesgo.
- Las protecciones monetarias y basadas en capital dependen de las estadísticas del portafolio de estrategia disponibles en StockSharp. Al ejecutar en modo tester, asegúrate de que la alimentación del portafolio esté habilitada.
- A diferencia del experto MQL original, esta implementación en C# mantiene una sola posición agregada por dirección. El escalado aumenta la posición agregada en lugar de abrir múltiples tickets discretos.
- Las Bandas de Bollinger usan una longitud fija de 20 y una anchura de 2 desviaciones estándar en precios típicos, coincidiendo con el código original.
