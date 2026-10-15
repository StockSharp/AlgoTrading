# Estrategia Xit de Cruce de Tres MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una recreación en StockSharp del asesor experto MetaTrader 5 **XIT_THREE_MA_CROSS.mq5**. Alinea tres medias móviles, verifica la separación de momento MACD y dimensiona posiciones a partir de límites de riesgo basados en ATR. El método es de seguimiento de tendencia con confirmación de momento y apunta a oscilaciones de mediano plazo en pares de divisas líquidos o índices.

## Descripción general

- **Régimen de mercado**: Funciona mejor en instrumentos que tienen tendencia durante múltiples velas en el marco temporal seleccionado.
- **Indicadores**:
  - Medias móviles lenta, intermedia y rápida (tipo seleccionable por el usuario) evaluadas en el marco temporal de trading.
  - MACD (basado en EMA) para dirección de momento y distancia entre la línea MACD y la señal.
  - Dos cálculos ATR (misma longitud, marcos temporales independientes) usados para proyectar distancias de stop-loss y take-profit.
- **Dirección de la orden**: Bidireccional. El motor puede abrir tanto operaciones largas como cortas.
- **Dimensionamiento de posición**: Calculado a partir del porcentaje de riesgo configurado y la distancia de stop basada en ATR. Cuando los metadatos del instrumento están incompletos, la estrategia recurre a la propiedad `Volume` predeterminada.

## Lógica de trading

### Entrada larga

Una posición larga se abre cuando todas las condiciones a continuación son verdaderas en una vela terminada:

1. La línea MACD aumenta en comparación con la barra anterior (`MACD[t] > MACD[t-1]`).
2. La línea de señal MACD aumenta en comparación con la barra anterior.
3. La línea MACD supera la línea de señal en al menos `MacdTriggerPoints * PriceStep`.
4. La media móvil intermedia sube vs el valor anterior.
5. La media móvil rápida sube vs el valor anterior.
6. La MA intermedia está por encima de la MA lenta.
7. La MA rápida está por encima de la MA intermedia.
8. Ambos valores ATR están disponibles para definir distancias de stop y objetivo.

### Entrada corta

Las reglas del lado corto reflejan la configuración larga con comparaciones invertidas:

1. La línea MACD disminuye en comparación con la barra anterior.
2. La línea de señal MACD disminuye en comparación con la barra anterior.
3. La línea de señal es mayor que la línea MACD en al menos `MacdTriggerPoints * PriceStep`.
4. La MA intermedia cae en comparación con la vela anterior.
5. La MA rápida cae en comparación con la vela anterior.
6. La MA intermedia está por debajo de la MA lenta.
7. La MA rápida está por debajo de la MA intermedia.
8. Ambas series ATR han entregado un valor terminado.

### Lógica de salida

- **Las posiciones largas** se cierran cuando la MA rápida cae por debajo de la MA intermedia, o el precio alcanza los niveles de stop/take-profit basados en ATR.
- **Las posiciones cortas** se cierran cuando la MA rápida cruza por encima de la MA intermedia, o se tocan los límites ATR.
- Después de cerrar una posición, el algoritmo espera la siguiente vela antes de evaluar nuevas entradas, siguiendo el comportamiento del EA original.

## Gestión de riesgo

- **Stop Loss**: La distancia es igual al último valor ATR de `AtrStopCandleType`. Para largos el precio de stop es `Entry - ATR`, para cortos es `Entry + ATR`.
- **Take Profit**: La distancia es igual al valor ATR de `AtrTakeCandleType`. Los objetivos son reflejados relativos al precio de entrada.
- **Porcentaje de riesgo**: La estrategia estima la pérdida monetaria por unidad a partir de la distancia de stop. Si `PriceStep` y `PriceStepCost` son conocidos, el riesgo por contrato usa la valoración de ticks. De lo contrario, se usa la distancia de precio bruta. El tamaño de posición es `RiskPercent%` del valor actual del portafolio dividido por el riesgo por unidad, redondeado hacia abajo al `VolumeStep` más cercano.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Marco temporal principal para cálculos de medias móviles y MACD. | Velas de 1 hora |
| `SlowMaLength` / `IntermediateMaLength` / `FastMaLength` | Períodos de las medias móviles. | 60 / 14 / 4 |
| `SlowMaType`, `IntermediateMaType`, `FastMaType` | Familias de medias móviles (Simple, Exponencial, Suavizado, Ponderado). | Simple |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Longitudes de EMA rápida, lenta y señal MACD. | 12 / 26 / 9 |
| `MacdTriggerPoints` | Distancia mínima entre MACD y su señal, medida en puntos del instrumento. Convertida usando `PriceStep`. | 7 |
| `AtrLength` | Período para ambos indicadores ATR. | 14 |
| `AtrTakeCandleType` / `AtrStopCandleType` | Marcos temporales para series ATR de take-profit y stop-loss. | Velas de 4 horas |
| `RiskPercent` | Porcentaje del valor actual del portafolio arriesgado en cada operación. | 10% |

## Notas de uso

1. Adjunte la estrategia a un valor con `PriceStep`, `PriceStepCost` y `VolumeStep` precisos para obtener un dimensionamiento de posición preciso.
2. Asegúrese de que los datos históricos estén disponibles para cada marco temporal suscrito (`CandleType`, `AtrTakeCandleType`, `AtrStopCandleType`). Los valores ATR faltantes retrasarán las entradas.
3. El algoritmo opera en velas completamente cerradas e ignora las fluctuaciones intrabar, reflejando la lógica MetaTrader original de obtener buffers de indicadores actuales y anteriores.
4. Modifique los tipos de media móvil si el mercado objetivo favorece filtros más suaves o más rápidos.

## Archivos

- `CS/XitThreeMaCrossStrategy.cs` – Implementación C# con API de alto nivel de StockSharp, incluyendo suscripciones ATR y dimensionamiento de riesgo.
- `README_ru.md` – Descripción en ruso de la estrategia.
- `README_zh.md` – Traducción al chino de la documentación.
