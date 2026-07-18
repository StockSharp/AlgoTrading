# Inversión de tendencia diaria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Daily Trend Reversal es una adaptación del MetaTrader 4 asesor experto `dailyTrendReversal_D1`. La estrategia ancla las operaciones intradiarias a la apertura, máximo y mínimo del día actual, y solo participa cuando tanto la acción del precio como el índice del canal de productos básicos (CCI) confirman el mismo sesgo direccional. El comercio se limita a una sesión GMT configurable, opcionalmente se detiene después de alcanzar un objetivo de ganancias diario y puede salir de posiciones inmediatamente cuando los filtros cambian al lado opuesto.

## Lógica estratégica
### Filtros de sesgo diarios
* **Pasos direccionales**: la estrategia evalúa hasta tres condiciones para validar el sesgo diario:
  1. La distancia desde el precio actual hasta el extremo diario debe superar un umbral de riesgo expresado en pips.
  2. La distancia desde la apertura hasta el extremo opuesto también debe exceder el umbral de riesgo y el precio debe permanecer dentro de los 10 pips de la apertura diaria.
  3. (Opcional) La vela actual debe cerrarse en la dirección del movimiento mientras el precio aún se encuentra dentro de los 10 pips de la apertura diaria.
* **Dominancia de rango**: compara la distancia desde la apertura hasta la máxima versus la apertura hasta la mínima. El lado más largo define la tendencia activa.
* **CCI tendencia**: los últimos tres valores CCI finales deben aumentar monótonamente (para posiciones largas) o disminuir (para posiciones cortas).

### Reglas de entrada
* **Entradas largas**
  * Permitido solo durante la ventana de negociación GMT configurada en días hábiles.
  * El precio actual debe estar por encima de la apertura diaria, los pasos direccionales deben confirmar una tendencia alcista, el dominio del rango debe favorecer el alza y la tendencia CCI debe ser creciente.
  * Solo abre una posición larga si la posición neta es plana o corta (la exposición corta se cierra como parte de la reversión a larga).
* **Entradas cortas**
  * Condiciones reflejadas: precio por debajo de la apertura diaria, los pasos direccionales confirman una tendencia bajista, el dominio del rango favorece la caída y la tendencia CCI está disminuyendo.
  * Sólo se abre cuando la posición neta es plana o larga.

### reglas de salida
* **Take Profit fijo/stop loss** – expresado en pips en relación con la entrada. Un valor de `0` desactiva el nivel respectivo.
* **Control de sesión y retención**: una vez que se alcanza la hora de cierre GMT o transcurre el tiempo de retención en horas, las posiciones rentables se cierran inmediatamente. Las operaciones perdedoras cambian a un modo de equilibrio y se cierran tan pronto como el precio vuelve a la entrada.
* **Salida de reversión (opcional)**: si está habilitada, las posiciones largas se cierran cuando los filtros bajistas se alinean (el precio está por debajo de las tendencias de apertura y diaria/CCI apuntando hacia abajo); Los pantalones cortos se cierran simétricamente cuando los filtros ascendentes se alinean.
* **Parada de ganancias diarias**: combina las ganancias obtenidas desde la primera operación del día con PnL flotante. Cuando se alcanza el umbral configurado, todas las posiciones se cierran y las nuevas entradas se suspenden hasta que el parámetro se vuelva a habilitar manualmente.

## Parámetros
* **Auto Trading**: alterna si la estrategia puede abrir nuevas operaciones.
* **Salida de reversión**: permite salidas inmediatas cuando se confirma la tendencia diaria opuesta.
* **Pasos de tendencia**: selecciona cuántos filtros de pasos (1 a 3) deben pasar para validar el sesgo diario.
* **Volumen** – volumen de pedidos para entradas al mercado.
* **Take Profit (pips)** – distancia objetivo de ganancia fija; configúrelo en `0` para deshabilitarlo.
* **Stop Loss (pips)** – distancia de parada protectora; configúrelo en `0` para deshabilitarlo.
* **Parada de ganancias**: objetivo de ganancias en unidades de precio que detiene la negociación por el resto del día; `0` desactiva la función.
* **GMT Diff** – hora del gráfico menos GMT (en horas). Se utiliza para convertir los límites de la sesión GMT en tiempo del gráfico.
* **Hora de inicio/hora de finalización**: horas GMT que limitan la ventana de negociación para nuevas posiciones.
* **Hora de cierre** – Hora GMT después de la cual la estrategia fuerza las salidas o arma la lógica de equilibrio.
* **Horas de tenencia**: cantidad máxima de tiempo que una operación puede permanecer abierta antes de que se active la lógica de la sesión.
* **Riesgo (pips)** – distancia de pips utilizada por los pasos direccionales.
* **CCI Período**: número de períodos para el índice del canal de productos básicos.
* **Tipo de vela**: período de tiempo que impulsa los cálculos (predeterminado: velas de 15 minutos).

## Notas adicionales
* La estrategia detecta el tamaño del pip a partir del paso del precio del valor. Los símbolos FX de cinco y tres dígitos convierten automáticamente las distancias de pips configuradas en incrementos de precio.
* El seguimiento de ganancias diarias se reinicia con la primera vela de cada nuevo día de negociación al capturar el PnL realizado actual como nueva línea de base.
* No existe una implementación de Python para esta estrategia; sólo la versión C# se proporciona en el paquete API.
