# Estratégia SVM Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A Estratégia SVM Trader demonstra como uma combinação de indicadores técnicos clássicos pode aproximar o comportamento de um modelo de máquina de vetores de suporte (SVM) para gerar sinais de trading. O exemplo MQL original treinava dois SVMs separados para decisões de compra e venda. Nesta conversão para StockSharp, emulamos o processo de decisão com um sistema simples de pontuação derivado de sete indicadores:

- **Bears Power** e **Bulls Power** – medem o equilíbrio entre vendedores e compradores.
- **Average True Range (ATR)** – captura a volatilidade atual.
- **Momentum** – verifica a aceleração do preço.
- **Moving Average Convergence Divergence (MACD)** – identifica a direção da tendência.
- **Stochastic Oscillator** – detecta níveis de sobrecompra e sobrevenda.
- **Force Index** – combina movimento de preço e volume.

Cada indicador contribui para uma pontuação cumulativa. Quando a pontuação excede um limiar, a estratégia abre uma posição comprada; quando a pontuação cai abaixo do limiar oposto, uma posição vendida é aberta. Esta configuração espelha o passo de classificação da abordagem SVM original mantendo a implementação leve e transparente.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `CandleType` | Período de candles para cálculos. |
| `Volume` | Volume de ordem para novas operações. |
| `TakeProfit` | Distância para take-profit em unidades absolutas de preço. |
| `StopLoss` | Distância para stop-loss em unidades absolutas de preço. |
| `RiskExposure` | Volume máximo de posição cumulativa permitido. |

## Lógica de Trading

1. Assinar candles do tipo especificado e vincular todos os indicadores usando a API de alto nível.
2. Para cada candle concluído, recuperar valores de indicadores do callback de vinculação.
3. Calcular uma pontuação:
   - Bulls Power maior que Bears Power
   - Momentum acima de zero
   - Linha MACD acima de sua linha de sinal
   - Estocástico %K acima de %D
   - Force Index acima de zero
4. Se pelo menos três condições forem verdadeiras e a posição atual for não positiva, uma ordem de compra a mercado é colocada.
5. Se duas ou menos condições forem verdadeiras e a posição atual for não negativa, uma ordem de venda a mercado é colocada.
6. `StartProtection` aplica tanto stop-loss quanto take-profit para cada posição aberta.

## Notas

- Os períodos dos indicadores são fixos com base nos valores do exemplo MQL original (principalmente 13 para simetria e suavidade).
- O sistema de pontuação é um proxy simplificado para a classificação SVM e pode ser substituído por um modelo mais avançado se necessário.
- `RiskExposure` evita superalocação limitando o tamanho total da posição.
- A estratégia usa tabulações para indentação e comentários em inglês conforme as convenções do projeto.

## Aviso legal

Esta estratégia é fornecida para fins educacionais. Ela demonstra vinculação de indicadores e gerenciamento básico de risco no StockSharp. Use e modifique por sua conta e risco.
