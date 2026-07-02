# Estratégia de previsão de mercado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Market Predictor é uma adaptação de alto nível do consultor especialista MarketPredictor original MetaTrader. A lógica se concentra em reestimar continuamente o movimento de preços esperado, combinando uma previsão de Monte Carlo com parâmetros estatísticos adaptativos coletados de velas recentes. A estratégia assina velas do período selecionado e processa apenas barras concluídas para evitar sinais prematuros.

## Conceitos Básicos
- **Estimativa média adaptativa:** A estratégia mantém um preço médio dinâmico (`mu`) atualizado a partir de uma média móvel simples. Isso reflete a etapa de otimização de parâmetros do consultor especialista original.
- **Amplitude orientada pela volatilidade:** O ATR da mesma série de velas controla o coeficiente de amplitude (`alpha`), mantendo a previsão responsiva aos picos de volatilidade.
- **Projeção de Monte Carlo:** Para cada vela concluída, a estratégia executa um número configurável de simulações aleatórias para estimar o preço esperado (`P_t1`). A previsão é igual à média dos preços simulados.
- **Decisão direcional:** As ordens de mercado são enviadas quando a previsão se desvia do último fechamento em mais do que o limite de `sigma`. A direção da posição é invertida somente depois que a exposição anterior estiver totalmente fechada.

## Regras de negociação
1. Espere a vela terminar e confirme se todos os indicadores estão formados.
2. Atualize `mu` com o valor SMA e `alpha` com a amplitude baseada em ATR.
3. Realize simulações de Monte Carlo em torno do último preço de fechamento.
4. Se o preço médio simulado estiver acima de `Close + sigma`, insira uma posição longa com uma ordem de mercado quando nenhuma posição estiver aberta.
5. Se o preço médio simulado estiver abaixo de `Close - sigma`, insira uma posição curta com uma ordem de mercado quando nenhuma posição estiver aberta.
6. Mantenha a posição até que o sinal oposto seja produzido.

## Parâmetros
- **InitialAlpha** – Amplitude padrão usada antes de ATR ficar disponível.
- **InitialBeta** – Coeficiente de placeholder mantido para compatibilidade com o Expert Advisor original (não utilizado diretamente nos cálculos).
- **InitialGamma** – Constante de amortecimento do espaço reservado preservada para consistência da documentação (não usada diretamente).
- **Kappa** – Parâmetro de sensibilidade para o conceito de componente sigmóide subjacente. Ele é armazenado para referência e futuras extensões.
- **InitialMu** – Preço médio padrão até a formação da média móvel.
- **Sigma** – Desvio necessário entre o preço previsto e o último fechamento para acionar entradas no mercado.
- **MonteCarloSimulations** – Número de simulações utilizadas para estimar o próximo preço.
- **CandleType** – Período da série de velas.

## Notas
- O StockSharp API de alto nível lida com assinaturas de velas, vinculação de indicadores e execução de ordens de mercado.
- Os comentários no código-fonte explicam cada etapa do processo para facilitar a manutenção.
- A porta Python é omitida intencionalmente conforme solicitado.
