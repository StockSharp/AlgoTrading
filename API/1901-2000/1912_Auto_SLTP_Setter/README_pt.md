# Estratégia de Definição Automática de SLTP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia utilitária que anexa automaticamente ordens de stop-loss e take-profit a posições abertas quando estão ausentes. As distâncias podem ser definidas como valores fixos em pips ou como múltiplos do Average True Range (ATR).

## Parâmetros

- `Candle Type` – período utilizado para o cálculo do ATR.
- `Set Stop Loss` – habilitar a colocação automática do stop-loss.
- `Set Take Profit` – habilitar a colocação automática do take-profit.
- `Stop Loss Method` – 1 = pips fixos, 2 = múltiplo de ATR.
- `Fixed SL (pips)` – distância do stop-loss em pips para o método fixo.
- `SL ATR Multiplier` – multiplicador de ATR para o stop-loss ao usar o método ATR.
- `Take Profit Method` – 1 = pips fixos, 2 = múltiplo de ATR.
- `Fixed TP (pips)` – distância do take-profit em pips para o método fixo.
- `TP ATR Multiplier` – multiplicador de ATR para o take-profit ao usar o método ATR.
- `ATR Period` – número de períodos utilizados para o cálculo do ATR.

## Como funciona

1. Na inicialização, a estratégia avalia a configuração.
2. Se valores baseados em ATR forem solicitados, ela se inscreve na série de velas especificada e calcula o ATR.
3. Após o valor do ATR estar disponível, a estratégia chama `StartProtection` com as distâncias calculadas.
4. `StartProtection` coloca ordens de proteção para qualquer posição existente e para negociações futuras abertas pela estratégia.

A estratégia não gera sinais de negociação; apenas gerencia o risco garantindo que cada posição tenha níveis adequados de stop-loss e take-profit.
