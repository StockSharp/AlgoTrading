# Estratégia Gazonkos de Retração
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia Gazonkos de Retração é uma conversão do consultor especialista original **gazonkos** do MetaTrader 5. A abordagem opera o gráfico horário do EUR/USD e procura um forte momentum entre dois fechamentos históricos. Depois de detectar esse momentum, aguarda uma retração de um tamanho predefinido e então entra na direção do movimento inicial. A implementação do StockSharp mantém a mesma máquina de estados por etapas do código-fonte enquanto usa a API de alto nível com assinaturas de velas e ordens protetoras.

## Lógica de trading
1. **Verificação de elegibilidade** – apenas uma posição por hora é permitida. Se outro trade foi aberto durante a mesma hora do relógio, ou se o número configurado de trades simultâneos já está em execução, a estratégia aguarda.
2. **Detecção de momentum** – compara os preços de fechamento de duas velas passadas (`SecondShift` menos `FirstShift`). Se a diferença exceder `Delta`, a estratégia registra a direção pretendida (comprado se o fechamento mais novo for mais alto, vendido caso contrário).
3. **Rastreamento de retração** – a partir do momento em que o momentum aparece, o código monitora a máxima mais alta (para configurações compradas) ou a mínima mais baixa (para configurações vendidas) atingida durante aquela hora. Quando o preço retrai pelo menos `Rollback`, a configuração torna-se elegível para execução. Se a hora mudar antes que a retração ocorra, o sinal é descartado.
4. **Execução de ordem** – uma vez que a condição de retração é atendida, a estratégia coloca uma ordem de mercado com distâncias fixas de take profit e stop loss. O dimensionamento de posição é controlado através do parâmetro `TradeVolume`, e o helper integrado `StartProtection` gerencia as ordens protetoras.

Esta sequência reflete de perto a versão MT5 que usou as variáveis `STATE` e `Trade` para coordenar o fluxo de trabalho.

## Gerenciamento de risco
* `StartProtection` configura distâncias fixas de take profit e stop loss em unidades de preço absolutas, similar a como o especialista anexava TP/SL a cada ordem.
* `ActiveTrades` limita a exposição total máxima comparando o valor absoluto da posição com o produto do volume configurado e a contagem de trades permitidos.
* A combinação de controle horário e confirmação de retração reduz o excesso de negociações durante condições laterais.

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------ | --------- |
| `TakeProfit` | `0.0016` | Distância absoluta (em unidades de preço) para o take profit. Corresponde a 16 pontos em uma cotação EUR/USD de 5 dígitos. |
| `Rollback` | `0.0016` | Retração necessária a partir do extremo atingido após o sinal de momentum. |
| `StopLoss` | `0.0040` | Distância absoluta para o stop loss protetor. Equivalente a 40 pontos no EUR/USD. |
| `Delta` | `0.0040` | Diferença mínima entre os dois fechamentos históricos que define um movimento forte. |
| `TradeVolume` | `0.1` | Volume de ordem padrão passado para `BuyMarket()` e `SellMarket()`. |
| `FirstShift` | `3` | Índice de barra mais antiga (número de velas para trás) usado para comparação do preço de fechamento. |
| `SecondShift` | `2` | Índice de barra mais nova usado na comparação do preço de fechamento. |
| `ActiveTrades` | `1` | Número máximo de trades simultâneos. Definir como zero para desativar o limite. |
| `CandleType` | Período de `1 hora` | Série de velas usada para análise; padrão de velas horárias como o EA fonte. |

## Notas
* A estratégia funciona com qualquer instrumento que tenha um tamanho de tick razoável; ajustar `Delta`, `Rollback`, `TakeProfit` e `StopLoss` para corresponder ao valor do ponto do instrumento.
* Todos os comentários inline estão escritos em inglês conforme exigido pelas diretrizes do projeto.
* Um port Python para esta estratégia ainda não está disponível.
