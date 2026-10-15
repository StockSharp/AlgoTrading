# Estrategia ATR Stop Loss con Doble SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra largo cuando una Media Móvil Simple (SMA) rápida cruza por encima de una SMA lenta y entra corto en el cruce opuesto.
Un stop-loss opcional usa el Rango Verdadero Promedio (ATR) multiplicado por un factor definido por el usuario para determinar los niveles de salida.

## Detalles

- **Criterios de entrada**:
  - **Largo**: SMA rápida cruza por encima de la SMA lenta.
  - **Corto**: SMA rápida cruza por debajo de la SMA lenta.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop-loss basado en ATR si está habilitado.
- **Stops**: Múltiplo de ATR desde el precio de entrada.
- **Valores predeterminados**:
  - `FastLength` = 15
  - `SlowLength` = 45
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, ATR
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
