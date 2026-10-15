# Estrategia Bjorgum Double Tap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia busca patrones de doble techo y doble suelo. Se abre una operación corta cuando el precio rompe por debajo de la línea de cuello del doble techo, y una operación larga cuando el precio rompe por encima de la línea de cuello del doble suelo. Los niveles de objetivo y stop se calculan como extensiones de Fibonacci de la altura del patrón.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Ruptura del doble suelo por encima de la línea de cuello.
  - **Corto**: Ruptura del doble techo por debajo de la línea de cuello.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Niveles de stop o de objetivo.
- **Stops**: Porcentaje de Fibonacci mediante `StopLossFib`.
- **Valores predeterminados**:
  - Longitud de pivote 50.
  - Tolerancia de pivote 15%.
  - Fibonacci objetivo 100%.
  - Fibonacci de stop 0%.
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Highest/Lowest
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
