# Estrategia Super Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Supertrend.

Las pruebas indican un retorno anual promedio de aproximadamente 67%. Funciona mejor en el mercado de acciones.

Super Trend calcula una línea dinámica del ATR que alterna entre soporte y resistencia. El precio que cruza por encima convierte el sesgo en alcista, y cruzar por debajo lo convierte en bajista. La operación termina cuando la línea se invierte.

Al seguir esta línea adaptativa, la estrategia intenta capturar movimientos sostenidos minimizando los falsos movimientos. Dado que el nivel de stop sigue al precio, bloquea las ganancias una vez que el momentum se desvanece.


## Detalles

- **Criterios de entrada**: Señales basadas en ATR, Supertrend.
- **Largo/Corto**: Ambos directions.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `Period` = 10
  - `Multiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, Supertrend
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

