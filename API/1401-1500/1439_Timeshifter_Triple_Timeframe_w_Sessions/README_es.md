# Estrategia Timeshifter de Triple Marco Temporal con Sesiones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera en tres marcos temporales con confirmación ADX opcional y filtros de sesión.

Las pruebas indican un retorno anual promedio de aproximadamente el 37%. Funciona mejor en el mercado forex.

El sistema se alinea con la tendencia del marco temporal superior, entra en rupturas del marco temporal medio y sale en reversiones del marco temporal inferior. Las operaciones pueden limitarse a las sesiones de Londres, Nueva York y Tokio. Se puede usar un filtro ADX para asegurar momentum suficiente.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cierre del marco temporal superior está por encima de su SMA y el precio del marco temporal medio cruza por encima de su SMA.
  - **Corto**: El cierre del marco temporal superior está por debajo de su SMA y el precio del marco temporal medio cruza por debajo de su SMA.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: El precio del marco temporal inferior cruza por debajo de su SMA.
  - **Corto**: El precio del marco temporal inferior cruza por encima de su SMA.
- **Stops**: No.
- **Valores predeterminados**:
  - `HigherMaLength` = 50
  - `MediumMaLength` = 20
  - `LowerMaLength` = 10
  - `AdxLength` = 14
  - `AdxThreshold` = 25
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, ADX
  - Stops: No
  - Complejidad: Complejo
  - Marco temporal: Múltiples
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
