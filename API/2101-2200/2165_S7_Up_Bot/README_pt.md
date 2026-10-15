# Estratégia S7 Up Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de rompimento que procura máximos ou mínimos quase iguais seguidos de um movimento brusco de preço.
Quando dois mínimos consecutivos são quase iguais e o preço sobe `Span Price`, o bot entra comprado.
Entra vendido quando dois máximos se alinham e o preço cai `Span Price`.
As posições são protegidas com funções opcionais de take-profit, stop-loss, trailing stop e saída antecipada.

## Detalhes

- **Critérios de entrada:**
  - **Compra:** A diferença entre o mínimo atual e o anterior é menor que `HL Divergence` e o preço está `Span Price` acima do mínimo.
  - **Venda:** A diferença entre o máximo atual e o anterior é menor que `HL Divergence` e o preço está `Span Price` abaixo do máximo.
- **Comprado/Vendido:** Ambos.
- **Critérios de saída:**
  - Take-profit ou stop-loss.
  - Trailing stop ou ajuste de trailing a zero.
  - Saída antecipada se o preço cruzar o máximo/mínimo anterior (`Exit At Extremum`) ou se aproximar do nível de reversão (`Exit At Reversal`).
- **Stops:** Take-profit e stop-loss absolutos com trailing opcional.
- **Filtros:** Nenhum.

## Parâmetros

- `Take Profit` – meta de lucro em unidades de preço.
- `Stop Loss` – limite de perda em unidades de preço, 0 para stop automático baseado em extremos.
- `HL Divergence` – diferença máxima permitida entre dois máximos ou mínimos consecutivos.
- `Span Price` – distância do extremo ao preço necessária para a entrada.
- `Max Trades` – número máximo de operações simultâneas.
- `Use Trailing Stop` – habilitar o mecanismo de trailing stop.
- `Trail Stop` – distância do trailing stop.
- `Zero Trailing` – mover o stop em direção ao preço quando a posição for lucrativa.
- `Step Trailing` – passo mínimo para ajustar o trailing a zero.
- `Exit At Extremum` – fechar se o preço cruzar o máximo/mínimo anterior.
- `Exit At Reversal` – fechar se o preço se aproximar do extremo oposto.
- `Span To Revers` – distância do extremo para acionar a saída por reversão.
- `Candle Type` – período de tempo utilizado para análise.
- `Order Volume` – quantidade por operação.
