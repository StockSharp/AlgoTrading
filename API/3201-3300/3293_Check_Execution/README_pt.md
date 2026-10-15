# Estratégia Check Execution
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Check Execution reproduz o comportamento do expert MQL original que modifica repetidamente uma ordem da corretora para medir a qualidade de execução. O algoritmo pode testar um buy stop pendente ou um sell stop de proteção que guarda uma posição comprada aberta com uma ordem a mercado. Cada modificação registra tanto o spread observado quanto o tempo necessário para o venue aceitar a mudança, facilitando avaliar condições sensíveis à latência oferecidas por uma corretora.

## Lógica central
1. Assinar atualizações de melhor bid/ask por meio da API de alto nível `SubscribeLevel1`.
2. Colocar a ordem inicial de teste dependendo do modo selecionado:
   - **Pendente** - enviar um buy stop acima do preço ask atual.
   - **Mercado** - comprar a mercado e depois enviar um sell stop de proteção abaixo do último ask.
3. Em cada atualização de cotação:
   - Atualizar a média móvel do spread bid/ask usando `SimpleMovingAverage`.
   - Re-registrar a ordem rastreada no novo offset a partir do preço ask quando uma mudança for necessária e uma solicitação anterior não estiver aguardando confirmação.
   - Medir a latência de execução assim que a ordem retorna ao estado `Active` e alimentá-la em um segundo `SimpleMovingAverage` para obter o atraso médio em milissegundos.
4. Repetir o ciclo de modificação até que o número configurado de iterações seja atingido. Depois disso, a estratégia cancela ordens pendentes/stop restantes, fecha a posição comprada aberta se necessário e imprime estatísticas agregadas de spread e latência.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Volume` | Volume de negociação usado para cada ordem. | `0.01` |
| `Iterations` | Número de tentativas de modificação para média. Limitado a 1-500. | `30` |
| `Order Mode` | Seleciona o fluxo: `Pending` ou `Market`. | `Pending` |
| `Pending Offset` | Distância em passos de preço acima do ask para o buy stop de teste. | `100` |
| `Stop Offset` | Distância em passos de preço abaixo do ask para o sell stop de proteção. | `100` |

## Notas de comportamento
- Valores de volume são normalizados às restrições `VolumeStep`, `MinVolume` e `MaxVolume` do ativo para evitar ordens rejeitadas.
- Offsets de preço são traduzidos em preços reais usando o `PriceStep` do instrumento. Um passo padrão de `0.0001` é usado se o ativo não fornecer um.
- A estratégia só conta uma modificação quando o venue confirma a solicitação movendo a ordem para o estado `Active` ou `Done`. Cada confirmação atualiza tanto o temporizador de execução quanto o contador de modificações.
- Quando o número alvo de iterações é alcançado, a estratégia para automaticamente de modificar ordens, cancela proteção pendente, fecha qualquer posição de teste e registra uma mensagem de resumo com as médias medidas.

## Diferenças em relação à versão MQL
- As médias de spread e execução são calculadas com indicadores `SimpleMovingAverage` do StockSharp em vez de arrays manuais.
- A gestão de ordens usa helpers de alto nível como `BuyMarket`, `BuyStop`, `SellStop` e `ReRegisterOrder` para permanecer consistente com o framework de estratégia do StockSharp.
- O feedback da interface é fornecido pelo log da estratégia, não por comentários no gráfico e objetos gráficos.
