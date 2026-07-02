# Estratégia manual EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia EA Manual** é uma conversão StockSharp de alto nível API um para um do MetaTrader 4 consultor especialista *Manual_EA.mq4* (pasta `MQL/8159`). O sistema original emite ordens discricionárias de compra ou venda sempre que o oscilador Stochastic sai das zonas extremas. A porta StockSharp mantém a mesma configuração do oscilador 5-3-3, compensa automaticamente a exposição existente antes de colocar a próxima ordem de mercado e expõe as opções comuns de gerenciamento de dinheiro por meio de parâmetros de estratégia.

## Lógica de negociação

1. A estratégia assina a série `CandleType` (padrão: velas de 15 minutos) e alimenta os preços de fechamento em um oscilador Stochastic configurado com:
   - `%K` lookback = `KPeriod` (padrão 5 barras)
   - `%K` desaceleração = `Slowing` (padrão 3 barras)
   - `%D` suavização = `DPeriod` (padrão 3 barras)
2. Os sinais são avaliados no valor final da linha %D (sinal) de cada vela finalizada. Duas leituras consecutivas são comparadas para detectar passagens de nível.
3. **Entrada longa** – Quando o valor %D anterior estava abaixo ou igual a `OversoldLevel` (padrão 10) e o valor mais recente ultrapassa esse limite. A estratégia primeiro neutraliza qualquer exposição curta e depois compra `Volume + |short position|` por ordem de mercado.
4. **Entrada curta** – Quando o valor %D anterior estava acima ou igual a `OverboughtLevel` (padrão 90) e o valor mais recente fica abaixo desse limite. Qualquer posição longa existente é fechada antes de vender `Volume + |long position|` no mercado.
5. As ordens de proteção são tratadas via `StartProtection`. Um `StopLoss` e/ou `TakeProfit` positivo (medido em faixas de preço) ativa o gerenciamento automático de risco. Definir um parâmetro como `0` desativa a proteção correspondente.

A porta evita deliberadamente padrões de acesso ao buffer do indicador e lógica de vela inacabada, em conformidade com StockSharp práticas recomendadas de alto nível API.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Período de tempo (como `DataType`) usado para construir velas e acionar o oscilador. | Período de 15 minutos |
| `KPeriod` | Comprimento de lookback da linha Stochastic %K. | 5 |
| `DPeriod` | Suavização do comprimento da linha de sinal Stochastic %D. | 3 |
| `Slowing` | Suavização adicional aplicada a %K antes de %D ser calculado. | 3 |
| `OverboughtLevel` | Limite superior que aciona entradas curtas quando cruzado para baixo por %D. | 90 |
| `OversoldLevel` | Limite inferior que aciona entradas longas quando cruzado para cima por %D. | 10 |
| `StopLoss` | Distância em pontos de preço para o stop loss de proteção (0 = desabilitado). | 100 |
| `TakeProfit` | Distância em faixas de preço para a meta de lucro (0 = desativado). | 100 |
| `Volume` | Tamanho do pedido enviado com cada novo sinal (lotes). As posições opostas existentes são compensadas primeiro. | 0,1 |

## Notas adicionais

- A estratégia usa `SubscribeCandles` junto com `BindEx` para transmitir atualizações de `StochasticOscillatorValue`, garantindo que os valores dos indicadores sejam definitivos antes que as decisões de negociação sejam tomadas.
- A visualização do gráfico traça automaticamente a série de velas selecionada, o oscilador Stochastic e as próprias negociações quando uma área do gráfico está disponível.
- Como os cruzamentos %D são avaliados em velas concluídas consecutivas, o comportamento corresponde à implementação MQL que comparou os valores `MODE_SIGNAL` nos turnos 1 e 2.
