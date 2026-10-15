# Estrategia de Rompimiento de Envolvente y Pin Bar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia espera una vela martillo o una vela envolvente alcista y entra en largo al romper por encima del máximo de la señal. Para configuraciones bajistas, usa estrella fugaz o envolvente bajista y vende al romper por debajo del mínimo de la señal. El stop loss se coloca en el lado opuesto de la vela de señal y el take profit usa una relación riesgo/recompensa.

## Detalles

- **Criterios de entrada:** martillo o envolvente alcista seguido de ruptura por encima del máximo; estrella fugaz o envolvente bajista seguida de ruptura por debajo del mínimo.
- **Largo/Corto:** Ambos.
- **Criterios de salida:** stop en el lado opuesto de la vela de señal; take profit a múltiplo del riesgo.
- **Stops:** Sí.
- **Valores predeterminados:**
  - Ratio de beneficio largo = 5
  - Ratio de beneficio corto = 4
  - Porcentaje de riesgo = 0.02
  - Marco temporal de vela = 1 minuto
- **Filtros:**
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
