# Estratégia de Cruzamento de Zero CMO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base nos cruzamentos da linha zero do Oscilador de Momento Chande (CMO).
Quando o oscilador cruza abaixo de zero, uma posição comprada é aberta. Quando cruza acima de
zero, uma posição vendida é aberta. Níveis opcionais de stop loss e take profit (em pontos)
protegem a posição. As entradas e saídas para operações compradas e vendidas podem ser habilitadas
ou desabilitadas individualmente.

## Parâmetros

- `Volume` – volume da ordem.
- `CmoPeriod` – período para o indicador CMO.
- `StopLoss` – stop loss em pontos.
- `TakeProfit` – take profit em pontos.
- `AllowLongEntry` – permitir abertura de posições compradas.
- `AllowShortEntry` – permitir abertura de posições vendidas.
- `AllowLongExit` – permitir fechamento de posições compradas com sinal oposto.
- `AllowShortExit` – permitir fechamento de posições vendidas com sinal oposto.
- `CandleType` – período utilizado para os cálculos.

## Lógica de Negociação

1. Subscrever candles do período selecionado e calcular o CMO.
2. Quando o CMO cruza de cima para baixo de zero:
   - Fechar posições vendidas se permitido.
   - Abrir uma posição comprada se permitido.
3. Quando o CMO cruza de baixo para cima de zero:
   - Fechar posições compradas se permitido.
   - Abrir uma posição vendida se permitido.
4. Stop loss e take profit são aplicados usando ordens de proteção em pontos.

## Observações

- As decisões de negociação são tomadas apenas em candles completos.
- A estratégia usa a API de alto nível do StockSharp e vincula indicadores através de `Bind`.
