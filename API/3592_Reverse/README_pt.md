# Estratégia reversa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Estratégia Reversa é um sistema de negociação de reversão à média que combina Bollinger Bandas e o Índice de Força Relativa (RSI) para identificar movimentos exaustos. A estratégia procura reversões de preços perto dos envelopes Bollinger e, ao mesmo tempo, exige que o RSI volte de uma zona de sobrevenda ou sobrecompra. Uma vez satisfeitas ambas as condições, a estratégia entra contra o movimento anterior e gerencia as negociações com paradas e metas fixas baseadas em banda.

## Lógica de negociação

1. Assine a série de velas configurada (velas padrão de 5 minutos).
2. Calcule Bollinger bandas usando uma média móvel simples com o período configurado e o multiplicador de desvio.
3. Calcule RSI usando o período de lookback configurado.
4. Acompanhe a vela finalizada anterior para detectar cruzamentos:
   - **Configuração longa**: o fechamento anterior está abaixo da banda inferior anterior e RSI está abaixo do limite de sobrevenda. O fechamento atual deve voltar acima da banda inferior enquanto RSI sobe acima do nível de sobrevenda.
   - **Configuração curta**: o fechamento anterior está acima da banda superior anterior e RSI está acima do limite de sobrecompra. O fechamento atual deve cair abaixo da banda superior enquanto RSI cai abaixo do nível de sobrecompra.
5. Quando uma configuração longa for acionada, compre no mercado, defina um stop de proteção um desvio padrão abaixo do fechamento de entrada e um take-profit dois desvios padrão acima dele.
6. Quando uma configuração curta for acionada, venda no mercado, defina um stop de proteção um desvio padrão acima do fechamento de entrada e um take-profit dois desvios padrão abaixo dele.
7. Gerenciar vagas abertas:
   - Feche negociações longas se o preço tocar a banda superior, atingir o stop ou atingir a meta de lucro.
   - Feche negociações curtas se o preço tocar a banda inferior, atingir o stop ou atingir a meta de lucro.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Prazo para assinatura da vela. | Período de 5 minutos |
| `BollingerPeriod` | Número de barras usadas para a média móvel e desvio padrão Bollinger. | 20 |
| `BollingerWidth` | Multiplicador de desvio padrão aplicado a Bollinger bandas. | 2,0 |
| `RsiPeriod` | Número de barras usadas para calcular o RSI. | 14 |
| `RsiOverbought` | RSI limite sinalizando condições de sobrecompra para entradas curtas. | 70 |
| `RsiOversold` | Limite de RSI sinalizando condições de sobrevenda para entradas longas. | 30 |

Todos os parâmetros suportam otimização por meio do StockSharp Designer ou Runner. Ajustar os níveis de sobrevenda/sobrecompra altera o quão agressiva é a detecção de reversão, enquanto a largura Bollinger controla até que ponto o preço deve se estender antes que os sinais sejam considerados.

## Notas de uso

- A estratégia usa StockSharp API de alto nível com assinaturas automáticas de velas e vinculação de indicadores.
- Todas as operações de negociação dependem de ordens de mercado (`BuyMarket`/`SellMarket`). Os níveis de stop-loss e take-profit são tratados em código e não como ordens pendentes.
- A configuração padrão visa grandes reversões em gráficos intradiários, mas pode ser adaptada a intervalos de tempo mais longos alterando `CandleType`.
- Considere combinar a estratégia com filtros adicionais (tendência, volatilidade, tempo de sessão) ao executar em ambientes ativos.
