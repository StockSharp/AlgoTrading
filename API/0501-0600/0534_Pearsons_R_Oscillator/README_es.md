# Estrategia Oscilador Pearson's R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Oscilador Pearson's R busca dinámicamente el período en el que el precio mejor se ajusta a un canal de regresión lineal utilizando el coeficiente de correlación de Pearson. Cuando la correlación alcanza el umbral positivo o negativo especificado, la estrategia forma un canal de regresión y opera rupturas.

Las posiciones se abren cuando el precio cruza los límites del canal y pueden cerrarse en cruces de la línea media. El enfoque se adapta a las condiciones del mercado ajustando automáticamente la ventana de análisis a la correlación más fuerte.

## Detalles

- **Criterios de entrada**:
  - El precio cruza por encima de la línea de regresión superior → **Largo**.
  - El precio cruza por debajo de la línea de regresión inferior → **Corto**.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Cruce de la línea media en dirección opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `MinPeriod` = 48
  - `MaxPeriod` = 360
  - `Step` = 12
  - `IdealPositive` = 0.85
  - `IdealNegative` = -0.85
  - `Deviations` = 2
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Pearson's R, Regresión Lineal
  - Stops: Ninguno
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
