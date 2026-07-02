# Estrategia Macd Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia que combina el MACD (Convergencia/Divergencia de Medias Móviles) con confirmación de volumen. Entra en posiciones cuando la línea MACD cruza la línea de Señal y lo confirma con un aumento de volumen.

Las pruebas indican un retorno anual promedio de aproximadamente 175%. Funciona mejor en el mercado de acciones.

Los cruces del MACD se filtran por un aumento de volumen para confirmar el momentum. Las señales de compra vienen en cruces alcistas con volumen en expansión; las de venta hacen lo contrario.

Los traders de momentum que observan picos de volumen pueden encontrarlo valioso. El riesgo se limita usando un stop de ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `MACD crosses above Signal && Volume > AvgVolume * VolumeMultiplier`
  - Corto: `MACD crosses below Signal && Volume > AvgVolume * VolumeMultiplier`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cruce del MACD en dirección opuesta
- **Stops**: Basado en porcentaje en `StopLossPercent`
- **Valores predeterminados**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: MACD, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

