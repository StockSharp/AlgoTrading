# Estrategia de aleatoriedad RRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de aleatoriedad RRS** es una adaptación StockSharp de "RRS Aleatoriedad en la naturaleza EA" para MetaTrader 4.
Emula al asesor experto original generando entradas aleatorias de mercado largas o cortas, aplica niveles de stop-loss y take-profit, opcionalmente rastrea operaciones rentables y realiza liquidaciones basadas en riesgos cuando las pérdidas flotantes exceden el umbral configurado.

Debido a que StockSharp utiliza posiciones netas por valor, no se admiten exposiciones largas y cortas simultáneas. Por lo tanto, el modo "DoubleSide" alterna la dirección de entrada en cada oportunidad en lugar de mantener dos operaciones cubiertas como en MetaTrader.

## Lógica de trading

1. En cada vela terminada, la estrategia evalúa el último precio de mercado obtenido de las operaciones o cotizaciones de Nivel 1.
2. Si hay una posición abierta, aplica reglas de stop-loss, take-profit y trailing-stop y realiza una verificación del riesgo de la cartera.
3. Cuando está plano, valida las restricciones de diferencial y volumen antes de abrir una nueva operación:
   - El modo **DoubleSide** alterna entre entradas largas y cortas.
   - El modo **OneSide** sigue la regla original EA: un entero aleatorio en `[0,5]` abre posiciones largas para los valores `1` o `4` y posiciones cortas para `0` o `3`.
4. Los volúmenes comerciales se dibujan uniformemente entre el mínimo y el máximo configurados y están alineados con el paso de volumen del instrumento.

## Parámetros

| grupo | Nombre | Descripción |
|-------|------|-------------|
| generales | `Mode` | Modo de negociación: entradas alternativas (`DoubleSide`) o entradas cerradas aleatorias (`OneSide`). |
| Configuración de lote | `MinVolume` / `MaxVolume` | Rango de volumen para operaciones generadas aleatoriamente. |
| Protección | `TakeProfitPoints` | Distancia de obtención de beneficios en pasos de precio. |
| Protección | `StopLossPoints` | Distancia de stop-loss en pasos de precio. |
| Protección | `TrailingStartPoints` | Distancia de beneficio que permite la gestión del trailing stop. |
| Protección | `TrailingGapPoints` | Compensación entre el precio de mercado y el trailing stop. |
| Filtros | `MaxSpreadPoints` | Spread máximo permitido (en incrementos de precio) para abrir nuevas posiciones. |
| Filtros | `SlippagePoints` | Configuración de deslizamiento informativo (no se aplica automáticamente). |
| Gestión de riesgos | `MoneyRiskMode` | Elija entre pérdida de moneda fija o porcentaje del valor de la cartera. |
| Gestión de riesgos | `RiskValue` | Cantidad de riesgo (moneda o porcentaje según la modalidad). |
| generales | `TradeComment` | Comentario informativo adjunto a los pedidos generados. |
| generales | `CandleType` | Serie de velas que impulsa el ciclo de decisión. |

## Notas

- La estrategia se basa en suscripciones a datos de mercado para velas, cotizaciones y operaciones de nivel 1. Asegúrese de que el tipo de datos seleccionado esté disponible para la seguridad elegida.
- La lógica del trailing stop refleja la implementación de MQL: se activa después de que el precio gana `TrailingStartPoints + TrailingGapPoints` pasos y luego sigue el precio a una distancia de `TrailingGapPoints`.
- La gestión de riesgos compara el PnL flotante con el umbral de pérdida configurado y liquida la posición cuando se supera el umbral.
