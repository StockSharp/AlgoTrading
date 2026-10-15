# Estrategia 80-20
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia detecta velas donde el precio cierra en el 20% superior o inferior de la sesión. Una señal alcista ocurre cuando el cierre está dentro del quinto superior y la apertura está dentro del quinto inferior del rango. Una señal bajista ocurre cuando la apertura está dentro del quinto superior y el cierre está dentro del quinto inferior. El enfoque busca capturar reversiones rápidas desde cierres extremos de vela.

## Detalles

- **Criterios de entrada**:
  - Cierre en el 20% superior y apertura en el 20% inferior → largo.
  - Apertura en el 20% superior y cierre en el 20% inferior → corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Una señal opuesta revierte la posición.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - Range percent = 0.2.
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
