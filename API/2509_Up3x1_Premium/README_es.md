# Estrategia UP3x1 Premium
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia UP3x1 Premium es un port en C# del asesor experto de MetaTrader *up3x1_premium_v2M*. Combina cruces de EMA rápida/lenta con filtros de velas de gran rango y un filtro de contexto diario para capturar rupturas de impulso mientras mantiene el riesgo gestionado a través de objetivos fijos y trailing stops.

## Cómo Funciona

1. **Detección de Tendencia**
   - Calcula dos EMAs en el marco temporal de trabajo (por defecto períodos 12 y 26).
   - Rastrea los dos valores EMA anteriores para identificar cruces alcistas o bajistas similar a la lógica MQL.
   - Mantiene una EMA diaria para entender el sesgo más amplio.

2. **Lógica de Entrada**
   - Los **setups largos** se activan cuando ocurre cualquiera de los siguientes:
     - La EMA rápida cruza por encima de la EMA lenta y las dos aperturas de velas anteriores muestran progresión ascendente.
     - La vela anterior forma una barra alcista de gran rango cuyo cuerpo supera el umbral de cuerpo configurado.
     - A medianoche, si la vela diaria anterior cerró notablemente más bajo de su apertura (capitulación), se permite una señal de rebote.
     - El precio opera por encima de la EMA diaria actual, favoreciendo el lado largo.
   - Los **setups cortos** se activan cuando se cumplen las condiciones espejo (cruce EMA bajista, barra bajista de gran rango, o reversión de medianoche en la dirección opuesta).
   - Cuando se activan simultáneamente los disparadores largos y cortos, la estrategia sigue la relación EMA prevaleciente para desempatar.

3. **Gestión de Salida**
   - Una posición abierta se cierra cuando:
     - Las EMAs convergen dentro de ±0.1%, señalizando pérdida de convicción direccional.
     - El precio toca los offsets de take-profit o stop-loss definidos en unidades de precio absolutas.
     - El trailing stop (si está habilitado) es arrastrado detrás del precio y posteriormente golpeado.

4. **Manejo de Posición**
   - Las operaciones se abren solo cuando la estrategia está plana, coincidiendo con el comportamiento original del EA.
   - El volumen se controla mediante el parámetro `OrderVolume` y se aplica a cada orden de mercado.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Tamaño de orden en lotes/contratos para cada operación. |
| `FastEmaLength` / `SlowEmaLength` | Períodos para las EMAs rápida y lenta en el marco temporal de trabajo. |
| `DailyEmaLength` | Período para la EMA calculada en las velas diarias. |
| `TakeProfit` | Objetivo de beneficio absoluto en unidades de precio (establecer en cero para deshabilitar). |
| `StopLoss` | Distancia de stop absoluta en unidades de precio (establecer en cero para deshabilitar). |
| `TrailingStop` | Distancia de trailing que sigue al precio una vez que el movimiento supera el umbral. |
| `RangeThreshold` | Rango total mínimo que la vela anterior debe superar para calificar como barra de gran rango. |
| `BodyThreshold` | Tamaño mínimo del cuerpo de la vela que define barras de impulso alcistas/bajistas. |
| `DailyReversalThreshold` | Tamaño del reverso diario anterior requerido durante el filtro de medianoche. |
| `CandleType` | Marco temporal de trabajo para la lógica principal de EMA y precio. |
| `DailyCandleType` | Marco temporal superior usado para el contexto EMA diario. |

## Notas de Uso

- Los valores predeterminados imitan las constantes numéricas encontradas en el EA original (convertidas de valores en puntos a offsets de precio decimal).
- Ajuste los umbrales basados en precio (`TakeProfit`, `StopLoss`, `TrailingStop`, umbrales de rango/cuerpo) para que coincidan con el tamaño del tick del instrumento operado.
- El filtro de EMA diario reemplaza el sesgo largo incondicional presente en el script MQL, manteniendo las operaciones alineadas con la tendencia del marco temporal superior prevaleciente.
- Siempre realice backtesting en datos históricos y pruebas hacia adelante en un entorno de demo antes de habilitar el trading en vivo.
