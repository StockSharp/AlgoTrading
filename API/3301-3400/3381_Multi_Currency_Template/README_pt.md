# Estratégia de modelo multimoeda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de modelo multimoeda** é uma conversão do consultor especialista MetaTrader 4 *Modelo multimoeda v4*. Ele reproduz a lógica de entrada cruzada EMA original junto com a média do estilo martingale, níveis de proteção baseados em pip e gerenciamento de trilha usando o StockSharp API de alto nível. O período padrão é de velas de cinco minutos, mas pode ser alterado através de um parâmetro.

## Lógica Comercial
- Duas médias móveis exponenciais (EMA 20 e EMA 50) são calculadas em cada vela concluída do período de tempo selecionado.
- Um sinal longo aparece quando o EMA rápida (20) fecha acima do EMA lenta (50). Um sinal curto aparece quando o EMA rápida fecha abaixo do EMA lenta.
- O parâmetro `Order Method` decide se a estratégia atua em ambos os sinais ou restringe a negociação a operações longas ou curtas.
- Apenas uma posição líquida por direção é mantida. Quando chega um novo sinal, a estratégia fecha qualquer posição oposta antes de abrir o lado solicitado.

## Gerenciamento de posição
- **Stop Loss / Take Profit** – as distâncias são inseridas em MetaTrader pips. Eles são convertidos em unidades de preço usando a etapa de preço do título, reproduzindo o tratamento original dos símbolos Forex de 4 e 5 dígitos.
- **Trailing Stop** – é ativado quando o preço se move a favor da posição em `Trailing Stop (pts)` e é reduzido após cada melhoria adicional de `Trailing Step (pts)`.
- **Martingale Média** – quando ativado, ordens de mercado adicionais são enviadas a cada `Step (pts)` em relação à posição atual. Cada novo volume de pedido é dimensionado em `Lot Multiplier` e o processo se repete até que a posição seja fechada.
- **Take Profit Médio** – quando duas ou mais ordens médias estão abertas, o alvo do Take Profit pode opcionalmente usar o preço de posição ponderado mais `Average TP Offset (pts)` para emular o comportamento de MetaTrader “média TP”.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| Método de pedido | Direção comercial (Compra e Venda, Somente Compra, Somente Venda). | Comprar e vender |
| Volume (lotes) | Tamanho base da ordem de mercado. | 0,01 |
| Stop Loss (pips) | Distância de parada protetora em MetaTrader pips. | 50 |
| Obter lucro (pips) | Distância alvo de lucro em MetaTrader pips. | 100 |
| Parada final (pontos) | Limite de ativação para o trailing stop em MetaTrader pontos. | 15 |
| Etapa final (pts) | Melhoria mínima necessária antes que o trailing stop seja movido. | 5 |
| Ativar Martingale | Permite diminuir/aumentar a média com o aumento do volume. | verdade |
| Multiplicador de lote | Multiplicador de volume aplicado a cada nova ordem média. | 1.2 |
| Etapa (pontos) | Distância de MetaTrader pontos antes de fazer o próximo pedido de média. | 150 |
| Lucro médio | Alterne entre lucro fixo ou médio quando existirem vários pedidos. | verdade |
| Deslocamento médio de TP (pts) | MetaTrader deslocamento de pontos aplicado ao lucro médio médio. | 20 |
| Tipo de vela | Tipo de vela (período de tempo) usado para cálculos de indicadores. | Velas de 5 minutos |

## Diferenças em relação ao Expert Advisor original
- StockSharp executa posições líquidas em vez de gerenciar tickets MetaTrader individuais. O módulo martingale aumenta o tamanho da posição líquida em vez de anexar metas separadas específicas para tickets.
- A negociação de vários símbolos deve ser alcançada através do lançamento de várias instâncias de estratégia, uma por título. O consultor especialista original suportava uma lista integrada de várias moedas dentro de uma instância EA.
- Verificações de gerenciamento de dinheiro (`CheckMoneyForTrade`, `CheckVolumeValue`) e restrições específicas do corretor são substituídas pela validação de pedido StockSharp.

## Notas de uso
1. Certifique-se de que os metadados de segurança (etapa de preço e decimais) correspondam ao instrumento para que a conversão do pip permaneça precisa.
2. O trailing stop e a lógica martingale atuam nos preços de fechamento das velas por padrão. Para um comportamento mais reativo, conecte fontes de dados adicionais (cotações ou negociações) e chame os ajudantes de gerenciamento a partir daí.
3. Como são utilizadas ordens de mercado, o controle de derrapagem é delegado ao corretor ou simulador conectado.
