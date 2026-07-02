# Estrategia Bollinger Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia que utiliza rupturas de las Bandas de Bollinger con confirmación de volumen.
Entra en posiciones cuando el precio rompe por encima/debajo de las Bandas de Bollinger con mayor volumen.

Las pruebas indican un retorno anual promedio de aproximadamente 178%. Funciona mejor en el mercado de acciones.

Las bandas de Bollinger muestran la expansión de la volatilidad y el volumen confirma la ruptura. Las posiciones se toman cuando el precio cierra fuera de una banda con fuerte actividad.

Adecuado para operadores de rupturas que esperan continuación. Un stop basado en ATR mantiene las pérdidas manejables.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > UpperBand && Volume > AvgVolume * VolumeMultiplier`
  - Corto: `Close < LowerBand && Volume > AvgVolume * VolumeMultiplier`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - El precio regresa a la banda media
- **Stops**: Basado en ATR usando `StopLossAtr`
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossAtr` = 2.0m
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

