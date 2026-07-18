# Estrategia CMO Dúplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia es un puerto de StockSharp del experto MetaTrader 5 `Exp_CMO_Duplex.mq5`. Divide la lógica en dos patas independientes
(larga y corta) que ambas reaccionan a los cruces de la línea cero del Oscilador de Momentum Chande (CMO). Cada pata puede consumir su propia
serie de velas, período y desplazamiento de señal, lo que hace posible ejecutar configuraciones asimétricas en el mismo instrumento.

## Cómo funciona

- La estrategia se suscribe a uno o dos feeds de velas dependiendo de si las patas larga y corta usan el mismo `DataType`.
- Cada pata posee su propia instancia del indicador CMO. El indicador se evalúa solo en velas terminadas.
- La configuración `SignalBar` define cuántas velas completadas hacia atrás en el historial deben usarse para la lógica de cruce. Un valor de 0
  significa «usar la barra cerrada más reciente», `1` usa la barra anterior, `2` usa la barra anterior a esa, y así sucesivamente.
- **Pata larga:** cuando el valor CMO seleccionado cruza de por encima de cero a cero o por debajo, la estrategia entra (o cambia a) una posición
  larga si las entradas largas están permitidas. Las salidas largas se activan cuando el valor antiguo del CMO está por debajo de cero o cuando
  se tocan los niveles de stop loss / take profit.
- **Pata corta:** refleja la lógica larga. Un cruce de por debajo de cero a cero o por encima abre (o cambia a) una posición corta y
  el signo opuesto del valor CMO o los stops configurados cierran la posición.
- Los cambios de posición reutilizan `Volume` más cualquier exposición opuesta, por lo que una sola orden de mercado cierra la posición anterior y
  abre la nueva.
- `StartProtection()` está habilitado en el inicio, por lo que los controles de riesgo integrados de StockSharp permanecen activos.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `LongCandleType` | Tipo de vela usado por la pata larga. |
| `LongCmoPeriod` | Período del indicador CMO en el lado largo. |
| `LongSignalBar` | Número de barras cerradas entre el tiempo actual y la barra analizada para señales (0 = última barra cerrada). |
| `EnableLongEntries` | Permite o bloquea abrir nuevas posiciones largas. |
| `EnableLongExits` | Permite o bloquea cerrar posiciones largas en señales del oscilador. |
| `LongStopLossPoints` | Distancia del stop-loss en pasos de precio para operaciones largas (0 deshabilita el stop). |
| `LongTakeProfitPoints` | Distancia del take-profit en pasos de precio para operaciones largas (0 deshabilita el objetivo). |
| `ShortCandleType` | Tipo de vela usado por la pata corta. |
| `ShortCmoPeriod` | Período del indicador CMO en el lado corto. |
| `ShortSignalBar` | Número de barras cerradas entre el tiempo actual y la barra analizada para señales cortas. |
| `EnableShortEntries` | Permite o bloquea abrir nuevas posiciones cortas. |
| `EnableShortExits` | Permite o bloquea cerrar posiciones cortas en señales del oscilador. |
| `ShortStopLossPoints` | Distancia del stop-loss en pasos de precio para operaciones cortas (0 deshabilita el stop). |
| `ShortTakeProfitPoints` | Distancia del take-profit en pasos de precio para operaciones cortas (0 deshabilita el objetivo). |

La propiedad base `Strategy.Volume` controla el tamaño predeterminado de la orden. Cuando la estrategia necesita cambiar de dirección, envía una orden
de mercado cuyo volumen es igual a `Volume + |Position|`, lo que cierra la exposición antigua y abre la nueva en una sola transacción.

## Gestión de riesgos

- Los niveles de stop-loss y take-profit se evalúan en cada vela terminada. Para posiciones largas el stop se coloca por debajo de la entrada
  y el objetivo por encima; para posiciones cortas los niveles se invierten.
- Un stop o un objetivo activa una orden de mercado inmediata para cerrar la posición. La misma rutina de salida también se ejecuta cuando el valor
  del oscilador respectivo mantiene el signo incorrecto (por debajo de cero para largos, por encima de cero para cortos).
- Establecer la distancia en cero deshabilita la protección correspondiente y deja la pata gestionada puramente por la lógica del oscilador.

## Notas de uso

- La estrategia funciona mejor en instrumentos donde el CMO tiende a revertir después de tocar la línea cero. Las entradas contrarias están
  deliberadamente retrasadas por el desplazamiento `SignalBar` para coincidir con el experto original.
- Las patas larga y corta pueden compartir el mismo feed de velas u operar en diferentes marcos temporales. Si ambas usan el mismo `DataType`, la
  estrategia reutiliza una sola suscripción para mejor rendimiento.
- Dado que la estrategia opera en velas completadas, se recomienda suministrar un flujo continuo de velas (por ejemplo, a través de un
  backtest histórico o un feed en tiempo real) para evitar señales perdidas.
