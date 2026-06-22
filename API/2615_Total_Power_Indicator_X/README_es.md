# Estrategia del Indicador de Potencia Total X
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia recrea el comportamiento del experto MetaTrader "Exp_TotalPowerIndicatorX" usando las APIs de alto nivel de StockSharp. Se basa en una implementación personalizada del Indicador de Potencia Total que mide el dominio de toros y osos contando cuántas velas en una ventana deslizante cierran por encima o por debajo de una línea base EMA interna. Las decisiones de trading se toman cuando las líneas de fortaleza alcista y bajista se cruzan entre sí.

El indicador funciona con cualquier símbolo y marco temporal. Por defecto la estrategia se suscribe a velas de 4 horas, coincidiendo con la configuración del asesor experto original, pero el marco temporal puede ajustarse a través de un parámetro.

## Lógica de trading
1. Para cada vela finalizada, la estrategia alimenta el Indicador de Potencia Total con los datos de la vela. El indicador:
   - Calcula una EMA con período **Power Period**.
   - Cuenta cuántas velas dentro de **Lookback Period** tuvieron `High > EMA` (toros) y `Low < EMA` (osos).
   - Convierte los conteos en valores de fortaleza al estilo porcentual en el rango 0–100.
2. Un **cruce alcista** (fortaleza alcista subiendo por encima de la bajista) desencadena una entrada larga cuando el trading largo está habilitado y no hay posiciones abiertas.
3. Un **cruce bajista** (fortaleza bajista subiendo por encima de la alcista) desencadena una entrada corta cuando el trading corto está habilitado y no hay posiciones abiertas.
4. Los cruces opuestos cierran posiciones existentes cuando los interruptores de salida relevantes están habilitados.
5. Un filtro de sesión de trading opcional fuerza el cierre de todas las posiciones fuera de la ventana de tiempo configurada y deshabilita nuevas entradas durante ese período.
6. Los niveles opcionales de stop-loss y take-profit se expresan en múltiplos del paso de precio de la seguridad. Se recalculan después de cada entrada y salen tan pronto como el máximo o mínimo de la vela supera el nivel.

## Parámetros
- **Candle Type** – marco temporal usado para los cálculos del indicador. Por defecto: velas de 4 horas.
- **Power Period** – longitud de la EMA dentro del indicador; refleja el input MQL. Por defecto: 10.
- **Lookback** – número de velas usadas para contar el dominio alcista y bajista. Por defecto: 45.
- **Volume** – tamaño de la orden enviada al exchange o simulador. Por defecto: 1.
- **Enable Long Entry / Enable Short Entry** – permitir o prohibir nuevas posiciones en la dirección correspondiente.
- **Enable Long Exit / Enable Short Exit** – cerrar posiciones en señales opuestas. Deshabilitar para mantener posiciones abiertas hasta cierre manual o stop-out.
- **Use Trading Hours** – habilitar el filtro de tiempo. Cuando está activo, la estrategia opera solo entre **Start Hour/Minute** y **End Hour/Minute** y cierra cualquier posición abierta fuera de ese intervalo. Se admiten ventanas nocturnas (inicio posterior al fin).
- **Stop Loss Points / Take Profit Points** – distancias desde el precio de entrada medidas en pasos de precio. Establecer en cero para deshabilitar el nivel. El cálculo usa `Security.PriceStep`, por lo que asegúrese de que los metadatos de la seguridad estén disponibles.

## Notas
- La estrategia abre una nueva posición solo cuando no hay ninguna posición activa en la seguridad, emulando el comportamiento del experto original.
- Dado que los cálculos de stop-loss y take-profit dependen del paso de precio del instrumento, ejecutar la estrategia sin esos metadatos mantiene los niveles protectores deshabilitados automáticamente.
- El valor del indicador se traza en el área del gráfico cuando la interfaz de usuario está disponible, lo que ayuda a visualizar los cruces entre la fortaleza alcista y bajista.
