# Estrategia de Filtro Elíptico Óptimo Modificado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica el indicador *Modified Optimum Elliptic Filter* descrito por John F. Ehlers para detectar giros direccionales. El indicador es un filtro digital de dos polos que suaviza el promedio de precios máximos y mínimos usando la siguiente fórmula recursiva:

```
F(t) = 0.13785*(2*HL2(t) - HL2(t-1))
     + 0.0007 *(2*HL2(t-1) - HL2(t-2))
     + 0.13785*(2*HL2(t-2) - HL2(t-3))
     + 1.2103 * F(t-1) - 0.4867 * F(t-2)
```

Donde `HL2` es el punto medio `(High + Low)/2` de cada vela.

La estrategia lee los últimos tres valores del filtro para determinar el momentum. Si el indicador está subiendo y el valor más reciente supera al anterior, se abre una posición larga. Si el indicador está bajando y el valor actual está por debajo del anterior, se abre una posición corta. Las posiciones se revierten cuando ocurre la condición opuesta.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `F(t-1) < F(t-2)` y `F(t) > F(t-1)`.
  - **Corto**: `F(t-1) > F(t-2)` y `F(t) < F(t-1)`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La posición se revierte en la señal opuesta.
- **Stops**: Sin stops explícitos.
- **Valores predeterminados**:
  - `Candle Type` = marco temporal de 4 horas.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
