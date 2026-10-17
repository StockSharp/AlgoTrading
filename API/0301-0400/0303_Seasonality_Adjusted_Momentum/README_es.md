# Estrategia de Momentum Ajustado por Estacionalidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina momentum con rendimientos mensuales históricos. Sobre velas completas de 5 minutos calcula `M = Momentum(14)` y `A = SMA(14)` de M. Sin posición, compra si `S >= SeasonalityThreshold` y `M > A`; vende si `S <= -SeasonalityThreshold` y `M < A`. `SeasonalityThreshold = 0.5` por defecto, en puntos porcentuales.

Cierra largos si deja de cumplirse su condición estacional o `M < A`, y cortos si falla su condición o `M > A`. Cada orden de señal impone `CooldownBars = 120` velas completas de pausa para nuevas decisiones. Las posiciones ejecutadas tienen protección nativa local separada `StopLossPercent = 2` desde el precio real, con bid/ask y salida a mercado sin esperar la pausa.

Requiere `SeasonalityHistoryFile`: CSV UTF-8 de aperturas y cierres mensuales con cabecera `SecurityId,Month,Open,Close`. `Month` usa `yyyy-MM`, los decimales un punto, los precios deben ser positivos y cada mes debe ser único para el instrumento. Ignora otros identificadores. La falta de datos, filas mal formadas o ausencia de observaciones coincidentes impiden iniciar.

En cada vela de señal, `S` es la media de `100 × (Close / Open − 1)` para ese mismo mes en años anteriores, dentro de `SeasonalityLookbackYears = 10`. Exige `MinimumSeasonalSamples = 2` observaciones. Excluye el mes actual y los futuros aunque aparezcan en el archivo. Sin historial suficiente no abre posiciones. Deben aportarse precios mensuales reales del instrumento; no hay una tabla fija de puntuaciones.
