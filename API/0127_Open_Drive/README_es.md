# Estrategia Open Drive
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El Open Drive se refiere a un fuerte movimiento direccional justo desde la apertura, a menudo tras un catalizador de noticias nocturnas.
Los operadores buscan alto volumen y momentum sostenido en los primeros minutos.

Las pruebas indican un retorno anual promedio de aproximadamente el 118%. Funciona mejor en el mercado de acciones.

La estrategia se suma a ese momentum, entrando largo o corto dentro del rango de apertura y ajustando un stop móvil conforme el precio se extiende.

Las posiciones se cierran rápidamente si el impulso se detiene, manteniendo pequeñas las pérdidas durante aperturas agitadas.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Intradía
  - Dirección: Ambos
  - Indicadores: Price Action
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

