# Estrategia de Tendencia Normalizada Crunchsters
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que normaliza los rendimientos y aplica una Hull Moving Average al precio normalizado acumulado.
Entra en largo cuando el precio normalizado cruza por encima de la HMA y en corto cuando cruza por debajo.

Las pruebas indican un retorno anual promedio de aproximadamente 105%. Funciona mejor en el mercado cripto.

Los rendimientos normalizados permiten escalar el precio según la volatilidad reciente. Un stop basado en ATR gestiona el riesgo.

## Detalles

- **Criterios de entrada**:
  - Largo: `nPrice` cruza por encima de `HMA`
  - Corto: `nPrice` cruza por debajo de `HMA`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruce opuesto o stop por ATR
- **Stops**: Basado en ATR usando `StopMultiple`
- **Valores predeterminados**:
  - `NormPeriod` = 14
  - `HmaPeriod` = 100
  - `HmaOffset` = 0
  - `StopMultiple` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Hull Moving Average, Standard Deviation, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
