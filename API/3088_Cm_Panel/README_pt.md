# Estratégia CM Panel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia CM Panel** é um auxiliar manual de ordens pendentes que recria o comportamento do script original do MetaTrader 5 "cm panel". Em vez de desenhar controles na tela, o porte para StockSharp expõe parâmetros interativos que funcionam como botões: definir um sinalizador como `true` coloca ou cancela ordens stop pendentes e o sinalizador imediatamente redefine para `false`, imitando o fluxo de trabalho de botão de pressão do painel. A estratégia mantém configuração separada para ordens de compra e venda, incluindo distâncias, volumes e alvos de proteção expressos em pontos.

A conversão se baseia inteiramente na API de alto nível do StockSharp. Ordens pendentes são enviadas com os helpers `BuyStop` e `SellStop`, enquanto a proteção pós-execução é implementada registrando ordens de stop-loss e take-profit independentes. Os valores de preço e volume são automaticamente adaptados ao tamanho do tick e passo de lote do ativo, para que a estratégia respeite as restrições da bolsa sem exigir normalização manual.

## Lógica de negociação
1. Quando o usuário alterna `PlaceBuyStop` para `true`, a estratégia lê o melhor ask (com fallback para o último preço negociado, se necessário) e o desloca por `BuyStopOffsetPoints` convertidos em unidades de preço. Uma ordem stop de compra com volume `BuyVolume` é enviada ao nível resultante. Os preços desejados de stop-loss e take-profit são calculados imediatamente e armazenados como alvos de proteção pendentes.
2. Quando o usuário alterna `PlaceSellStop` para `true`, o melhor bid (ou último negócio) é deslocado para baixo por `SellStopOffsetPoints`. Uma ordem stop de venda com volume `SellVolume` é colocada nesse preço e os níveis de proteção correspondentes são registrados.
3. Depois que uma ordem stop pendente é executada, a estratégia coloca automaticamente as ordens de proteção registradas:
   - Posições compradas recebem um `SellStop` de stop-loss abaixo do preço de entrada e um `SellLimit` de take-profit acima.
   - Posições vendidas recebem um `BuyStop` de stop-loss acima do preço de entrada e um `BuyLimit` de take-profit abaixo.
   Cada ordem de proteção é enviada apenas uma vez; se uma for executada, a outra é cancelada para emular o par SL/TP único do MetaTrader.
4. Quando o sinalizador `CancelPendingOrders` é alternado, todas as ordens stop de compra ou venda ativas criadas pela estratégia são canceladas. Ordens de proteção que já guardam posições abertas são intencionalmente deixadas intactas para que as negociações em andamento permaneçam protegidas.
5. Os volumes são ajustados ao `VolumeStep`, `MinVolume` e `MaxVolume` do ativo. Se o tamanho resultante se tornar inválido (por exemplo, abaixo do lote mínimo), a operação é abortada e um aviso é registrado em vez de enviar uma ordem.
6. Todas as distâncias de preço são expressas em pontos e convertidas usando o `PriceStep` do ativo. Se o passo for desconhecido, um fallback conservador de `0.0001` é aplicado para que o painel permaneça utilizável em símbolos sem metadados de tick.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `BuyVolume` | `decimal` | `0.10` | Volume enviado com cada ordem stop de compra após respeitar o passo de lote do instrumento. |
| `SellVolume` | `decimal` | `0.10` | Volume enviado com cada ordem stop de venda após respeitar o passo de lote do instrumento. |
| `BuyStopOffsetPoints` | `int` | `100` | Distância em pontos adicionada acima do ask atual para posicionar o stop de compra pendente. |
| `SellStopOffsetPoints` | `int` | `100` | Distância em pontos subtraída do bid atual para posicionar o stop de venda pendente. |
| `BuyStopLossPoints` | `int` | `100` | Distância do stop-loss (em pontos) para posições compradas acionadas pelo stop de compra. Definir como zero para pular a ordem de proteção. |
| `SellStopLossPoints` | `int` | `100` | Distância do stop-loss (em pontos) para posições vendidas acionadas pelo stop de venda. Definir como zero para pular a ordem de proteção. |
| `BuyTakeProfitPoints` | `int` | `150` | Distância do take-profit (em pontos) para posições compradas acionadas pelo stop de compra. Definir como zero para pular a ordem de proteção. |
| `SellTakeProfitPoints` | `int` | `150` | Distância do take-profit (em pontos) para posições vendidas acionadas pelo stop de venda. Definir como zero para pular a ordem de proteção. |
| `PlaceBuyStop` | `bool` | `false` | Sinalizador que coloca uma ordem stop de compra uma vez. O valor redefine para `false` automaticamente após o processamento. |
| `PlaceSellStop` | `bool` | `false` | Sinalizador que coloca uma ordem stop de venda uma vez. O valor redefine para `false` automaticamente após o processamento. |
| `CancelPendingOrders` | `bool` | `false` | Sinalizador que cancela todas as ordens stop pendentes ativas criadas pelo painel. |

## Diferenças em relação à versão do MetaTrader
- O MetaTrader anexa níveis de stop-loss e take-profit diretamente às ordens pendentes. O StockSharp mantém o mesmo comportamento gerando ordens de proteção dedicadas imediatamente após a execução de uma entrada.
- A implementação do StockSharp adapta transparentemente os volumes e preços aos metadados do ativo, eliminando a necessidade de normalização manual com `_Point`, `_Digits` ou arredondamento de volume.
- As limitações de nível de stop da sede de negociação não são consultadas automaticamente. Os usuários devem configurar deslocamentos que respeitem a distância mínima do corretor, assim como fariam no MetaTrader.
- O toggle de exclusão (`CancelPendingOrders`) cancela apenas stops pendentes. Ordens de proteção existentes para posições abertas permanecem ativas para que as negociações ao vivo permaneçam protegidas.

## Dicas de uso
- Atribuir um portfólio e ativo antes de alternar qualquer sinalizador de ação; caso contrário, a estratégia registra um aviso e ignora a solicitação.
- Para emular o fluxo de trabalho do painel original, adicionar a estratégia à interface Designer ou Runner, expor os parâmetros na grade de propriedades e alternar os booleanos quando quiser enviar ou cancelar ordens.
- Como a lógica depende das melhores cotações bid/ask, garantir que os dados Level 1 estejam sendo transmitidos. Se os melhores preços estiverem faltando, o código recorre ao último preço negociado, mas as ordens pendentes podem ficar mais próximas do mercado do que o pretendido.
- Ajustar as distâncias em pontos para respeitar o nível de stop mínimo do instrumento. O helper não aplica automaticamente buffers específicos do corretor.
- Definir as distâncias de proteção como zero quando quiser colocar ordens stop nuas sem níveis de SL/TP acompanhantes.
