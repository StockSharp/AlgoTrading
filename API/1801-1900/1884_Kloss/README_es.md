# Estrategia Kloss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Kloss combina una media móvil ponderada (WMA), el Índice de Canal de Materias Primas (CCI) y el oscilador Stochastic. Todos los indicadores se evalúan en valores históricos desplazados, lo que permite que las señales se basen en el contexto pasado del mercado. Una posición larga se abre cuando el CCI cae por debajo de un umbral negativo, la línea principal del Stochastic cae por debajo de una desviación del nivel neutro 50, y el precio desplazado está por encima del WMA desplazado. Una posición corta se abre en las condiciones opuestas. El cierre inverso opcional sale de una posición existente cuando aparece la señal opuesta. El stop loss y el take profit se establecen en puntos desde el precio de entrada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: CCI desplazado por debajo de `-CciDiffer`, Stochastic desplazado por debajo de `50 - StochDiffer`, y precio desplazado por encima del WMA desplazado.
  - **Corto**: CCI desplazado por encima de `CciDiffer`, Stochastic desplazado por encima de `50 + StochDiffer`, y precio desplazado por debajo del WMA desplazado.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal inversa si `RevClose` está habilitado o niveles de stop loss / take profit.
- **Stops**: Stop loss y take profit absolutos en puntos.
- **Filtros**:
  - Los desplazamientos de indicadores y precios mediante `CommonShift` permiten la generación de señales a partir de barras pasadas.
