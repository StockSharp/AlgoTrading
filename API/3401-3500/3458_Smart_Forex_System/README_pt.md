# Estratégia Inteligente do Sistema Forex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia do Sistema Smart Forex é uma versão StockSharp do MetaTrader consultor especialista "Smart Forex System". O robô combina um filtro de impulso de vela única com uma grade de média estilo martingale. A primeira negociação é aberta quando a vela anterior mostra um forte fechamento direcional e o preço atual se afastou suficientemente do fechamento de referência. Entradas adicionais são adicionadas em intervalos fixos de pip na direção adversa, com o tamanho da posição aumentando por um multiplicador configurável. A estratégia gerencia as saídas por meio de níveis médios de lucro e um stop-loss de segurança vinculado à última ordem de rede.

## Lógica de negociação
- **Geração de sinal**
  - Avalie a última vela concluída no período selecionado.
  - Calcule uma taxa de impulso: `(current close - previous close) / previous close * 10,000`.
  - Se a vela anterior for de baixa e o momentum for inferior ao limite negativo, uma cesta longa pode começar.
  - Se a vela anterior for de alta e o momentum exceder o limite positivo, uma cesta curta poderá começar.
  - A negociação pode ser limitada apenas a posições compradas, apenas vendidas, em ambas as direções ou totalmente desativada por meio do parâmetro `Trading Mode`.
- **Expansão da rede**
  - Uma vez existente uma cesta, novas entradas são adicionadas sempre que o preço se move contra a posição em pelo menos `Grid Step` pips em relação ao preço da última ordem.
  - Cada novo volume de pedido é multiplicado por `Lot Multiplier`. Os volumes são limitados aos limites do corretor e ao `Max Volume` configurado.
  - A cesta para de crescer quando o número de pedidos atinge `Max Trades`.
- **Gerenciamento de saídas**
  - Um stop loss rígido é colocado a `Stop Loss` pips de distância do preço do pedido mais recente. Romper essa distância fecha a cesta inteira.
  - Os níveis de lucro dependem do tamanho da cesta:
    - Um único pedido usa `First Take Profit` pips do preço médio de entrada ponderado pelo volume.
    - Vários pedidos usam `Grid Take Profit` pips do mesmo preço médio de entrada para capturar rebotes menores.
  - As saídas são processadas nas velas finalizadas para garantir que os indicadores tenham valores finais.

## Notas de gerenciamento de risco
- O dimensionamento da posição tipo martingale aumenta dramaticamente a exposição em tendências adversas. Use multiplicadores conservadores e tamanhos de cesta em instrumentos altamente voláteis.
- O stop-loss padrão (400 pips) é intencionalmente amplo para espelhar o EA original. Considere alinhá-lo com o ATR do instrumento se forem necessárias perdas menores.
- A negociação em grade consome margem rapidamente. Certifique-se de que a alavancagem da conta, o tamanho do contrato e os parâmetros `Start Volume` sejam consistentes com as especificações do corretor.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| Modo de negociação | Direção comercial permitida (somente longa, somente curta, ambas ou desativada). | Longo e curto |
| Limite de impulso | Momento mínimo em pseudo-pips necessário para acionar um sinal. | 1 |
| Volume inicial | Volume do primeiro pedido em uma nova cesta. | 0,01 |
| Volume máximo | Limite rígido aplicado a qualquer volume de pedido único. | 2 |
| Multiplicador de lote | Multiplicador usado ao dimensionar pedidos de grade subsequentes. | 1,5 |
| Etapa da grade | Distância mínima em pips antes de adicionar o próximo pedido. | 26 |
| Máximo de negociações | Número máximo de pedidos permitidos por direção. | 12 |
| Primeiro obtenha lucro | Distância de lucro em pips quando apenas uma ordem está aberta. | 30 |
| Grade obtém lucro | Distância de lucro em pips quando a cesta contém vários pedidos. | 7 |
| Parar perda | Pare a distância em pips do último preço do pedido. | 400 |
| Tipo de vela | Prazo usado para avaliação do sinal. | Velas de 1 hora |

## Uso recomendado
1. Anexe a estratégia a um símbolo Forex com liquidez suficiente e um spread previsível.
2. Defina o `Candle Type` para corresponder ao período operacional original do EA (H1 por padrão) ou adapte-o ao seu horizonte preferido.
3. Otimize o espaçamento da grade, o multiplicador e o filtro de impulso em dados históricos antes da implantação em tempo real.
4. Monitore de perto o uso da margem. A cesta pode crescer rapidamente, portanto considere combinar a estratégia com proteção de patrimônio em toda a conta.
5. Evite a sobreposição com outros sistemas baseados em grade no mesmo instrumento para reduzir o risco de rebaixamentos agravados.

## Diferenças em comparação com a versão MetaTrader
- A porta StockSharp funciona com velas finalizadas em vez de atualizações tick-by-tick, o que reduz o ruído e torna a lógica determinística.
- Os volumes de pedidos são ajustados usando StockSharp metadados de segurança (mínimo, máximo e passo), garantindo compatibilidade com uma ampla variedade de corretores.
- As verificações de take-profit e stop-loss são tratadas dentro da lógica da estratégia, em vez de enviar modificações de pedidos individuais para cada nível da grade.
