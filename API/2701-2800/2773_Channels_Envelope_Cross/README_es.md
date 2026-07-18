# Estrategia de Cruce de Canales con Envolvente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia de Cruce de Canales con Envolvente es un port directo del asesor experto de MetaTrader "Channels". El sistema opera velas horarias y monitorea una media móvil exponencial (EMA) rápida de dos períodos en relación con tres envolventes basadas en EMA (desviaciones de 0.3%, 0.7% y 1.0%) que se calculan a partir de una EMA lenta de 220 períodos. Las rupturas de la EMA rápida a través de estas envolventes generan entradas direccionales, mientras que un filtro de tiempo opcional restringe el trading a horas específicas.

## Lógica de trading

1. **Pila de indicadores**
   - EMA rápida (longitud 2) calculada sobre precios de cierre de vela.
   - EMA rápida (longitud 2) calculada sobre precios de apertura de vela.
   - EMA lenta (longitud 220) calculada sobre precios de cierre de vela.
   - Tres niveles de envolvente derivados de la EMA lenta con desviaciones de 0.3%, 0.7% y 1.0%.
2. **Configuración larga**
   - Se activa cuando la EMA rápida de cierre cruza por encima de la envolvente inferior del 1.0% o 0.7%, permanece por debajo de la envolvente inferior del 0.3% durante dos barras consecutivas, cruza por encima de la EMA lenta, o rompe las envolventes superiores del 0.3% o 0.7%. Cualquiera de estas condiciones puede activar una entrada larga cuando no hay posición abierta.
3. **Configuración corta**
   - Se activa cuando la EMA rápida de apertura cruza por debajo de cualquiera de las envolventes superiores, cae por debajo de la EMA lenta, o perfora las envolventes inferiores desde arriba. Cualquiera de estas condiciones puede activar una entrada corta cuando no hay posición abierta.
4. **Gestión de riesgos**
   - Los niveles fijos de stop-loss y take-profit (por lado) se expresan en pips y se convierten a distancia de precio usando el tamaño de tick del instrumento. Si las entradas se establecen en cero, el nivel respectivo no se aplica.
   - Los trailing stops independientes para posiciones largas y cortas mueven el stop de protección más cerca del precio de mercado cuando la ganancia supera la distancia de trailing más un incremento de paso configurable.
5. **Filtro de tiempo**
   - Cuando está habilitado, la estrategia solo procesa entradas durante el rango de horas inclusivo configurado. Las posiciones todavía se gestionan cuando el filtro está activo.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Tamaño de la orden usado para entradas de mercado (lotes o contratos dependiendo del instrumento). |
| `UseTradeHours` | Habilita el filtro de tiempo para las entradas. |
| `FromHour` / `ToHour` | Horas de inicio y fin inclusivas para la ventana de trading (admite rangos nocturnos). |
| `StopLossBuyPips` / `StopLossSellPips` | Distancia del stop-loss para operaciones largas/cortas expresada en pips. |
| `TakeProfitBuyPips` / `TakeProfitSellPips` | Distancia del take-profit para operaciones largas/cortas expresada en pips. |
| `TrailingStopBuyPips` / `TrailingStopSellPips` | Distancia del trailing stop en pips para operaciones largas/cortas. |
| `TrailingStepPips` | Incremento mínimo (en pips) requerido para mover un trailing stop. |
| `CandleType` | Serie de velas usada para cálculos (por defecto marco temporal de 1 hora). |

## Gestión de posiciones

- En la entrada, la estrategia almacena el precio de ejecución, calcula los objetivos de stop-loss y take-profit en unidades de precio absoluto y restablece los niveles de trailing.
- Mientras una posición larga está abierta, el stop-loss se sigue hacia arriba cada vez que la ganancia supera `TrailingStopBuyPips + TrailingStepPips`. La estrategia sale en el stop-loss o take-profit, el que se alcance primero.
- Mientras una posición corta está abierta, el stop-loss se sigue hacia abajo usando los parámetros de trailing del lado corto y las salidas se ejecutan simétricamente.

## Notas

- El tamaño del pip se deriva del tamaño del tick del instrumento. Para instrumentos de tres o cinco decimales, el pip se multiplica por diez para emular la lógica de MetaTrader.
- La estrategia trabaja con una sola posición a la vez. Una nueva entrada solo se coloca después de que la posición existente se haya cerrado.
- Habilite `StartProtection` en la clase base para protegerse contra posiciones abiertas inesperadas después de reinicios (ya llamado en la implementación).
