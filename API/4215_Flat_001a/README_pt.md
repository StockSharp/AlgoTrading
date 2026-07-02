# Estratégia de alcance Flat 001a
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Flat 001a é um sistema de scalping projetado para o gráfico horário EURUSD. Ele verifica as velas de três horas mais recentes e mede a distância entre a máxima mais alta e a mínima mais baixa. Quando o intervalo desta janela de três velas permanece dentro de um número configurável de pontos, a estratégia prevê que o preço permanecerá preso dentro da linha plana. Em seguida, procura atenuar excursões de curto prazo no quarto superior ou inferior do canal e imediatamente atribui ordens de proteção.

O consultor especialista original MQL4 negociou apenas EURUSD no primeiro semestre e rejeitou a negociação se o símbolo ou período de tempo estivesse incorreto. Esta porta mantém os mesmos padrões (EURUSD, velas de 60 minutos) e reproduz todos os cálculos de entrada, stop-loss, take-profit e trailing-stop em StockSharp.

## Indicadores e dados
- Os indicadores `Highest` e `Lowest` (período = 3) rastreiam a parte superior e inferior das últimas três velas concluídas.
- Um parâmetro de período de tempo tem como padrão velas de 60 minutos para espelhar o requisito H1 do código-fonte.
- Não são utilizados osciladores adicionais ou filtros de suavização, pelo que a estratégia reage apenas aos extremos do preço bruto.

## Lógica de entrada
1. Aguarde o fechamento da vela de assinatura. Apenas velas prontas são processadas.
2. Verifique se o código de segurança atual corresponde ao código configurado (padrão: `EURUSD`). Caso contrário, a estratégia permanece ociosa.
3. Avalie a janela de negociação opcional. Por padrão, as entradas são permitidas durante as duas horas a partir da meia-noite do horário da plataforma (horas 0 e 1). O filtro pode ser desativado.
4. Calcule o intervalo de três velas `range = highest - lowest` e converta-o em pontos por meio do instrumento `PriceStep`.
5. Continue apenas se o número de pontos estiver entre `DiffMinPoints` e `DiffMaxPoints`.
6. Se o preço de fechamento estiver dentro do quarto mais baixo do intervalo e nenhuma posição estiver aberta, entre em uma negociação longa.
7. Se o preço de fechamento estiver dentro do quarto mais alto do intervalo e nenhuma posição estiver aberta, entre em uma negociação curta.

## Gerenciamento de pedidos
- **Stop-loss inicial**
  - Negociações longas: `lowest - range / 3`.
  - Negociações curtas: `highest + range / 3`.
- **Realização de lucro**
  - Negociações longas: preço de entrada + `TakeProfitPoints * PriceStep`.
  - Negociações curtas: preço de entrada − `TakeProfitPoints * PriceStep`.
- **Parada final**
  - Assim que o lucro não realizado exceder `TrailingStopPoints * PriceStep`, o stop-loss é seguido vela por vela.
  - As negociações longas movem o stop para `closePrice - TrailingDistance` se for maior que o stop atual.
  - As negociações curtas movem o stop para `closePrice + TrailingDistance` se for inferior ao stop atual.
- Todas as saídas são executadas com ordens de mercado. A estratégia fecha a posição completa quando o nível de stop-loss ou de take-profit é tocado pela vela subsequente.

## Parâmetros
| Grupo | Nome | Descrição | Padrão |
| --- | --- | --- | --- |
| Geral | `CandleType` | Tipo de vela usado para cálculos. Deve ser definido para um período de 60 minutos para corresponder ao sistema original. | `TimeFrame(60m)` |
| Geral | `SecurityCode` | Código de segurança esperado. Deixe em branco para negociar qualquer instrumento. | `EURUSD` |
| Filtro de intervalo | `DiffMinPoints` | Faixa mínima de três velas em pontos necessários para negociar. | `18` |
| Filtro de intervalo | `DiffMaxPoints` | Faixa máxima de três velas em pontos permitidos para negociação. | `28` |
| Janela de negociação | `EnableTimeFilter` | Ativa ou desativa o filtro de hora. | `true` |
| Janela de negociação | `OpenHour` | Hora de início (0–23) da janela de negociação. A estratégia também permite a próxima hora imediata. | `0` |
| Gestão de Risco | `TakeProfitPoints` | Distância de lucro expressa em pontos. Defina como zero para desativar. | `8` |
| Gestão de Risco | `TrailingStopPoints` | Distância do trailing-stop expressa em pontos. Defina como zero para desativar o rastreamento. | `6` |

## Notas práticas
- A propriedade StockSharp `Strategy.Volume` controla o tamanho do pedido. Ajuste-o de acordo com o tamanho do seu contrato de corretor.
- Certifique-se de que o instrumento selecionado exponha um `PriceStep` válido. Se `PriceStep` estiver faltando, a estratégia volta para `1` e registra um aviso.
- O consultor especialista MQL4 ofereceu gerenciamento de dinheiro opcional, escalonando os lotes de acordo com o saldo da conta. A amostra de StockSharp mantém o tamanho da posição constante; você pode criar scripts para seu próprio gerenciamento de volume, se necessário.
- Sempre teste a estratégia em simulação antes de executá-la ao vivo. A lógica final pressupõe que o corretor atenderá às ordens de proteção quando os extremos das velas cruzarem o nível; em mercados rápidos, a derrapagem pode aumentar o risco realizado.
