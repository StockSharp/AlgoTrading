# Estratégia de limite de mudança de sorte
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Lucky Shift Limit** é uma conversão direta do MetaTrader 4 consultor especialista `Lucky_acnl6p6j89zn91fa.mq4`. Ele observa as melhores cotações de compra/venda em tempo real e reage a saltos repentinos medidos em MetaTrader "pontos" (pips). Quando o preço de venda acelera para cima pela distância de mudança configurada, a estratégia atenua o movimento através da venda, enquanto uma queda acentuada na oferta provoca uma compra contrária. Todas as negociações abertas são constantemente monitoradas e fechadas quando se tornam lucrativas ou quando a perda flutuante excede um limite de segurança idêntico à lógica MQ4 original.

## Requisitos de dados e execução

- **Dados de mercado** – assina apenas cotações de Nível 1; nenhuma vela ou profundidade de mercado são necessárias.
- **Estilo de execução** – entradas e saídas dependem de ordens de mercado para imitar as chamadas `OrderSend` imediatas de MetaTrader.
- **Modo de conta** – funciona com contas de hedge e de compensação. Nas contas de compensação, a estratégia acumula exposição em uma única posição e o módulo de saída a nivela.
- **Dimensionamento do volume** – o tamanho padrão do pedido vem de `Strategy.Volume`, mas o auxiliar emula `AccountFreeMargin/10000` de MetaTrader quando o valor do portfólio está disponível.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Shift points` | 3 | Número mínimo de MetaTrader pontos entre pedidos/ofertas consecutivos que aciona um novo pedido. Valores maiores filtram o ruído, valores menores reagem mais rapidamente. |
| `Limit points` | 18 | Excursão adversa máxima permitida para uma negociação aberta. Se o preço se mover contra a posição em tantos pontos, a negociação será fechada à força. |

Ambos os parâmetros são expressos em MetaTrader pontos e convertidos internamente em compensações de preço absoluto usando o tamanho do tick do instrumento. Os limites de otimização na UI correspondem aos intervalos práticos da versão MQ4.

## Lógica de negociação

1. **Inicialização**
   - Converte as configurações baseadas em pontos em distâncias de preços reais usando `Security.PriceStep`.
   - Redefine cotações de compra/venda em cache e inicia uma assinatura de nível 1 com processamento `Bind` de alto nível.
2. **Condições de entrada**
   - Se o pedido aumentar pelo menos `Shift points` em comparação com o pedido anterior, a estratégia envia uma ordem de venda a mercado (desvanecendo o pico) com uma nota de registro explicando o gatilho.
   - Se o lance cair pelo menos na mesma distância em relação ao lance anterior, abre-se uma compra no mercado.
   - Os sinais podem disparar várias vezes em sequência, exatamente como o especialista original que não restringiu o número de posições simultâneas.
3. **Gerenciamento de saídas**
   - Cada tick de cotação invoca `TryClosePosition()`. As posições longas são fechadas imediatamente quando o lance está acima da entrada média (lucro realizado) ou quando o pedido é inferior à entrada em `Limit points` (limite de perda).
   - As posições vendidas refletem essa lógica, fechando em cotações de venda lucrativas ou quando o lance ultrapassa a entrada pelo limite configurado.
   - Todas as saídas usam ordens de mercado para replicar `OrderClose` e garantir que a posição seja achatada no mesmo tick.
4. **Dimensionamento da posição**
   - Calcula o volume padrão do patrimônio do portfólio (`equity / 10,000`, arredondado para um lote decimal) quando disponível, correspondendo ao auxiliar MQ4 `GetLots()`.
   - Volta para a propriedade da estratégia `Volume` quando faltam dados de patrimônio.

## Notas de implementação

- Usa apenas APIs StockSharp de alto nível: `SubscribeLevel1().Bind(ProcessLevel1)` elimina a necessidade de ouvintes de cotação manuais.
- Nenhuma coleção personalizada é armazenada; os valores de compra/venda anteriores são mantidos em variáveis ​​simples anuláveis, conforme permitido pelas diretrizes.
- O limite de perda funciona com o tamanho do tick do instrumento, portanto, símbolos exóticos com etapas fracionárias de pip são mapeados automaticamente para o delta de preço correto.
- As alterações de parâmetros durante o tempo de execução são respeitadas – a estratégia recalcula os limites quando os dados do Nível 1 chegam.
- As instruções de registro documentam todos os motivos de entrada e saída, o que simplifica o backtesting e o diagnóstico em tempo real.

## Dicas de uso

- Ideal para pares de FX de alta liquidez ou índices onde choques de compra/venda ocorrem com frequência.
- Considere combinar a estratégia com proteções em nível de portfólio (`StartProtection`) se forem necessários limites adicionais de stop loss ou drawdown.
- Aumente `Shift points` em feeds barulhentos para reduzir o overtrading ou diminua-o para capturar movimentos de ultracurto prazo.
- A lógica é inerentemente contrária; se o comportamento de fuga for desejado, basta definir `Shift points` alto o suficiente ou combiná-lo com outro indicador de filtro.
