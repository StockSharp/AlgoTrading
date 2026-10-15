# Estratégia de Especialista em Ordens (1916)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre uma posição de mercado quando o preço do instrumento atinge níveis predefinidos. Ela imita o comportamento do especialista MQL original que gerenciava ordens via linhas no gráfico.

## Como funciona
- Assina velas de um período configurável.
- Quando o preço de fechamento cruza os limites `BuyLevel` ou `SellLevel`, abre uma posição comprada ou vendida a mercado.
- Os valores de stop-loss e take-profit são calculados a partir do preço de entrada usando `StopLossPip` e `TakeProfitPip`.
- Um trailing stop opcional move o stop-loss em direção ao preço atual conforme ele se move em direção favorável.

## Parâmetros
- **TakeProfitPip** – distância do preço de entrada ao take profit em pips.
- **StopLossPip** – distância do preço de entrada ao stop loss em pips.
- **EnableTrailingStop** – habilitar ou desabilitar a lógica de trailing stop.
- **CandleType** – tipo de vela utilizado para cálculos.
- **BuyLevel** – nível de preço que aciona a entrada comprada (0 desabilita).
- **SellLevel** – nível de preço que aciona a entrada vendida (0 desabilita).

## Observações
- A estratégia usa API de alto nível e processa apenas velas finalizadas.
- O subsistema de proteção é ativado na inicialização para evitar posições grandes acidentais.
