# Estrategia extrema del punto ideal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sweet Spot Extreme es una adaptación directa del asesor experto MetaTrader 4 "Sweet_Spot_Extreme.mq4" construido sobre el API de alto nivel de StockSharp. La estrategia busca fuertes retrocesos dentro de una tendencia existente combinando dos promedios móviles exponenciales en velas de 15 minutos con un filtro de índice de canales de productos básicos (CCI) de 30 minutos. El tamaño de la posición refleja los controles de riesgo originales, incluida la reducción de lotes al estilo MetaTrader después de rachas perdedoras.

## Lógica central

1. **Confirmación de pendiente de tendencia.** El EMA principal (`MaPeriod`, predeterminado 85) y el cierre EMA (`CloseMaPeriod`, predeterminado 70) se alimentan con precios medios de 15 minutos. Una configuración larga requiere que ambas EMA tengan una pendiente ascendente; una configuración corta necesita que ambos se inclinen hacia abajo.
2. **CCI filtro de agotamiento.** Una segunda suscripción de vela (de 30 minutos de forma predeterminada) alimenta el `CciPeriod` CCI. Las operaciones largas solo se activan cuando CCI cae por debajo de `BuyCciLevel` (-200), mientras que las posiciones cortas requieren CCI por encima de `SellCciLevel` (+200).
3. **Límite de pirámide.** La posición neta agregada no puede exceder `MaxTradesPerSymbol × volume`. Cuando aparece una nueva señal, la estrategia cierra cualquier exposición opuesta y luego suma la capacidad permitida en la dirección de la señal.
4. **Salidas.** Las posiciones se cierran cuando la tendencia EMA pierde su ventaja de pendiente (reflejando la condición MQL `MA <= MAprevious`) o después de que el precio recorre `StopPoints` puntos del instrumento a favor de la posición.

## Gestión de riesgos

- **Volumen basado en riesgo.** El tamaño del pedido predeterminado es `Portfolio.CurrentValue × MaximumRisk ÷ price`. Cuando falta información sobre el capital, el motor recurre al parámetro `Lots` (o la estrategia `Volume`).
- **Ajuste de racha de pérdidas.** Después de dos o más operaciones perdedoras consecutivas, el tamaño de la nueva orden se reduce en `volume × losses ÷ DecreaseFactor`, coincidiendo con el MQL ayudante `LotsOptimized()`.
- **Normalización.** El volumen final está alineado con el `VolumeStep` del instrumento, delimitado por `MinVolume` y recortado por `Security.MaxVolume` cuando se proporciona.

## Parámetros

| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `MaxTradesPerSymbol` | `3` | Número máximo de entradas agregadas permitidas por dirección. |
| `Lots` | `1` | Tamaño de lote fijo alternativo cuando el capital de la cartera no está disponible. |
| `MaximumRisk` | `0.05` | Fracción de capital utilizada para dimensionar cada nueva operación. |
| `DecreaseFactor` | `6` | Divisor que reduce el siguiente pedido después de pérdidas consecutivas. |
| `StopPoints` | `10` | Distancia objetivo de beneficio en puntos del instrumento. Establezca en `0` para desactivar. |
| `MaPeriod` | `85` | EMA período aplicado a velas de 15 minutos para verificar la pendiente de la tendencia. |
| `CloseMaPeriod` | `70` | EMA período aplicado a velas de 15 minutos para el filtro de suavizado de cierre. |
| `CciPeriod` | `12` | Lookback utilizado para el filtro CCI de 30 minutos. |
| `BuyCciLevel` | `-200` | Se requiere un umbral de sobreventa de CCI para entradas largas. |
| `SellCciLevel` | `200` | Se requiere un umbral de sobrecompra de CCI para entradas cortas. |
| `MinVolume` | `0.1` | Volumen mínimo permitido después de la normalización. |
| `TrendCandleType` | `15m` | Tipo de vela utilizado para los cálculos de EMA (precio medio). |
| `CciCandleType` | `30m` | Tipo de vela utilizado para el filtro CCI. |

## Notas y limitaciones

- StockSharp opera en modo de compensación, por lo que varios tickets MT4 se representan como una única posición agregada. Por lo tanto, la guardia `MaxTradesPerSymbol` limita la exposición neta en lugar de contar las órdenes individuales.
- El EA original se basó en `AccountFreeMargin` para el tamaño. Este puerto lo aproxima con `Portfolio.CurrentValue`; ajuste `MaximumRisk` o `Lots` para que se ajuste a las especificaciones del contrato de su corredor.
- Asegúrese de que ambas suscripciones de velas estén habilitadas en la fuente de datos; de lo contrario, los filtros EMA o CCI nunca se formarán y la estrategia permanecerá inactiva.
