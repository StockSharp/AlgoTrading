# Estratégia de Grade com Cobertura Tunnel Gen4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a lógica do especialista MetaTrader "Tunnel gen4" usando a API de alto nível do StockSharp. Ela mantém uma cobertura neutra ao mercado abrindo um par inicial de compra/venda, dobra a posição na direção do rompimento quando o preço percorre um número configurável de pips e sai de toda a cesta quando a mesma distância é percorrida novamente além da segunda âncora.

## Lógica de Negociação

- **Cobertura inicial:** Assim que não existe exposição, a estratégia envia ordens de compra e venda simultâneas a mercado com volume `StartVolume`. A primeira execução define o preço de referência para todas as decisões subsequentes.
- **Detecção de passo:** O `StepPips` configurado é convertido em um deslocamento de preço usando o tamanho do tick do instrumento (com ajustes automáticos para cotações forex de três e cinco decimais). As atualizações do melhor bid/ask do fluxo Level 1 são comparadas com esse deslocamento.
- **Ordem de reforço:** Quando o melhor bid sobe pelo menos um passo desde a primeira execução, uma ordem de venda com o dobro do volume base é enviada. Quando o melhor ask cai pelo menos um passo, uma ordem de compra do mesmo tamanho é emitida. A primeira execução dessa ordem se torna a segunda âncora.
- **Encerramento do ciclo:** Após a segunda âncora estar ativa, qualquer movimento adicional do tamanho de um passo em qualquer direção aciona a liquidação completa de todas as posições abertas. Uma vez que ambos os lados estão fechados, o estado é reiniciado e um novo ciclo pode começar.
- **Validação de volume:** O início da estratégia verifica que tanto os volumes inicial quanto dobrado respeitam os requisitos mínimos, máximos e de incremento do instrumento, para que cada ordem enviada ao conector seja executável.

## Condições de Entrada

### Reforço comprado
- Há pelo menos uma posição aberta da cobertura inicial.
- A segunda âncora ainda não foi criada.
- O preço atual do melhor ask é menor ou igual a `first_fill_price - StepPips_em_preço`.

### Reforço vendido
- Há pelo menos uma posição aberta da cobertura inicial.
- A segunda âncora ainda não foi criada.
- O preço atual do melhor bid é maior ou igual a `first_fill_price + StepPips_em_preço`.

## Gerenciamento de Saída

- **Fechamento da cesta:** Uma vez que a segunda âncora está definida, se o melhor bid subir acima de `second_anchor + StepOffset` ou o melhor ask cair abaixo de `second_anchor - StepOffset`, ordens a mercado são enviadas para fechar a exposição longa e curta acumulada. As ordens de fechamento são rastreadas para garantir que o estado seja reiniciado somente após a confirmação de todas as negociações.
- **Reinício do estado:** Após ambos os lados estarem fechados e nenhuma ordem de fechamento permanecer ativa, a estratégia limpa as âncoras internas e aguarda uma nova cobertura ser aberta.

## Dados e Indicadores

- A assinatura Level 1 fornece os melhores preços bid e ask usados para comparações de passo.
- Nenhum indicador adicional é necessário; toda a lógica funciona com atualizações de cotação brutas.
- A conversão do passo de preço imita o ajuste ponto-a-pip do MetaTrader, para que símbolos forex com três ou cinco decimais se comportem da mesma forma que no especialista de origem.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `StartVolume` | Volume das ordens de compra e venda que formam a cobertura inicial. |
| `StepPips` | Distância em pips que aciona a ordem de reforço e a subsequente saída da cesta. |

## Notas de Implementação

- O StockSharp mantém uma posição líquida por ativo. A estratégia mantém contadores de exposição internos para emular os tickets longos e curtos separados usados pelo especialista MetaTrader e emite ordens a mercado com os volumes acumulados ao fechar a cesta.
- Como a lógica depende de spreads em tempo real, forneça dados Level 1 tanto em backtests quanto em sessões de trading ao vivo. A falta de informações bid/ask desabilita o loop de negociação.
- Certifique-se de que a conta de trading suporte ordens de compra e venda simultâneas para o mesmo instrumento, pois o algoritmo pressupõe que ambos os lados da cobertura possam coexistir até que a condição de saída seja atingida.
