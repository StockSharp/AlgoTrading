# Estratégia de Ciclo de Tendência Color Schaff MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma tradução do especialista MQL5 `Exp_ColorSchaffMFITrendCycle`.
Ela utiliza o indicador **Color Schaff MFI Trend Cycle**, que combina
valores do Índice de Fluxo de Dinheiro com um cálculo de duplo estocástico. O indicador
produz oito estados de cor representando zonas de momentum e sobrecompra/sobrevenda.

Lógica de negociação:

- Quando a cor anterior do indicador é **verde** (índices 6-7) e a cor atual
  cai abaixo da zona de forte tendência de alta, a estratégia fecha posições vendidas
  e abre uma nova posição comprada.
- Quando a cor anterior do indicador é **laranja** (índices 0-1) e a cor atual
  sobe acima da zona de forte tendência de queda, a estratégia fecha posições compradas
  e abre uma nova posição vendida.

Parâmetros:

- `FastMfiPeriod` – período do MFI rápido.
- `SlowMfiPeriod` – período do MFI lento.
- `CycleLength` – comprimento do buffer cíclico usado no indicador.
- `HighLevel` / `LowLevel` – limites de sobrecompra e sobrevenda para o valor STC.
- `CandleType` – período das velas de entrada (padrão 1 hora).

A estratégia usa a API de alto nível do StockSharp e processa apenas velas finalizadas.
