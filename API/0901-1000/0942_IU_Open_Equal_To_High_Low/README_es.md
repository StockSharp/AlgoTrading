# Estrategia IU Apertura Igual al Máximo o Mínimo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entra en largo en la primera vela del día cuando su apertura es igual a su mínimo, y entra en corto cuando la apertura es igual al máximo. El stop-loss usa la vela anterior y el take profit se basa en la relación `RiskReward`.

## Detalles

- **Criterios de entrada**:
  - **Largo**: la apertura de la primera vela es igual a su mínimo.
  - **Corto**: la apertura de la primera vela es igual a su máximo.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop-loss en el mínimo de la vela anterior para largo, máximo de la vela anterior para corto.
  - Take profit calculado desde el precio de entrada usando `RiskReward`.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RiskReward` = 2.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Acción del precio
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
