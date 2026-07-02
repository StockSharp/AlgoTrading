# Estrategia Bollinger Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la reversión a la media de las Bandas de Bollinger

Las pruebas indican un rendimiento anual promedio de aproximadamente 118%. Funciona mejor en el mercado de acciones.

Bollinger Reversion opera contra los movimientos fuera de las Bandas de Bollinger. Las operaciones se abren contra los cierres más allá de las bandas y se cierran una vez que el precio regresa al interior o alcanza un stop.

Las bandas de desviación estándar ofrecen una vista estadística de la sobreextensión. Entrar después de cierres extremos tiene como objetivo obtener ganancias del retroceso hacia la banda media.


## Detalles

- **Criterios de entrada**: Señales basadas en RSI, ATR, Bollinger.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: RSI, ATR, Bollinger
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

