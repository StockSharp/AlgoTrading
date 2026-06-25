# Estrategia BykovTrend + ColorX2MA MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de StockSharp reproduce el experto MQL5 `Exp_BykovTrend_ColorX2MA_MMRec`. Combina dos módulos independientes:
BykovTrend, que colorea velas con un filtro Williams %R, y ColorX2MA, que inspecciona la pendiente de una media móvil de doble suavizado.
Las entradas se emiten cada vez que el módulo seleccionado detecta un nuevo cambio de color/pendiente y la gestión del dinero se simplifica
para usar el volumen de la estrategia. Los stop-loss y take-profit porcentuales opcionales pueden habilitarse a través del bloque de
protección integrado de StockSharp.

## Lógica de la estrategia

### Módulo BykovTrend
- Usa un Williams %R (`BykovTrendWprLength`) calculado sobre `BykovTrendCandleType` (predeterminado velas de 2 horas).
- `BykovTrendRisk` controla los umbrales alcista/bajista (`33 - Risk` y `-Risk`).
- El color del indicador se evalúa en la barra `BykovTrendSignalBar` (desplazamiento desde la barra cerrada más reciente).
- Un color alcista (< 2) cierra cortos si `AllowBykovTrendCloseSell` está habilitado y puede abrir largos si
  `EnableBykovTrendBuy` es verdadero y el color anterior no era alcista.
- Un color bajista (> 2) cierra largos si `AllowBykovTrendCloseBuy` está habilitado y puede abrir cortos si
  `EnableBykovTrendSell` es verdadero y el color anterior no era bajista.

### Módulo ColorX2MA
- Se aplican dos etapas de suavizado (`ColorX2MaMethod1`, `ColorX2MaLength1` y `ColorX2MaMethod2`, `ColorX2MaLength2`) sobre
  el precio definido por `ColorX2MaPriceType` usando velas de `ColorX2MaCandleType`.
- La salida de la segunda etapa se compara con el valor anterior para generar estados de pendiente: subiendo (1), bajando (2) o plano (0).
- El estado de pendiente se evalúa en la barra `ColorX2MaSignalBar` (desplazamiento desde la última barra cerrada).
- Una pendiente ascendente cierra cortos (`AllowColorX2MaCloseSell`) y puede abrir largos (`EnableColorX2MaBuy`) si la pendiente anterior
  no estaba ya subiendo.
- Una pendiente descendente cierra largos (`AllowColorX2MaCloseBuy`) y puede abrir cortos (`EnableColorX2MaSell`) si la pendiente anterior
  no estaba ya bajando.

### Gestión de operaciones
- Las señales de cierre se ejecutan antes de las aperturas para emular la secuencia de órdenes del experto original.
- Las órdenes usan `Strategy.Volume` como tamaño de posición; el complejo recontador de gestión de dinero de la versión MQL no
  se replica.
- `StopLossPercent` y `TakeProfitPercent` activan `StartProtection` con salidas basadas en porcentaje cuando son mayores que cero.

## Detalles

- **Largo/Corto**: Ambas direcciones soportadas.
- **Criterios de entrada**:
  - Transición de color alcista de BykovTrend.
  - Transición de pendiente ascendente de ColorX2MA.
- **Criterios de salida**:
  - Color/pendiente opuesto dependiendo de los módulos habilitados.
  - Stop-loss/take-profit porcentual opcional.
- **Filtros**: Ninguno más allá de la lógica del indicador.
- **Dimensionamiento de posición**: Fijo mediante `Strategy.Volume`.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `EnableBykovTrendBuy` | Permitir que BykovTrend abra operaciones largas. | `true` |
| `EnableBykovTrendSell` | Permitir que BykovTrend abra operaciones cortas. | `true` |
| `AllowBykovTrendCloseBuy` | Cerrar largos cuando BykovTrend se vuelve bajista. | `true` |
| `AllowBykovTrendCloseSell` | Cerrar cortos cuando BykovTrend se vuelve alcista. | `true` |
| `BykovTrendRisk` | Sensibilidad del Williams %R (valores más pequeños reaccionan más rápido). | `3` |
| `BykovTrendWprLength` | Período del Williams %R. | `9` |
| `BykovTrendSignalBar` | Índice de barra (desplazamiento) para evaluar el color de BykovTrend. | `1` |
| `BykovTrendCandleType` | Tipo/marco temporal de vela para BykovTrend. | `2h` |
| `EnableColorX2MaBuy` | Permitir que ColorX2MA abra operaciones largas. | `true` |
| `EnableColorX2MaSell` | Permitir que ColorX2MA abra operaciones cortas. | `true` |
| `AllowColorX2MaCloseBuy` | Cerrar largos cuando la pendiente de ColorX2MA se vuelve bajista. | `true` |
| `AllowColorX2MaCloseSell` | Cerrar cortos cuando la pendiente de ColorX2MA se vuelve alcista. | `true` |
| `ColorX2MaMethod1` | Tipo de media móvil para la etapa 1. | `Simple` |
| `ColorX2MaLength1` | Período para el suavizado de la etapa 1. | `12` |
| `ColorX2MaPhase1` | Marcador de posición de fase mantenido para documentación (no usado). | `15` |
| `ColorX2MaMethod2` | Tipo de media móvil para la etapa 2. | `Jurik` |
| `ColorX2MaLength2` | Período para el suavizado de la etapa 2. | `5` |
| `ColorX2MaPhase2` | Marcador de posición de fase mantenido para documentación (no usado). | `15` |
| `ColorX2MaPriceType` | Fuente de precio para el suavizado de ColorX2MA. | `Close` |
| `ColorX2MaSignalBar` | Índice de barra (desplazamiento) para evaluar el estado de pendiente. | `1` |
| `ColorX2MaCandleType` | Tipo/marco temporal de vela para ColorX2MA. | `2h` |
| `StopLossPercent` | Stop protector opcional en porcentaje (0 deshabilita). | `0` |
| `TakeProfitPercent` | Take-profit protector opcional en porcentaje (0 deshabilita). | `0` |

## Notas

- `ColorX2MaPhase1` y `ColorX2MaPhase2` se conservan para reflejar los inputs originales pero no se consumen porque las
  implementaciones de medias móviles de StockSharp no exponen un parámetro de fase.
- Solo se proporcionan los métodos de suavizado disponibles en StockSharp; las opciones de SmoothAlgorithms no compatibles vuelven al
  análogo más cercano.
- Los recontadores de gestión de dinero de `TradeAlgorithms.mqh` no están portados; el dimensionamiento de posición debe gestionarse mediante
  controles de riesgo externos o lógica personalizada en StockSharp.

## Uso

1. Asignar el instrumento deseado y establecer `Strategy.Volume` al tamaño de lote que se desea operar.
2. Configurar los tipos de vela para BykovTrend y ColorX2MA si el marco temporal predeterminado de 2 horas no es apropiado.
3. Ajustar métodos/longitudes de suavizado y desplazamientos de barra de señal para coincidir con la configuración original o las propias pruebas.
4. Opcionalmente habilitar el bloque de protección estableciendo `StopLossPercent` y/o `TakeProfitPercent` mayor que cero.
5. Iniciar la estrategia; se suscribirá a los flujos de velas configurados, monitoreará ambos módulos y emitirá órdenes de mercado en la
   secuencia definida anteriormente.
