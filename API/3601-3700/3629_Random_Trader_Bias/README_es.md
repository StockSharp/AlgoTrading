# Estrategia de comerciante de sesgo aleatorio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Random Bias Trader emula al asesor experto "random trader" de MetaTrader utilizando el API de alto nivel de StockSharp.
En cada vela terminada, la estrategia lanza una moneda virtual y abre una posición en esa dirección cuando no hay ninguna operación activa.
Los niveles de stop-loss y take-profit se derivan de ATR(10) o de una distancia de pip fija y se dimensionan según la relación recompensa-riesgo.
El tamaño de la posición se calcula a partir del porcentaje de riesgo configurado y se limita automáticamente por los límites de volumen del instrumento.
Un activador de equilibrio opcional puede mover el límite de pérdidas al precio de entrada una vez que se alcanza una ganancia de pip específica.

## Detalles
- **Datos**: suscripción a una vela definida por `CandleType`.
- **Criterios de entrada**:
  - Largo: No hay posición abierta, el lanzamiento de moneda devuelve largo. El precio de entrada es igual al último cierre.
  - Corto: No hay posición abierta, el lanzamiento de una moneda devuelve corto. El precio de entrada es igual al último cierre.
- **Criterios de salida**:
  - Stop-loss: la distancia es igual a `LossPipDistance` × tamaño del pip o `LossAtrMultiplier` × ATR(10) dependiendo de `LossType`.
  - Take-profit: distancia de parada multiplicada por `RewardRiskRatio`.
  - Punto de equilibrio: cuando está habilitado, mueve la parada a la entrada después de una ganancia de `BreakevenDistancePips`.
- **Paradas**: Stop-loss dinámico y toma de ganancias por operación, stop de equilibrio opcional.
- **Valores predeterminados**:
  - `CandleType` = período de tiempo de 1 minuto
  - `RewardRiskRatio` = 2,0
  - `LossType` = pipa
  - `LossAtrMultiplier` = 5,0
  - `LossPipDistance` = 20 puntos
  - `RiskPercentPerTrade` = 1%
  - `UseBreakeven` = Habilitado
  - `BreakevenDistancePips` = 10 puntos
  - `UseMaxMargin` = Habilitado
- **Filtros**:
  - Categoría: Aleatorio, neutral en cuanto a tendencias
  - Dirección: Ambas, determinadas por giro.
  - Indicadores: ATR(10) (opcional)
  - Complejidad: Principiante
  - Nivel de riesgo: Medio, depende del tamaño de la parada

## Notas
- Cuando el volumen basado en el riesgo se vuelve demasiado pequeño, la estrategia opcionalmente vuelve al volumen máximo negociable.
- Los niveles stop y objetivo se redondean al paso del precio del instrumento antes de realizar las órdenes.
- La lógica de equilibrio mantiene solo una posición abierta en cualquier momento, reflejando la lógica MetaTrader original.
