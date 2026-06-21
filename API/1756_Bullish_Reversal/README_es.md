# Estrategia de Reversión Alcista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que busca formaciones clásicas de velas de reversión alcista. Cuando cualquiera de estos patrones aparece por debajo de una media móvil simple de 50 períodos, la estrategia abre una posición larga. Un trailing stop opcional protege las ganancias abiertas.

## Patrones
- **Abandoned Baby** – dos velas bajistas consecutivas seguidas de una vela alcista que cierra por encima de la apertura de la primera vela, mientras que la segunda vela tiene una brecha hacia abajo.
- **Morning Doji Star** – una vela bajista, una vela doji o de cuerpo pequeño, y luego una vela alcista que cierra de nuevo dentro del cuerpo de la primera vela.
- **Three Inside Up** – una vela bajista, una vela alcista más pequeña dentro de su rango, y luego una vela alcista que cierra por encima de la apertura de la primera vela.
- **Three Outside Up** – una vela bajista seguida de una vela alcista más grande que la envuelve y una tercera vela alcista que confirma la reversión.
- **Three White Soldiers** – tres velas alcistas consecutivas con cierres ascendentes.

## Detalles
- **Criterios de entrada**: cualquier patrón listado y la última vela abrió por debajo de la media móvil
- **Largo/Corto**: Largo
- **Criterios de salida**: trailing stop opcional
- **Stops**: Trailing
- **Valores predeterminados**:
  - `MaPeriod` = 50
  - `TrailingStop` = 50
  - `UseTrailingStop` = true
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Solo largos
  - Indicadores: SMA
  - Stops: Trailing
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
