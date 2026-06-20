# Estrategia de Ejemplo para Strategy Tester
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este ejemplo ilustra cómo se puede combinar el momentum y la fuerza de la tendencia
para formar un sistema discrecional básico. Una pendiente de regresión lineal mide el
momentum a corto plazo mientras que el Average Directional Index evalúa la persistencia
de un movimiento. Dos reglas independientes activan entradas: un pivote de momentum
acompañado de una caída en el ADX, o un nuevo máximo de ADX con el momentum girando al
alza desde valores negativos.

La estrategia es intencionalmente simple y se centra en posiciones largas. Está pensada
como plantilla para probar ideas como niveles de riesgo basados en ATR y controles de
salida opcionales. Los desarrolladores pueden ampliar la lógica de salida o añadir
manejo de stop-loss para convertirla en un modelo de trading completo.

## Detalles

- **Criterios de entrada**:
  - Pivote alto de momentum y ADX en declive.
  - Pivote alto de ADX con momentum subiendo desde valores negativos.
- **Largo/Corto**: Solo largos por defecto.
- **Criterios de salida**:
  - Pivote alto de momentum (si la salida por momentum está habilitada).
  - Marcador de posición de salida de estrategia personalizada.
- **Stops**: Ninguno; los valores ATR están disponibles para uso externo.
- **Valores predeterminados**:
  - Longitud de momentum = 20, longitud DI = 14.
  - Nivel clave ADX = 25, longitud ATR = 14.
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo
  - Indicadores: Regresión lineal, ADX, ATR
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Corto/medio
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí (pivotes de momentum)
  - Nivel de riesgo: Medio
