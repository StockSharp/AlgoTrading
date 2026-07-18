# Estrategia Hull Ma Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia que utiliza la Media Móvil Hull para la dirección de la tendencia y la confirmación de volumen para las entradas de operaciones.

Las pruebas indican un retorno anual promedio de aproximadamente 169%. Funciona mejor en el mercado de criptomonedas.

La media móvil Hull suaviza el ruido y el aumento de volumen confirma la convicción. Las entradas ocurren cuando el precio se mueve con la pendiente de Hull respaldado por un aumento de volumen.

Este método está orientado a traders que observan una fuerte participación en rupturas. Los stops basados en ATR defienden contra reversiones repentinas.

## Detalles

- **Criterios de entrada**:
  - Largo: `HullMA(t) > HullMA(t-1) && Volume > AvgVolume * VolumeMultiplier`
  - Corto: `HullMA(t) < HullMA(t-1) && Volume > AvgVolume * VolumeMultiplier`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: `HullMA(t) < HullMA(t-1)`
  - Corto: `HullMA(t) > HullMA(t-1)`
- **Stops**: `StopLossAtr` ATR desde la entrada
- **Valores predeterminados**:
  - `HullPeriod` = 9
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossAtr` = 2.0m
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Hull MA, Moving Average, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

