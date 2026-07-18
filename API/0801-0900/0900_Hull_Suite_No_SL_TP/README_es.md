# Hull Suite Sin SL/TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hull Suite Sin SL/TP es una estrategia de seguimiento de tendencia basada en variaciones de la Hull Moving Average. Invierte la posición cuando la línea Hull cambia de dirección en comparación con dos velas atrás.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: El valor de Hull es mayor que dos velas atrás.
  - **Corto**: El valor de Hull es menor que dos velas atrás.
- **Criterios de salida**: Señal inversa.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 55
  - `Mode` = `Hma`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo/Corto
  - Indicadores: Hull Moving Average
  - Complejidad: Bajo
  - Nivel de riesgo: Bajo
