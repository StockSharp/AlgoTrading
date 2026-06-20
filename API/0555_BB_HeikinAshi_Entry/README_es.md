# Entrada BB HeikinAshi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de Bollinger Bands que utiliza velas Heikin Ashi.

El sistema espera dos o tres barras Heikin Ashi bajistas consecutivas que toquen la banda inferior de Bollinger. Una vela alcista que cierre de vuelta por encima de la banda activa una entrada larga. Los cortos funcionan en la dirección opuesta. La mitad de la posición se cierra en el primer objetivo y el resto se protege con un stop trailing.

## Detalles

- **Criterios de entrada**: Reversión de velas Heikin Ashi consecutivas alrededor de Bollinger Bands.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Toma parcial de beneficios y stop trailing.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `BollingerLength` = 20
  - `BollingerWidth` = 2
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Heikin Ashi, Bollinger Bands
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
