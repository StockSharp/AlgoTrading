# AH HM RSI Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una adaptación StockSharp del experto MetaTrader **Expert_AH_HM_RSI**. Busca patrones de velas de martillo o de hombre colgado y requiere una señal de confirmación del índice de fuerza relativa (RSI) antes de operar. El enfoque refleja el Asesor Experto original, incluida su filosofía de gestión de riesgos de revertir posiciones cuando aparece una nueva señal.

## Lógica de trading
1. **Filtro de tendencias**: se utiliza una media móvil simple corta (longitud predeterminada 2) para determinar si el mercado se encuentra en una micro tendencia bajista o alcista.
2. **Patrón de vela**: la estrategia analiza la vela completada más reciente:
   - Se detecta un **martillo** cuando el cuerpo se ubica en el tercio superior del rango, la vela tiene un hueco más bajo que la barra anterior y el punto medio de la vela está por debajo de la tendencia de la media móvil.
   - Se detecta un **hombre ahorcado** cuando el cuerpo se ubica en el tercio superior, la vela tiene un hueco más alto que la barra anterior y el punto medio de la vela está por encima de la tendencia de la media móvil.
3. **RSI Filtro** –
   - Las operaciones largas requieren que RSI esté por debajo del umbral de martillo configurable (predeterminado 40).
   - Las operaciones cortas requieren que RSI esté por encima del umbral del ahorcado (predeterminado 60).
4. **Ejecución comercial**: ante una señal válida, la estrategia ingresa con `Volume + |Position|`, por lo que las posiciones abiertas se revierten inmediatamente cuando llega la configuración opuesta.
5. **Reglas de salida**: las posiciones se aplanan cuando el RSI cruza los límites configurables inferior (predeterminado 30) o superior (predeterminado 70) en la dirección opuesta, replicando los votos de salida en el código original.

## Indicadores
- **RelativeStrengthIndex** (longitud 33 de forma predeterminada).
- **SimpleMovingAverage** (longitud 2 por defecto) aplicada a los cierres de velas.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Volume` | Tamaño del pedido utilizado para las entradas. | `1` |
| `RsiPeriod` | RSI período retrospectivo. | `33` |
| `MaPeriod` | Período de media móvil para el filtro de tendencias. | `2` |
| `HammerRsiThreshold` | Valor máximo RSI que permite una entrada larga como martillo. | `40` |
| `HangingManRsiThreshold` | Valor mínimo RSI que permite una entrada corta del ahorcado. | `60` |
| `LowerExitLevel` | RSI límite utilizado para cerrar cortos después de un cruce alcista. | `30` |
| `UpperExitLevel` | RSI límite utilizado para cerrar posiciones largas después de un cruce a la baja. | `70` |
| `CandleType` | Plazo procesado por la estrategia. | `1 hour` velas |

Todos los parámetros se pueden optimizar a través de la interfaz de usuario del parámetro StockSharp.

## Notas de uso
- La lógica funciona exclusivamente con velas terminadas. Asegúrese de que el período de tiempo seleccionado y la fuente de datos produzcan barras completas.
- Debido a que la lógica de reversión siempre opera con `Volume + |Position|`, las posiciones cambian de dirección instantáneamente en la señal opuesta, coincidiendo con el Asesor Experto.
- Inicie la gestión de riesgos integrada una vez en el lanzamiento (`StartProtection()` se llama en `OnStarted`).

## Archivos
- `CS/AhHmRsiStrategy.cs` – Implementación de la estrategia.
- `README.md` – Documentación en inglés.
- `README_zh.md` – Documentación china.
- `README_ru.md` – Documentación rusa.
