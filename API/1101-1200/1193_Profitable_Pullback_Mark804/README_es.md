# Estrategia de Retroceso Rentable Mark804
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia de retroceso de seguimiento de tendencia que utiliza una cinta de medias móviles exponenciales. El sistema busca retrocesos del precio hacia la EMA de señal dentro de una tendencia confirmada. Cuando el precio cierra de nuevo en la dirección de la tendencia tras un retroceso, la estrategia abre una posición y la protege con niveles de take profit y stop loss basados en porcentaje.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA rápida > EMA señal > EMA media, opcionalmente EMA media > EMA lenta, cierre anterior por debajo de la EMA señal y cierre actual por encima.
  - **Corto**: EMA rápida < EMA señal < EMA media, opcionalmente EMA media < EMA lenta, cierre anterior por encima de la EMA señal y cierre actual por debajo.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Se alcanza el take profit o el stop loss.
- **Stops**: Sí, porcentajes fijos de take profit y stop loss.
- **Valores predeterminados**:
  - Fast EMA Length = 8
  - Signal EMA Length = 21
  - Medium EMA Length = 50
  - Slow EMA Length = 200
  - Take Profit % = 2
  - Stop Loss % = 1
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
