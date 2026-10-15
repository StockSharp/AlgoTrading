# Estrategia de la línea de la brújula
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica al experto CompassLine al fusionar dos filtros complementarios:

* **Seguir línea**: una ruta de ruptura de Bollinger bandas desplazada opcionalmente en ATR. Cuando el precio cierra fuera de las bandas, la trayectoria se extiende en la dirección de ruptura y nunca retrocede mientras persista la tendencia.
* **Brújula**: una transformación logística del precio medio en relación con el máximo más alto y el mínimo más bajo durante la ventana de promedio móvil. La señal bruta se suaviza dos veces (promedio triangular) para producir un estado alcista/bajista estable.

Una posición se abre sólo cuando ambos filtros coinciden en la tendencia. El filtrado de tiempo opcional y las paradas de protección reflejan la lógica MQL.

## Detalles

- **Criterios de entrada**:
  - La línea de seguimiento debe apuntar hacia arriba (cierre reciente por encima de la banda superior) para posiciones largas o hacia abajo (cierre reciente por debajo de la banda inferior) para posiciones cortas. El desplazamiento ATR se puede alternar con `UseAtrFilter`.
  - El estado de la brújula (basado en `CompassPeriod`) debe ser positivo para posiciones largas o negativo para posiciones cortas después de la fase de doble suavizado.
  - La negociación se ejecuta solo cuando el filtro de sesión opcional (`UseTimeFilter` con `Session` en HHmm-HHmm) lo permite.
- **Largo/Corto**: Se admiten ambas direcciones.
- **Criterios de salida**:
  - `CloseMode = None` mantiene la posición hasta que se produce una entrada opuesta o una parada de protección.
  - `CloseMode = BothIndicators` se cierra cuando tanto Seguir línea como Brújula invierten la dirección simultáneamente.
  - `CloseMode = FollowLineOnly` sale cuando Seguir línea cambia contra la posición.
  - `CloseMode = CompassOnly` sale cuando Compass cambia de polaridad.
- **Paradas**: Las distancias `TakeProfit` y `StopLoss` (en pasos de seguridad) se aplican después de cada ingreso cuando sean mayores a cero.
- **Valores predeterminados**:
  - `FollowBbPeriod` = 21
  - `FollowBbDeviation` = 1
  - `FollowAtrPeriod` = 5
  - `UseAtrFilter` = falso
  - `CompassPeriod` = 30 (longitud de suavizado = redondo(CompassPeriod / 3))
  - `CloseMode` = Ninguno
  - `UseTimeFilter` = falso
  - `Session` = "0000-2400"
  - `TakeProfit` = 0
  - `StopLoss` = 0
  - `CandleType` = Intervalo de tiempo.DesdeMinutos(15)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger Bandas, ATR, Media móvil triangular
  - Paradas: Opcionales
  - Complejidad: Intermedia
  - Plazo: Intradiario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: medio

## Notas adicionales

- El suavizado de Compass utiliza una ventana triangular igual a round(`CompassPeriod` / 3), que coincide estrechamente con la implementación del indicador original.
- Las cadenas de sesión como `0930-1600` restringen las operaciones a la ventana especificada y al mismo tiempo actualizan los estados de los indicadores fuera de la sesión.
- Las órdenes de protección reutilizan los ayudantes de alto nivel de StockSharp para que la lógica sea compatible con los módulos de gestión de riesgos de cartera.
