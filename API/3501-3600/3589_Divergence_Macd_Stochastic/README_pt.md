# Estratégia de Divergência MACD Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o consultor especialista MetaTrader 5 **"Divergência EA pip sl tp"** na estrutura StockSharp. O algoritmo procura divergências clássicas entre a ação do preço e o histograma MACD e, em seguida, valida o sinal com um filtro oscilador Stochastic de sobrecompra/sobrevenda antes de abrir negociações de reversão.

## Lógica de negociação

1. Assine as velas do período principal selecionadas pelo parâmetro `CandleType`.
2. Calcule o histograma MACD (`MACD line - Signal line`) e os valores Stochastic %K/%D em cada vela concluída.
3. Acompanhe os dois últimos máximos e mínimos dos valores de preço e histograma.
4. **Divergência de baixa**: uma nova máxima de preço mais alta acompanhada por um pico de histograma MACD mais baixo e Stochastic %K acima de `StochasticUpperLevel` aciona uma posição curta ou reverte uma posição comprada existente.
5. **Divergência de alta**: uma nova mínima de preço mais baixa com um fundo de histograma MACD mais alto e %K abaixo de `StochasticLowerLevel` abre ou reverte para uma posição longa.
6. As proteções opcionais `TakeProfitSteps` e `StopLossSteps` são convertidas em unidades de passo StockSharp e ativadas uma vez quando a estratégia é iniciada.

## Notas de implementação

- Construído com StockSharp API de alto nível usando uma única assinatura de vela vinculada aos indicadores `MovingAverageConvergenceDivergenceSignal` e `StochasticOscillator`.
- Mantém o estado de divergência sem chamar ajudantes do indicador `GetValue`, obedecendo às diretrizes de conversão.
- A integração do gráfico exibe velas de preço, MACD e Stochastic linhas quando uma área do gráfico está disponível.
- As posições são invertidas adicionando o tamanho absoluto da posição atual à base `Volume`, garantindo mudanças de direção imediatas após divergências confirmadas.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Prazo usado para cálculos de divergência. | Velas de 1 hora |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD EMA comprimentos replicando as entradas originais EA. | 26/12/9 |
| `MacdDivergenceThreshold` | Diferença mínima do histograma entre oscilações consecutivas necessária para confirmar a divergência. | 0,0005 |
| `StochasticLength` | Período %K rápido do oscilador Stochastic. | 50 |
| `StochasticSlowK`, `StochasticSlowD` | Comprimentos de suavização %K/%D adicionais espelhando a configuração EA. | 9/9 |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Filtros de sobrecompra e sobrevenda validando configurações de baixa/alta. | 80/20 |
| `TakeProfitSteps`, `StopLossSteps` | Distâncias de proteção opcionais expressas em etapas de preço (0 desativa o nível). | 50 |

## Uso

1. Anexe a estratégia a um conector StockSharp com uma segurança que suporte o período de tempo selecionado.
2. Configure o tamanho da posição através da propriedade base `Volume` e ajuste as configurações do indicador conforme desejado.
3. Inicie a estratégia - os pedidos serão gerados automaticamente sempre que a divergência e as condições Stochastic forem satisfeitas.
