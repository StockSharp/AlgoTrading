# Estratégia do Painel de Negociação (ID 3468)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O **TradingPanelStrategy** é um auxiliar de entrada manual de pedidos convertido do consultor especialista MQL5 *EA_TradingPanel*. Ele expõe métodos programáticos que replicam o painel original no gráfico: uma única ação pode enviar várias ordens de mercado, anexar automaticamente distâncias de stop-loss e take-profit medidas em pips e, opcionalmente, selecionar um título personalizado para negociar. Os padrões refletem a fonte EA (uma negociação, stop de 2 pip, take de 10 pip, volume de 0,01).

Ao contrário do painel gráfico, esta porta StockSharp concentra-se em pontos de entrada amigáveis à automação. Os chamadores (por exemplo, uma UI ou script personalizado) podem acionar `PlaceBuyOrders()` ou `PlaceSellOrders()` sempre que necessário, enquanto a estratégia cuida da normalização do volume, arredondamento de preços e colocação de pedidos de proteção.

## Parâmetros
| Nome | Descrição | Notas |
| ---- | ----------- | ----- |
| `TradeCount` | Número de ordens de mercado enviadas por ação. | Garante pelo menos zero. Padrão `1`. |
| `StopLossPips` | Distância de stop-loss em pips. | Zero desativa a parada de criação. Padrão `2`. |
| `TakeProfitPips` | Distância de lucro em pips. | Zero desativa a criação de destino. Padrão `10`. |
| `VolumePerTrade` | Volume para cada ordem de mercado individual. | Arredondado por `Security.VolumeStep`. Padrão `0.01`. |
| `TargetSecurity` | Substituição opcional para o instrumento negociado. | Volta para `Strategy.Security` quando nulo. |

Todos os parâmetros são expostos por meio de `StrategyParam<T>` para que suportem otimização e reconfiguração de tempo de execução da IU do StockSharp.

## Fluxo de Execução
1. Resolva a segurança ativa (`TargetSecurity` ou `Strategy.Security`).
2. Derive o tamanho do pip dos metadados do instrumento: `PriceStep` multiplicado por 10 quando o instrumento tiver mais de 3 decimais, idêntico à lógica MQL que multiplica símbolos com 3 ou 5 dígitos.
3. Obtenha o preço de referência mais recente (melhor compra/venda, voltando à última negociação) e arredonde-o para `Security.ShrinkPrice`.
4. Calcule o volume desejado: `TradeCount × VolumePerTrade`, alinhe-o com os limites de troca (`MinVolume`, `MaxVolume`, `VolumeStep`) e ajuste para uma posição aberta oposta para que uma ação possa nivelar e reverter.
5. Envie uma ordem de mercado via `BuyMarket` ou `SellMarket`.
6. Crie ordens de proteção (stop e limit) usando as compensações de pip, novamente normalizadas para o tamanho do tick da bolsa.
7. Cancele ordens de proteção obsoletas sempre que a posição mudar ou a estratégia parar.

## Lógica de Ordem Protetora
- As entradas longas colocam um `SellStop` para o stop loss e um `SellLimit` para o take-profit.
- As entradas curtas colocam um `BuyStop` para o stop loss e um `BuyLimit` para o take-profit.
- Cada ordem de proteção cobre o volume do painel recém-solicitado (o mesmo valor que uma única ação no painel MQL original).
- Os pedidos são cancelados automaticamente em `OnStopped`, `OnReseted` e sempre que o lado oposto for acionado.

## Notas de uso
- Atribua `Strategy.Security` no aplicativo host ou forneça um `TargetSecurity` antes de chamar os métodos do painel; caso contrário, nenhuma negociação será enviada.
- Invoque `PlaceBuyOrders()` para replicar o botão MQL “COMPRAR” e `PlaceSellOrders()` para o botão “VENDER”.
- Os preços dependem de dados de mercado ao vivo. Se nem a melhor oferta/venda nem a última negociação estiverem disponíveis, a estratégia registra um erro e ignora o envio da ordem.
- O auxiliar chama `StartProtection()` em `OnStarted` para proteger contra posições obsoletas após reinicializações.
- Quando os metadados do instrumento não incluem `PriceStep`, o tamanho do pip é padronizado como `0.0001` (um pip para a maioria dos símbolos FX); defina `PriceStep` explicitamente se seu corretor usar incrementos alternativos.

## Diferenças em comparação com o painel MQL
- Não há interface gráfica incorporada. Espera-se que os integradores construam sua própria interface ou acionem os métodos públicos a partir de lógica externa.
- As ordens de proteção são agregadas por ação, e não por ticket MT5 individual. A exposição líquida resultante corresponde ao comportamento MT5, mantendo a implementação StockSharp concisa.
- A validação de volume e preço segue convenções StockSharp (`Security.ShrinkPrice`, `VolumeStep`, `MinVolume`, `MaxVolume`). Isso evita pedidos rejeitados em locais com incrementos rigorosos.
- O registro de execução é fornecido por meio de `LogInfo` e `LogError` para auxiliar no monitoramento em terminais StockSharp.

## Primeiros passos
1. Instancie a estratégia, atribua portfólio e segurança (ou defina `TargetSecurity`).
2. Inicie a estratégia para que `StartProtection()` arma as salvaguardas internas.
3. Chame `PlaceBuyOrders()` ou `PlaceSellOrders()` com base na entrada do usuário ou em gatilhos automatizados.
4. Monitore o log em busca de mensagens de confirmação e gerencie lógica de UI adicional conforme necessário.

Esta conversão manual do painel de negociação oferece uma reprodução leve, porém fiel, do consultor especialista MT5 original, adaptado à estrutura estratégica de alto nível de StockSharp.
