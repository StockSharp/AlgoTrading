# Estrategia EMA SAR Power
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia intradía combina medias móviles exponenciales rápidas y lentas con Parabolic SAR e indicadores Bulls/Bears Power. Solo opera durante las horas activas del mercado y requiere margen libre suficiente antes de entrar en cualquier posición.

El sistema va corto cuando la EMA rápida está por debajo de la EMA lenta, el Parabolic SAR se sitúa por encima del máximo de la vela, y Bears Power está subiendo mientras permanece negativo. Va largo cuando la EMA rápida está por encima de la EMA lenta, el Parabolic SAR está por debajo del mínimo de la vela, y Bulls Power está cayendo pero sigue positivo. Cada operación coloca un stop-loss amplio y un take-profit más cercano.

**Filtro Dinámico de Margen**

Antes de operar, la estrategia verifica el margen libre de la cartera. Dependiendo de su valor, el margen mínimo requerido aumenta escalonadamente: 600 → 1000 → 1300 → 1500 → 1800 → 2000 → 2500. La operación se omite cuando el margen libre cae por debajo del umbral actual.

## Detalles

- **Criterios de entrada**:
  - **Corto**: `EMA3 < EMA34` && `SAR > High` && `BearsPower < 0` && `BearsPower > BearsPower[1]`.
  - **Largo**: `EMA3 > EMA34` && `SAR < Low` && `BullsPower > 0` && `BullsPower < BullsPower[1]`.
- **Largo/Corto**: Ambos lados.
- **Stop/Objetivo**: Stop-loss en 2000 puntos, take-profit en 400 puntos.
- **Filtro de tiempo**: Opera solo entre las 09:00 y las 16:59 hora del bróker.
- **Indicadores**:
  - Medias Móviles Exponenciales (3, 34) sobre precio mediano.
  - Parabolic SAR (paso 0.02, máximo 0.2).
  - Bulls Power (13) y Bears Power (13).
- **Volumen predeterminado**: 30 contratos.
- **Marco temporal**: Velas de 15 minutos.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
