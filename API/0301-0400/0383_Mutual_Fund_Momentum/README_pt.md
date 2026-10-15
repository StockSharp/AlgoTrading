# Estratégia de Momentum de Fundos Mútuos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia rotaciona trimestralmente entre um conjunto de fundos mútuos. No final de cada trimestre, os fundos são classificados pelo desempenho dos últimos seis meses. O capital é alocado no fundo líder para o próximo trimestre, permitindo que investidores de longo prazo sigam o momentum persistente em produtos de gestão ativa.

Apenas um fundo é mantido de cada vez. São utilizados dados de preço diários e o rebalanceamento ocorre durante os primeiros três dias de negociação de janeiro, abril, julho e outubro.

## Detalhes

- **Universo**: lista de fundos mútuos.
- **Sinal**: classificação pelo retorno total de 126 dias (seis meses).
- **Rebalanceamento**: trimestral nos primeiros dias de negociação do novo trimestre.
- **Posicionamento**: totalmente comprado no fundo de maior classificação.
- **Controle de risco**: ignorar negociação quando o valor da ordem estiver abaixo de `MinTradeUsd`.
