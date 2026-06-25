# Estratégia Exp T3 TRIX (ID 2946)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Exp T3 TRIX replica o consultor especialista do MetaTrader 5 construído em torno do oscilador TRIX de tripla suavização. Ela aplica suavização Tillson T3 para gerar um fluxo TRIX rápido e lento e reage a reversões de momentum usando três modos selecionáveis. Cada modo controla como o histograma ou a posição relativa dos componentes rápido e lento devem se comportar antes que a estratégia entre ou saia de uma posição.

## Lógica de trading

- **Cálculo Tillson T3 TRIX**
  - Duas pilhas de seis médias móviles exponenciais com o mesmo comprimento produzem valores Tillson T3 para um fluxo rápido e um lento.
  - A derivada de cada valor T3 (atual menos anterior dividido por anterior) se torna o histograma TRIX usado para tomada de decisão.
- **Modo = Breakdown**
  - *Entrada comprada*: TRIX rápida cruza de abaixo de zero para acima de zero enquanto entradas compradas estão habilitadas. Qualquer posição vendida aberta é fechada primeiro (se saídas vendidas forem permitidas).
  - *Entrada vendida*: TRIX rápida cruza de acima de zero para abaixo de zero enquanto entradas vendidas estão habilitadas. Qualquer posição comprada aberta é fechada primeiro (se saídas compradas forem permitidas).
  - *Apenas saída*: Quando um cruzamento ocorre mas a entrada correspondente está desabilitada, a estratégia ainda fecha a exposição oposta se a permissão de saída relevante estiver habilitada.
- **Modo = Twist**
  - *Entrada comprada*: A inclinação da TRIX rápida muda de negativa para positiva (ou seja, a barra atual está subindo depois de cair). A estratégia replica as regras de fechamento e permissão do modo Breakdown.
  - *Entrada vendida*: A inclinação da TRIX rápida muda de positiva para negativa.
- **Modo = CloudTwist**
  - *Entrada comprada*: A TRIX rápida se move acima da TRIX lenta depois de estar abaixo dela na barra completa anterior.
  - *Entrada vendida*: A TRIX rápida cai abaixo da TRIX lenta depois de estar acima dela na barra anterior.
- **Tratamento de ordens**
  - A estratégia primeiro fecha a exposição oposta quando um sinal de reversão aparece e saídas são permitidas.
  - Novas ordens usam `Volume + |Position|` para que uma reversão possa ser executada em uma única operação quando permitido.
  - `StartProtection()` é ativado para reutilizar a camada de segurança integrada do StockSharp do template do projeto original.

## Parâmetros

| Parâmetro | Valor padrão | Descrição |
|-----------|--------------|-----------|
| `Fast Length` | 10 | Profundidade usada para a pilha Tillson T3 rápida (seis EMAs encadeadas). |
| `Slow Length` | 18 | Profundidade usada para a pilha Tillson T3 lenta. |
| `Volume Factor` | 0.7 | Coeficiente de suavização Tillson T3 (0 a 1). |
| `Mode` | Twist | Escolhe entre detecção de sinais Breakdown, Twist ou CloudTwist. |
| `Allow Long Entry` | true | Habilita a abertura de posições compradas. |
| `Allow Short Entry` | true | Habilita a abertura de posições vendidas. |
| `Allow Long Exit` | true | Habilita o fechamento de posições compradas. |
| `Allow Short Exit` | true | Habilita o fechamento de posições vendidas. |
| `Candle Type` | Período de 4 horas | Intervalo de agregação usado para solicitar candles e alimentar a cadeia de indicadores. |

Todos os parâmetros são expostos através de `StrategyParam<T>`, tornando-os visíveis na UI do Designer e prontos para otimização.

## Notas de uso

1. A lógica funciona apenas com candles finalizados. Garanta que a fonte de dados entregue o período configurado em `Candle Type`.
2. Como a derivada TRIX requer valores históricos, os dois primeiros candles completos são usados para inicialização e não produzem sinais.
3. Para replicar o comportamento do MetaTrader, desabilite o flag `Allow ...` correspondente se desejar trading unidirecional ou supressão de saídas.
4. Gerenciamento de risco como níveis de stop-loss ou take-profit não foi incluído no consultor especialista original e portanto não é implementado aqui. Combine a estratégia com os módulos de gerenciamento de dinheiro do StockSharp se necessário.

## Detalhes de conversão

- Fonte: `MQL/2156/exp_t3_trix.mq5` mais o indicador `t3_trix.mq5`.
- O port de API implementa os mesmos três modos de sinal usando assinaturas de candles de alto nível do StockSharp e classes de indicadores.
- A suavização Tillson T3 é recriada usando seis médias móveis exponenciais encadeadas e o fator de volume canônico de 0.7, ajustável através de `Volume Factor`.
