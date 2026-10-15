# Estrategia de Auto Adjusting
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

AutoAdjustingStrategy replica el experto MetaTrader *Aouto Adjusting1* usando la API de alto nivel de StockSharp. El puerto conserva el filtro de momentum multitemporal original, la confirmación de tendencia MACD mensual y una pila de tres EMA para detectar retrocesos con la tendencia. Los stops y objetivos se proyectan desde los extremos de oscilación recientes y se ajustan automáticamente en cada vela completada.

## Lógica principal

1. **Estructura de tendencia** – tres medias móviles exponenciales en el marco temporal de trading (6, 14, 26) deben estar alineadas (`EMA6 < EMA14 < EMA26` para largos, invertido para cortos). La vela anterior debe tocar la EMA media, mientras que la vela anterior a esa forma un mínimo más alto / máximo más bajo para confirmar un retroceso.
2. **Confirmación de momentum** – el momentum en el marco temporal superior (mapeado desde el marco temporal de trading, p.ej., H1 → D1) debe desviarse al menos `MomentumBuyThreshold` / `MomentumSellThreshold` de 100 en cualquiera de las últimas tres barras completadas.
3. **Filtro macro** – una señal MACD(12, 26, 9) mensual asegura que las operaciones se alineen con la tendencia dominante (`MACD > Señal` para compras, `<` para ventas).
4. **Ejecución** – las órdenes de mercado se envían una vez que todos los filtros están de acuerdo y no hay exposición opuesta presente. Las posiciones opuestas se aplanan antes de entrar en la nueva dirección.
5. **Protección** – los niveles de stop-loss se colocan un número configurable de pips más allá del mínimo más bajo / máximo más alto de las últimas barras `CandlesBack`. Las distancias de take-profit se escalan por `RewardRatio`. Tanto el stop como el objetivo se reactivan en cada cierre de vela mientras la posición está activa.

## Riesgo y dimensionamiento de posición

La estrategia refleja la parametrización de riesgo original:

- `RiskPercent` calcula un tamaño de posición adaptativo cuando están disponibles el valor del portafolio y los metadatos del paso de precio. El algoritmo divide la pérdida monetaria permitida por la pérdida por unidad implicada por la distancia de stop actual.
- Cuando el dimensionamiento basado en riesgo no puede evaluarse (p.ej., estadísticas de portafolio faltantes), el motor recurre al parámetro `TradeVolume` fijo.

## Parámetros

| Nombre | Tipo | Por defecto | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeFrame(H1)` | Marco temporal de trading usado para la pila EMA. |
| `MomentumCandleType` | `DataType` | Derivado de `CandleType` | Marco temporal superior que alimenta el indicador de momentum (H1→D1, H4→W1, etc.). |
| `MacroMacdCandleType` | `DataType` | `TimeFrame(30 days)` | Marco temporal para la confirmación MACD macro (mensual por defecto). |
| `PadAmount` | `decimal` | `3` | Pips extra más allá de los extremos de oscilación al calcular los stops. |
| `RiskPercent` | `decimal` | `0.1` | Porcentaje del capital del portafolio arriesgado por operación. |
| `RewardRatio` | `decimal` | `2` | Multiplicador aplicado a la distancia de stop para colocar el take-profit. |
| `CandlesBack` | `int` | `3` | Número de velas inspeccionadas para la detección de máximos/mínimos de oscilación. |
| `MomentumBuyThreshold` | `decimal` | `0.3` | Desviación mínima de momentum requerida para habilitar entradas largas. |
| `MomentumSellThreshold` | `decimal` | `0.3` | Desviación mínima de momentum requerida para habilitar entradas cortas. |
| `TradeVolume` | `decimal` | `1` | Tamaño de lote de respaldo cuando el dimensionamiento basado en riesgo no está disponible. |

## Gráficos y visualización

- Suscribirse al marco temporal de trading y trazar las tres EMAs para observar los retrocesos.
- Seguir la serie de momentum en su panel de marco temporal superior para confirmar los umbrales de energía.
- Monitorizar los valores de MACD del marco temporal macro para validar el filtro de tendencia.

## Notas

- El mapeo automático de marcos temporales coincide con el experto MQL: M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1. Otros marcos conservan su valor original.
- La estrategia evita las llamadas a `GetValue` de indicadores almacenando los valores más recientes dentro de la estrategia y alimentándolos a través de los callbacks de bind.
- El comportamiento de trailing refleja el EA original recalculando los niveles protectores cada vez que se cierra una vela.
