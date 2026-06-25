# Estratégia de Swing com MA Serial (API/2782)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Converte o expert advisor SerialMA do MetaTrader em uma estratégia de alto nível do StockSharp usando assinaturas de velas e um indicador de média móvel serial personalizado.
- Abre novas posições de swing sempre que a média móvel serial inverte sua direção relativa ao preço, opcionalmente revertendo o sinal e limitando o número de swings simultâneos.
- Implementa as mesmas distâncias protetoras de stop-loss e take-profit medidas em pontos do instrumento, recalculadas em cada vela concluída.

## Indicador de Média Móvel Serial
O EA original depende do indicador personalizado *SerialMA* que reconstrói sua média móvel após cada cruzamento de preço. O indicador portado replica esse comportamento:
1. Acumulando preços de fechamento desde o cruzamento mais recente e calculando sua média aritmética.
2. Rastreando a diferença entre a média e o fechamento atual para detectar uma mudança de sinal.
3. Redefinindo a janela interna quando o sinal muda, reiniciando efetivamente a média a partir da barra de cruzamento e sinalizando o evento para a estratégia.

Esta implementação expõe o valor da média móvel junto com um sinalizador booleano indicando que um cruzamento ocorreu na barra anterior, permitindo que a estratégia espelhe a lógica MQL sem acesso manual ao buffer.

## Lógica de trading
1. Em cada vela concluída a estratégia lê o valor da média móvel serial e o sinalizador de cruzamento.
2. Quando a vela anterior desencadeou um cruzamento:
   - Se o fechamento anterior estava acima da média móvel anterior, um sinal comprado é gerado.
   - Se o fechamento anterior estava abaixo da média móvel anterior, um sinal vendido é gerado.
3. O parâmetro **ReverseSignals** opcionalmente troca entradas compradas e vendidas.
4. O parâmetro **OpenedMode** controla o empilhamento de posições:
   - **AllSwing** abre uma nova ordem em cada sinal, mesmo que já exista uma posição nessa direção.
   - **SingleSwing** só abre uma nova ordem quando não existe exposição nessa direção.
5. Antes de enviar uma nova ordem, a estratégia sempre fecha a exposição existente na direção oposta para manter a lógica de swing consistente com o EA fonte.
6. As distâncias de stop-loss e take-profit são aplicadas em cada vela usando o passo de preço do instrumento, correspondendo aos controles de risco baseados em pontos do expert original.

## Parâmetros
| Nome | Descrição | Valor padrão |
| --- | --- | --- |
| `OpenedMode` | Permite empilhar swings ou manter um único swing por direção. | `AllSwing` |
| `EnableBuy` | Habilita ou desabilita entradas compradas. | `true` |
| `EnableSell` | Habilita ou desabilita entradas vendidas. | `true` |
| `ReverseSignals` | Inverte a direção de trading. | `false` |
| `TradeVolume` | Tamanho da ordem (lotes) para cada novo swing. | `1` |
| `StopLossPoints` | Distância de stop-loss em passos de preço (pontos). Um valor de `0` desabilita o stop. | `0` |
| `TakeProfitPoints` | Distância de take-profit em passos de preço (pontos). Um valor de `0` desabilita o take profit. | `0` |
| `CandleType` | Tipo de dados de vela usado para cálculos. | `Velas de 5 minutos` |

## Gestão de ordens e proteção
- Quando comprado, a estratégia verifica se a mínima da vela violou o nível de stop-loss ou se a máxima da vela atingiu o alvo de lucro e emite uma ordem de mercado para nivelar de acordo.
- Quando vendido, a máxima da vela dispara o stop-loss e a mínima da vela dispara o alvo de lucro.
- Os níveis de proteção são medidos em unidades de `PriceStep`. Se o instrumento não fornecer um passo de preço, as verificações de proteção permanecem inativas, refletindo o comportamento de informações de tamanho de tick ausentes.

## Notas de uso
- A implementação usa a API de alto nível do StockSharp (`SubscribeCandles` + `BindEx`) e evita o gerenciamento de buffer de baixo nível.
- Nenhuma versão em Python está incluída, conforme solicitado. Apenas o port em C# reside em `CS/SerialMASwingStrategy.cs`.
- A estratégia é destinada à execução no estilo swing semelhante ao EA original; habilitar ambas as direções e manter o modo padrão `AllSwing` mais se assemelha ao comportamento MQL.
