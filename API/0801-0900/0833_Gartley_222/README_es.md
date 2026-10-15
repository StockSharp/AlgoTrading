# Estrategia Gartley 222
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en largo cuando se forma un patrón harmónico Gartley 222 alcista.
El patrón se detecta utilizando pivotes altos y bajos validados por razones de Fibonacci.

Se abre una posición larga `PivotLength` barras después de la confirmación cuando el precio cierra por encima del punto C.
La protección cierra la posición en un objetivo de extensión de Fibonacci o en un stop-loss porcentual fijo.

## Detalles

- **Criterios de entrada**:
  - Patrón Gartley 222 alcista confirmado
  - Entrada retrasada `PivotLength` barras
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - Stop-loss o take profit
- **Stops**:
  - `Stop Loss %` por debajo de la entrada
  - `TP Fib Extension` por encima de la entrada
- **Valores predeterminados**:
  - `Pivot Length` = 5
  - `Fib Tolerance` = 0.05
  - `TP Fib Extension` = 1.27
  - `Stop Loss %` = 2

- **Filtros**:
  - Categoría: Patrón
  - Dirección: Solo largos
  - Indicadores: Pivot points, Fibonacci
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
