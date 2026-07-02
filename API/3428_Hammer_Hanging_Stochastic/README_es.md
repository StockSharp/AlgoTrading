# Martillo colgando Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia transfiere el MetaTrader experto "Expert_AH_HM_Stoch" al API de alto nivel de StockSharp. Combina patrones de velas de martillo y hombre colgado con confirmación de oscilador estocástico para capturar configuraciones de reversión después de movimientos prolongados.

La estrategia espera a que se complete una vela antes de actuar, utiliza la línea de señal estocástica para filtrar y cierra posiciones cuando el impulso sale de las zonas extremas.

## Detalles

- **Criterios de entrada**:
  - Largo: Vela martillo alcista y %D estocástico (barra anterior) por debajo del nivel de sobreventa.
  - Corto: Vela colgante bajista y %D estocástico (barra anterior) por encima del nivel de sobrecompra.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cerrar posiciones cuando el estocástico %D cruce por encima/por debajo de la recuperación configurable y los niveles extremos.
- **Se detiene**: habilitado a través del gancho integrado `StartProtection()` (el valor predeterminado es protección a nivel de cuenta).
- **Valores predeterminados**:
  - `CandleType` = Intervalo de tiempo.Desdehoras(1)
  - `StochPeriodK` = 15
  - `StochPeriodD` = 49
  - `StochPeriodSlow` = 25
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `ExitLowerLevel` = 20
  - `ExitUpperLevel` = 80
  - `MaxBodyRatio` = 0,35
  - `LowerShadowMultiplier` = 2,5
  - `UpperShadowMultiplier` = 0,3
- **Filtros**:
  - Categoría: Patrón + Confirmación de oscilador
  - Dirección: Ambos
  - Indicadores: Vela japonesa, Stochastic
  - Paradas: Controles de riesgo opcionales vía `StartProtection`
  - Complejidad: Intermedia
  - Plazo: Swing / Intradiario (1h por defecto)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: moderado

## Cómo funciona

1. Se suscribe a la serie de velas configuradas y al oscilador estocástico utilizando el `BindEx` API de alto nivel.
2. Detecta formaciones de martillos y hombres colgados según las proporciones del cuerpo y las sombras.
3. Confirma las entradas con la línea estocástica %D utilizando el valor de la barra cerrada anterior.
4. Gestiona las salidas cuando el estocástico sale de las zonas de sobreventa/sobrecompra, reflejando la lógica del experto original MQL.
5. Proporciona visualización de gráficos para velas, estocásticos y operaciones propias cuando hay un área de gráfico disponible.
