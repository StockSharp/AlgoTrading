# Estrategia de Índice Alcista Porcentual de Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa el Índice de Fuerza Relativa (RSI) para aproximar el Índice Alcista Porcentual de Bitcoin. Entra largo cuando el RSI sube por encima del nivel de sobreventa y entra corto cuando el RSI cae por debajo del nivel de sobrecompra.

## Detalles

- **Criterios de entrada**:
  - **Largo**: RSI cruza por encima del nivel de sobreventa.
  - **Corto**: RSI cruza por debajo del nivel de sobrecompra.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `RSI Period` = 14
  - `Overbought` = 70
  - `Oversold` = 30
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Medio plazo
