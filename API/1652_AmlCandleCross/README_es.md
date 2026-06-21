# Estrategia de Cruce de Vela AML
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el indicador Adaptive Market Level (AML).
Se abre una operación cuando el valor de AML se encuentra dentro del cuerpo de la vela actual:
si la vela cierra por encima de la apertura y el AML está entre ellos, se abre una posición
larga. Para velas bajistas, la condición opuesta abre una posición corta. Opcionalmente,
la posición puede revertirse cuando aparezca la señal contraria.

## Detalles

- **Criterios de entrada**:
  - **Largo**: vela alcista y `open <= AML <= close`.
  - **Corto**: vela bajista y `open >= AML >= close`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Posición revertida en señal opuesta cuando está habilitado.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Fractal` = 70
  - `Lag` = 18
  - `Shift` = 0
  - `UseOpposite` = true
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único (AML)
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
