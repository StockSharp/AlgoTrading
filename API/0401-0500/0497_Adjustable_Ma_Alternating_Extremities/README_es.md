# Estrategia de MA Ajustable y Extremidades Alternadas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza Bandas de Bollinger para emular la Media Móvil Ajustable con extremidades alternadas. Se abre una posición larga cuando el precio rompe por encima de la banda superior, mientras que se abre una posición corta cuando el precio cae por debajo de la banda inferior. El estado de sobrepaso se alterna, evitando operaciones consecutivas en la misma dirección.

## Detalles

- **Criterios de entrada**:
  - Ir largo cuando el máximo de la vela cruza por encima de la banda superior.
  - Ir corto cuando el mínimo de la vela cruza por debajo de la banda inferior.
- **Criterios de salida**:
  - Ruptura de la banda opuesta.
- **Indicadores**: Bandas de Bollinger (SMA + desviación estándar).
- **Valores predeterminados**:
  - Length = 50
  - Multiplier = 2
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Marco temporal: Corto/medio plazo
  - Nivel de riesgo: Medio
