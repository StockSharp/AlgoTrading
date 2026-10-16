# Estrategia Santa Claus Rally
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta implementación opera **ventanas de días de cada mes**, no solo los últimos días de diciembre y los primeros de enero de la temporada navideña tradicional. La afirmación anterior de un retorno anual del 100% no está respaldada por el replay BTC/TON.

### Reglas mensuales ejecutadas (C# y Python)

Este ejemplo **generaliza** el patrón navideño a **todos los meses**, no exclusivamente a diciembre y enero. En el primer cierre de 5 minutos elegible de un nuevo día 25–31, abre un largo si está plano o cubre un corto. En días 3–7 cierra largos; en días 12–18 puede abrir corto, estando plano, si `Close < SMA20`. Las acciones requieren nuevo día y respetan 50 velas terminadas de espera. La protección nativa de pérdidas del 2% actúa sobre posiciones ejecutadas independientemente del calendario. La rentabilidad indicada arriba no ha sido validada con BTC/TON.


## Detalles

- **Criterios de entrada**: desencadenadores de efecto calendario
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Ambos
  - Indicadores: Estacionalidad
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

