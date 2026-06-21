# Estrategia Simple Bars
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Simple Bars replica el comportamiento del experto MQL5 original `Exp_SimpleBars`. Utiliza el indicador *SimpleBars* para determinar la tendencia actual comparando la última vela con los máximos y mínimos recientes. Cuando el indicador detecta un cambio de tendencia, la estrategia ejecuta una operación en la apertura de la siguiente barra.

## Detalles

- **Criterios de entrada**
  - **Largo**: La señal del indicador en la barra anterior es *buy*.
  - **Corto**: La señal del indicador en la barra anterior es *sell*.
- **Largo/Corto**: Se operan ambas direcciones.
- **Criterios de salida**
  - La posición se invierte cuando aparece una señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**
  - `Period` = 6 barras.
  - `UseClose` = `true` (se utiliza el precio de cierre para la comparación).
  - `CandleType` = velas de 6 horas.
- **Filtros**
  - Categoría: Seguimiento de tendencia.
  - Dirección: Ambos.
  - Indicadores: Personalizado.
  - Stops: No.
  - Complejidad: Moderado.
  - Marco temporal: Medio plazo.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Medio.
