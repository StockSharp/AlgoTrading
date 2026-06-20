# Estrategia de QQE Signals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa la técnica de Quantitative Qualitative Estimation sobre RSI. El indicador construye bandas dinámicas superiores e inferiores alrededor de una línea RSI suavizada y rastrea los cruces de banda para señalar cambios de tendencia. Cuando RSI cruza por encima de la banda de seguimiento se genera una señal larga; los cruces por debajo activan las salidas.

Al adaptar las bandas a la volatilidad, QQE busca suavizar el ruido mientras permanece sensible. La estrategia se centra en operaciones largas y depende de las reversiones de trades del motor para cerrar posiciones.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La línea suavizada de RSI cruza por encima de la banda de seguimiento.
- **Criterios de salida**:
  - RSI cae por debajo de la banda opuesta o aparece una señal opuesta.
- **Indicadores**:
  - RSI (período 14, suavizado 5)
  - Bandas QQE derivadas del ATR del RSI con factor 4.238
- **Stops**: Ninguno por defecto; depende de señales opuestas.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.238
  - `Threshold` = 10
- **Filtros**:
  - Seguimiento de tendencia
  - Marco temporal único
  - Indicadores: RSI, QQE
  - Stops: Ninguno
  - Complejidad: Moderado
