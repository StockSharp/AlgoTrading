# Estratégia Nevalyashka
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um sistema simples de alternância comprado/vendido com dimensionamento de posição martingale.

## Lógica da estratégia

1. No início, uma posição vendida é aberta.
2. Um take profit e um stop loss fixos são anexados à posição.
3. Sempre que a posição é fechada (por stop ou alvo):
   - A próxima operação é aberta na direção oposta.
   - Se a operação anterior terminou com perda, o volume da ordem é multiplicado por `LotMultiplier`.
   - Se a operação anterior terminou com lucro, o volume é redefinido para o `Volume` base.
4. Os passos 2‑3 se repetem indefinidamente.

## Parâmetros

- `Volume` – volume de ordem base usado na primeira operação e após operações lucrativas.
- `LotMultiplier` – multiplicador aplicado ao volume após uma operação perdedora.
- `TakeProfit` – distância do objetivo de lucro em pontos de preço.
- `StopLoss` – distância do stop loss em pontos de preço.

## Notas

- As ordens de proteção são gerenciadas por meio de `StartProtection`.
- A estratégia não depende de dados de mercado; ela reage apenas a mudanças de posição.
