# Estratégia DeMarker Pendente 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia replica a lógica central do especialista MetaTrader "DeMarker Pending 2" usando o StockSharp API de alto nível. Ele avalia um oscilador DeMarker no período de trabalho e prepara entradas pendentes de compra ou venda quando o indicador ultrapassa limites configuráveis. As ordens podem ser criadas como solicitações de stop ou limite com um recuo adicional do preço de mercado atual. Um filtro de sessão, proteção de propagação e verificações de distância mantêm as novas entradas sob controle.

## Lógica de negociação

1. Assine a série de velas configurada e calcule o indicador DeMarker com o período selecionado.
2. Quando o valor anterior estiver acima do nível inferior e o valor atual estiver abaixo dele, coloque uma ordem pendente longa na fila. Quando o valor anterior estiver abaixo do nível superior e o valor atual ultrapassar ele, coloque uma ordem pendente curta na fila. Apenas um sinal por vela é processado.
3. As ordens pendentes são colocadas como ordens stop ou limit usando a distância de recuo expressa em pontos. Os pedidos existentes podem ser cancelados antes da nova solicitação se a opção de substituição estiver habilitada. A estratégia limita o número total de posições abertas mais ordens pendentes e impõe uma distância mínima do preço médio atual da posição.
4. As posições longas e curtas usam lógica opcional de stop-loss, take-profit e trailing. Os níveis de proteção são calculados em faixas de preço e monitorados em cada vela fechada. Os trailing stops são ajustados quando a posição obtém o lucro de ativação e a distância adicional do trailing step.
5. Um filtro de spread evita novos pedidos se o melhor spread de compra/venda exceder o limite configurado. Limites de sessão opcionais podem desabilitar novas entradas fora da janela de negociação permitida.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| Velas de trabalho | Prazo usado para sinais e verificações de proteção. |
| Volume do pedido | Volume padrão para ordens pendentes. |
| Stop Loss (pontos) | Distância inicial do stop-loss em pontos de preço. |
| Obter lucro (pontos) | Distância inicial de lucro em faixas de preço. |
| Ativação final (pts) | Lucro necessário antes que o trailing stop seja ativado. |
| Parada final (pontos) | Distância entre o preço e o trailing stop. |
| Etapa final (pts) | Ganho adicional necessário para mover o trailing stop. |
| Trilha próxima | Atualize o trailing stop somente em velas finalizadas quando habilitado. |
| Posições máximas | Número máximo de posições abertas mais ordens pendentes. Zero desativa o limite. |
| Distância mínima (pontos) | Distância mínima do preço da posição atual até novas entradas pendentes. |
| Use ordens de parada | Coloque ordens de parada quando verdadeiro, caso contrário, serão usadas ordens de limite. |
| Único pendente | Permitir apenas uma ordem pendente ativa por vez. |
| Substituir Pendentes | Cancele pedidos pendentes pendentes antes de fazer um novo. |
| Deslocamento pendente (pts) | Recuo para novos preços pendentes em relação ao mercado. |
| Spread máximo (pontos) | Spread máximo permitido antes de pular a colocação do pedido. |
| Usar filtro de sessão | Ative ou desative o filtro da janela de negociação. |
| Hora/minuto inicial, hora/minuto final | Limites de sessão quando o filtro de sessão está ativo. |
| Período DeMarker | Período médio para o oscilador DeMarker. |
| Nível Superior | Limite que aciona configurações curtas. |
| Nível inferior | Limite que aciona configurações longas. |

## Notas

* A expiração do pedido e o dimensionamento do risco de gerenciamento de dinheiro do especialista original não são transferidos. Em vez disso, é usado um parâmetro de volume fixo.
* Os níveis de stop-loss e take-profit são avaliados em velas fechadas usando preços altos/baixos, que podem diferir da execução intrabarra em MetaTrader.
* A lógica final é avaliada apenas em velas fechadas. O rastreamento baseado em ticks em tempo real não é reproduzido.
* Os pedidos pendentes dependem das melhores cotações de compra/venda fornecidas pela fonte de dados. Certifique-se de que as assinaturas de nível 1 estejam disponíveis.
