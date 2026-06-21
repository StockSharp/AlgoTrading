# Estrategia de Canal Karpenko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Canal Karpenko construye un canal de precio dinámico utilizando dos medias móviles. La línea base es un promedio de precios de cierre, mientras que los límites superior e inferior se derivan del rango promedio alto-bajo escalado por la razón áurea 1.618. El canal se expande hasta envolver la barra actual.

Una señal para ir largo aparece cuando el límite superior, previamente por encima de la línea base, cruza por debajo de ella. Una señal corta surge cuando el límite superior cruza por encima de la línea base después de permanecer por debajo. Las posiciones existentes en dirección opuesta se cierran cuando cambia el régimen.

Solo se procesan las velas completadas. Niveles fijos de stop-loss y take-profit protegen cada operación.

## Detalles

- **Criterios de entrada:**
  - **Largo:** El límite superior anterior estaba por encima de la línea base y el valor actual está por debajo o igual a ella.
  - **Corto:** El límite superior anterior estaba por debajo de la línea base y el valor actual está por encima o igual a ella.
- **Criterios de salida:**
  - Cerrar largo cuando el límite superior anterior estaba por debajo de la línea base.
  - Cerrar corto cuando el límite superior anterior estaba por encima de la línea base.
- **Stops:** Distancias fijas de stop-loss y take-profit en unidades de precio.
- **Valores predeterminados:**
  - `Base MA` = 144
  - `History` = 500
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
  - `Candle Type` = 4 hour
- **Filtros:**
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Custom
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
