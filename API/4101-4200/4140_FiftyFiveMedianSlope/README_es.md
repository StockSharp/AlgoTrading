# Cincuenta y cinco Mediana Pendiente Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Origen
- Convertido del asesor experto MetaTrader 4 **55_MA_med_FIN.mq4**.
- Se centra en la pendiente de una media móvil de 55 períodos calculada sobre los precios medios de las velas.

## Lógica comercial
- Se suscribe a la serie de velas configuradas (predeterminado: período de 1 hora) y procesa solo velas completadas.
- Calcula una media móvil sobre el precio medio (\((Alto + Bajo) / 2\)) utilizando el método seleccionado (SMA, EMA, SMMA o LWMA).
- Almacena los últimos valores de media móvil en un búfer circular para comparar el valor de hace una barra con el valor de hace `MaShift` barras.
- Cuando el valor de hace una barra es mayor que el valor de hace `MaShift` barras, la estrategia:
  - Cierra primero cualquier exposición corta.
  - Abre una posición larga si no se ha alcanzado el límite `MaxOrders`.
- Cuando el valor de hace una barra es menor que el valor de hace `MaShift` barras, refleja el comportamiento de las posiciones cortas.
- Las señales se alternan mediante banderas internas, por lo que la estrategia espera un cruce opuesto antes de volver a ingresar en la misma dirección.
- Solo se permite operar mientras la hora de apertura de la vela sea `StartHour < hour < EndHour`. Los límites son exclusivos para coincidir con la implementación original de MQL.

## Dimensionamiento de posiciones y gestión de riesgos.
- `FixedVolume` define el tamaño del lote por orden de mercado. Cuando se establece en cero, la estrategia cambia al tamaño basado en el riesgo utilizando `RiskPercentage` y el valor actual de la cartera.
- `MaxOrders` limita cuántas veces se puede apilar el volumen base en la misma dirección. Un valor de cero elimina el límite.
- Los `StopLossPoints` y `TakeProfitPoints` opcionales recrean las distancias de stop-loss y take-profit de MT4 a través de `StartProtection` utilizando incrementos de precios.

## Parámetros
- `FixedVolume` – tamaño del lote principal. Establezca en cero para habilitar el tamaño basado en porcentaje.
- `RiskPercentage`: fracción de la cartera asignada cuando `FixedVolume` es igual a cero.
- `TakeProfitPoints` / `StopLossPoints` – distancias de protección expresadas en incrementos de precio.
- `MaPeriod` – longitud de la media móvil mediana (predeterminado 55).
- `MaShift`: número de barras entre las instantáneas de media móvil reciente e histórica (predeterminado 13).
- `MaMethod` – tipo de cálculo de media móvil (simple, exponencial, suavizado, ponderado lineal).
- `StartHour` / `EndHour` – ventana de negociación exclusiva en tiempo de plataforma (0–23 horas).
- `MaxOrders` – entradas simultáneas máximas por dirección.
- `CandleType` – marco de tiempo utilizado para las velas de señal.

## Notas de uso
- Asegúrese de que el instrumento suscrito proporcione un `PriceStep` distinto de cero y metadatos de volumen para que la alineación del volumen coincida con los requisitos del intercambio.
- El dimensionamiento basado en el riesgo utiliza el valor actual de la cartera y el último precio de cierre. Si alguno de ellos no está disponible, la estrategia vuelve a tener un volumen cero (sin operaciones).
- La estrategia cancela la exposición opuesta antes de abrir una nueva posición, emulando el comportamiento original de MT4 de cerrar órdenes opuestas.
