# Estrategia MACD CCI Lotfy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina el MACD y el CCI con un factor de escala.
Se abre una posición cuando ambos indicadores cruzan umbrales extremos en la misma dirección.

El valor del MACD se multiplica por un coeficiente para alinear la escala con el CCI, permitiendo la comparación directa con el mismo umbral.
El enfoque apunta a capturar reversiones desde zonas de sobrecompra y sobreventa.

## Detalles

- **Criterios de entrada**:
  - Largo: `CCI < -Threshold` y `MACD * MacdCoefficient < -Threshold`
  - Corto: `CCI > Threshold` y `MACD * MacdCoefficient > Threshold`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Una señal opuesta activa la posición inversa
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `CciPeriod` = 8
  - `FastPeriod` = 13
  - `SlowPeriod` = 33
  - `MacdCoefficient` = 86000
  - `Threshold` = 85
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: MACD, CCI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
