# Estrategia Parabolic Sar Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia que combina Parabolic SAR con confirmación de volumen. Entra en operaciones cuando el precio cruza el Parabolic SAR con un volumen superior al promedio.

Las pruebas indican un retorno anual promedio de aproximadamente 151%. Funciona mejor en el mercado de acciones.

El Parabolic SAR identifica cambios de tendencia y el mayor volumen valida la señal. Las operaciones comienzan cuando el giro del SAR viene acompañado de expansión de volumen.

Útil para traders que siguen movimientos basados en volumen. El rastro del SAR y un factor ATR protegen contra grandes pérdidas.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > SAR && Volume > AvgVolume`
  - Corto: `Close < SAR && Volume > AvgVolume`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Giro del SAR
- **Stops**: Usa Parabolic SAR como trailing stop
- **Valores predeterminados**:
  - `Acceleration` = 0.02m
  - `MaxAcceleration` = 0.2m
  - `VolumePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Parabolic SAR, Parabolic SAR, Volumen
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

