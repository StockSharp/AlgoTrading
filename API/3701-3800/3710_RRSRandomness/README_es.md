# Estrategia de aleatoriedad RRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de aleatoriedad RRS** es una adaptación StockSharp de "RRS Aleatoriedad en la naturaleza EA" para MetaTrader 4.
Emula al asesor experto original con entradas de mercado largas o cortas seudoaleatorias, comprobaciones de stop-loss y take-profit en velas finalizadas, trailing opcional y liquidación cuando la pérdida flotante alcanza el umbral configurado.

Debido a que StockSharp utiliza posiciones netas por valor, no se admiten exposiciones largas y cortas simultáneas. Por lo tanto, `DoubleSide` empieza con una compra y alterna la dirección tras cada entrada, en vez de mantener dos operaciones cubiertas como en MetaTrader.

## Lógica de trading

1. En cada vela finalizada usa el cierre para las protecciones y, si están disponibles, el bid/ask de Nivel 1 para el spread y el precio de liquidación.
2. Con una posición abierta comprueba stop loss, take profit, trailing stop y el límite de pérdida flotante; envía como máximo una orden de cierre por vela.
3. Cuando está plano, valida las restricciones de diferencial y volumen antes de abrir una nueva operación:
   - **DoubleSide** alterna entre entradas largas y cortas, empezando por una larga.
   - **OneSide** usa un entero seudoaleatorio repetible en `[0,5]`: `1` o `4` abre largo, `0` o `3` abre corto y `2` o `5` omite la vela. La secuencia se reinicia al iniciar o restablecer la estrategia.
4. Los volúmenes comerciales se dibujan uniformemente entre el mínimo y el máximo configurados y están alineados con el paso de volumen del instrumento.

## Parámetros

| grupo | Nombre | Descripción |
|-------|------|-------------|
| generales | `Mode` | Entradas alternas (`DoubleSide`, `0`) o filtradas al azar (`OneSide`, `1`). |
| Configuración de lote | `MinVolume` / `MaxVolume` | Rango de volumen para operaciones generadas aleatoriamente. |
| Protección | `TakeProfitPoints` | Distancia de obtención de beneficios en pasos de precio. |
| Protección | `StopLossPoints` | Distancia de stop-loss en pasos de precio. |
| Protección | `TrailingStartPoints` | Distancia de beneficio que permite la gestión del trailing stop. |
| Protección | `TrailingGapPoints` | Compensación entre el precio de mercado y el trailing stop. |
| Filtros | `MaxSpreadPoints` | Spread máximo de Nivel 1 en pasos de precio. Cero bloquea nuevas entradas; un valor positivo permite el fallback de velas sin bid/ask. |
| Filtros | `SlippagePoints` | Configuración de deslizamiento informativo (no se aplica automáticamente). |
| Gestión de riesgos | `MoneyRiskMode` | Pérdida fija (`FixedMoney`, `0`) o porcentaje de la cartera (`BalancePercentage`, `1`). |
| Gestión de riesgos | `RiskValue` | Cantidad de riesgo (moneda o porcentaje según la modalidad). |
| generales | `TradeComment` | Comentario de las entradas; las salidas añaden el motivo de activación. |
| generales | `CandleType` | Serie de velas que impulsa el ciclo de decisión. |

## Notas

- Las cotizaciones de Nivel 1 mejoran el cálculo del spread y del precio de liquidación. Sin ambas puntas, un límite positivo permite el fallback de velas; `MaxSpreadPoints = 0` siempre bloquea entradas.
- Las protecciones se evalúan en velas finalizadas. El trailing se activa tras ganar `TrailingStartPoints + TrailingGapPoints` pasos y sigue el precio a `TrailingGapPoints`.
- `FixedMoney` interpreta `RiskValue` en moneda de la cuenta; `BalancePercentage` usa ese porcentaje del valor actual de la cartera.
