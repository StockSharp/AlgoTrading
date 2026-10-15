# Estrategia Tendency EMA + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia superpone un cruce de EMA rápida/media sobre una EMA de tendencia más
lenta y un filtro RSI. Las operaciones largas requieren que la EMA rápida cruce por
encima de la EMA media mientras ambas permanecen por encima de la línea de tendencia
lenta y la vela cierra alcista. Las operaciones cortas reflejan estas reglas. Los
extremos del RSI cierran posiciones, y una función opcional de "cerrar después de X
barras" fija las ganancias si el precio se mueve en la dirección esperada rápidamente.

El enfoque busca participar solo en entradas de retroceso que se alineen con la tendencia
prevalente, usando el RSI para salir cuando el momentum se estira en exceso. Funciona
mejor en gráficos intradía donde los cruces de EMA ofrecen señales oportunas y se
producen múltiples configuraciones en cada sesión.

## Detalles

- **Criterios de entrada**:
  - EMA rápida cruza por encima de EMA media, ambas por encima de EMA lenta, vela alcista.
  - EMA rápida cruza por debajo de EMA media, ambas por debajo de EMA lenta, vela bajista.
- **Largo/Corto**: Largo habilitado, corto opcional.
- **Criterios de salida**:
  - RSI > 70 cierra largo; RSI < 30 cierra corto.
  - Opcional: cerrar después de X barras si la operación es rentable.
- **Stops**: Ninguno incorporado.
- **Valores predeterminados**:
  - Longitud RSI = 14.
  - Longitudes EMA A/B/C = 9/21/50.
  - Cerrar después de X barras = desactivado, X = 5.
- **Filtros**:
  - Categoría: Tendencia + Momentum
  - Dirección: Ambos (largo por defecto)
  - Indicadores: EMA, RSI
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Corto
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
