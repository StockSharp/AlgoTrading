# Estratégia de Hoop Master
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Hoop Master é um sistema de rompimento pendente que mantém continuamente duas ordens stop em torno do preço atual. O consultor especialista MetaTrader 5 original coloca um buy stop acima do mercado e um sell stop abaixo do mercado. Quando um lado é acionado, a ordem oposta é cancelada e ambos os lados são recriados com um volume maior. O port do StockSharp segue a mesma ideia gerenciando ordens stop e dimensionamento martingale opcional dentro de uma única classe de estratégia.

A estratégia também pode anexar ordens protetoras de stop-loss e take-profit a qualquer posição aberta. Um módulo de trailing stop move gradualmente o stop protetor quando o mercado avança na direção do trade.

## Lógica de trading

1. A cada vela concluída, a estratégia recalcula os níveis de colocação para os stops de rompimento.
2. Se nenhuma posição estiver aberta, tanto um buy stop quanto um sell stop são registrados a uma distância configurável em pips do bid/ask atual.
3. Quando qualquer stop pendente é preenchido, o stop oposto é removido. Novos stops de rompimento são enviados imediatamente usando o dobro do volume base.
4. Após um trade ser aberto, a estratégia cria ordens independentes de stop-loss e take-profit. Um motor de trailing pode mover o stop em direção ao preço quando o movimento for suficientemente grande.
5. Quando a posição é fechada, todas as ordens de proteção são canceladas e as ordens de rompimento são re-inicializadas com o volume base no próximo sinal.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| **Candle Type** | Tipo de dados de vela usado para a lógica barra a barra. |
| **Order Volume** | Volume base para cada ordem de rompimento. O passo martingale usa o dobro desta quantidade. |
| **Stop Loss (pips)** | Distância em pips entre o preço de entrada e a ordem stop protetora. Definir como 0 para desabilitar. |
| **Take Profit (pips)** | Distância em pips entre o preço de entrada e a ordem alvo protetora. Definir como 0 para desabilitar. |
| **Trailing Stop (pips)** | Distância usada ao mover o trailing stop. Definir como 0 para desabilitar o trailing. |
| **Trailing Step (pips)** | Melhoria de preço mínima (em pips) necessária antes que o trailing stop seja atualizado. |
| **Indent (pips)** | Offset, medido em pips, adicionado acima do ask e abaixo do bid ao colocar stops de rompimento. |

## Detalhes de gestão de ordens

- A estratégia rastreia continuamente as melhores cotações de bid/ask. Quando as cotações não estão disponíveis, recorre ao último preço de negociação ou fechamento de vela.
- Todas as ordens são alinhadas ao passo de preço do instrumento para evitar preços inválidos.
- Ordens protetoras de stop e take-profit são substituídas sempre que uma nova posição aparecer.
- O trailing só funciona quando tanto a distância de trailing quanto os parâmetros de passo estão acima de zero. O stop é movido na direção do trade quando a melhoria desejada for suficientemente grande.

## Notas

- Certifique-se de que o corretor ou simulador conectado suporte ordens stop e limite para o instrumento selecionado.
- O passo martingale pode aumentar a exposição rapidamente. Ajuste o volume base para permanecer dentro dos limites de risco aceitáveis.
- A estratégia espera receber dados de Nível1 (bid/ask) junto com dados de velas para que os preços de rompimento possam ser calculados com precisão.
