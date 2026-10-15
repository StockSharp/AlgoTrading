# Estratégia BreadandButter2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Breadandbutter2 é uma conversão direta do consultor especialista MT4 de `MQL/7710/Breadandbutter2.mq4`. O sistema monitora velas de uma hora e rastreia três médias móveis lineares ponderadas (LWMA) construídas sobre os preços de abertura das velas. Um cruzamento sincronizado das três médias indica uma reversão de tendência. A estratégia imediatamente inverte a posição para se alinhar com a nova direção e, opcionalmente, coloca pedidos adicionais em pirâmide enquanto a tendência persiste.

## Lógica principal
1. Assine velas de uma hora (configuráveis via **Tipo de vela**).
2. Calcule LWMA(5), LWMA(10) e LWMA(15) na abertura da vela.
3. Detecte uma reversão de alta quando a vela anterior tinha `LWMA5 < LWMA10 < LWMA15` e a vela atual mostra `LWMA5 > LWMA10 > LWMA15`. Detecte uma reversão de baixa com a sequência de desigualdade oposta.
4. Em um cruzamento de alta, vise uma posição longa de lotes de **Volume**. Em um cruzamento de baixa, vise uma posição curta de tamanho igual. A estratégia ajusta a posição existente comprando ou vendendo apenas a diferença entre a exposição atual e a exposição alvo.
5. Após cada entrada, o contador de **Intervalo** é reiniciado. Depois que as velas finalizadas do **Intervalo** passam sem um novo cruzamento, a estratégia adiciona outra ordem na direção atual (pirâmide) e atualiza as ordens de proteção.
6. A meta de lucro e o limite de perda são anexados a cada posição resultante usando distâncias **Take Profit** e **Stop Loss** expressas em etapas de preço. Definir qualquer um dos valores como zero desativa a proteção correspondente.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| **Volume** | 0,1 | Tamanho do pedido em lotes para cada entrada base e camada da pirâmide. |
| **Receba lucro** | 20 | Distância nas etapas de preço para a ordem de realização de lucro. Defina como 0 para desativar. |
| **Stop Loss** | 20 | Distância em etapas de preço para o stop protetor. Defina como 0 para desativar. |
| **Intervalo** | 4 | Número de velas finalizadas que devem ser aguardadas antes de adicionar outra posição na pirâmide. Zero desativa a pirâmide. |
| **Filtro Cruzado** | 1.1 | Parâmetro reservado mantido no código original para filtragem ADX futura (atualmente não usado). |
| **Tipo de vela** | Período de 1 hora | Fonte de dados Candle para os cálculos LWMA. |

## Gerenciamento de posição
- O método auxiliar `AdjustPosition` garante que a posição final corresponda exatamente à exposição desejada após cada cruzamento.
- As negociações em pirâmide dependem do sinal atual de `Position` para adicionar lotes apenas na direção existente.
- `SetTakeProfit` e `SetStopLoss` são invocados após cada negociação para manter os controles de risco sincronizados com o tamanho da posição mais recente.

## Notas
- O script MT4 calculou um valor ADX mas nunca o utilizou; o parâmetro **Cross Filter** é mantido para compatibilidade e extensão futura.
- A implementação original do MQL teve o contador de intervalo comentado. A versão StockSharp ativa o comportamento de pirâmide pretendido contando velas concluídas.
- `StartProtection()` é chamado durante `OnStarted` para ativar serviços integrados de proteção de posição.
