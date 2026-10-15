# Estrategia AfterEffects
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia AfterEffects se basa en la idea de que los precios del mercado pueden mostrar efectos residuales.
Calcula una señal utilizando el precio de cierre actual y las aperturas de `p` y `2p` barras atrás:

`signal = Close - 2 * Open[p] + Open[2p]`

Una señal positiva abre una posición larga, mientras que una señal negativa abre una posición corta.
Configurar `Random` en verdadero invierte la señal.

Una vez en posición, la estrategia coloca un stop-loss a `StopLoss` puntos del punto de entrada.
Cuando el precio se mueve `2 * StopLoss` puntos en la dirección favorable:

- si la señal cambia de signo, la posición se revierte operando con el doble del volumen;
- de lo contrario, el stop-loss se ajusta al nuevo nivel.

## Detalles

- **Criterios de entrada**: `signal > 0` para largo, `signal < 0` para corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop-loss.
- **Stops**: Trailing.
- **Valores predeterminados**:
  - `StopLoss` = 500
  - `Period` = 3
  - `Random` = false
  - `Volume` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Fórmula personalizada
  - Stops: Trailing
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
