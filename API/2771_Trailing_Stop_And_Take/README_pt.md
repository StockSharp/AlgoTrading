# Estratégia de Trailing Stop e Take
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Trailing Stop e Take** é uma adaptação direta do StockSharp do consultor especialista MetaTrader de `MQL/19963`. Foca no gerenciamento ativo de operações: uma vez que uma posição está aberta, a estratégia anexa níveis iniciais de stop-loss e take-profit e então segue ambos os níveis conforme o preço se move. Os ajustes de trailing respeitam tamanhos mínimos de passo configuráveis, proteção de break-even e a opção de evitar o trailing enquanto uma operação ainda está em perda.

A estratégia opera em um único instrumento usando velas concluídas. Quando a estratégia está plana, abre uma posição na direção do corpo da vela mais recente (fechamentos de alta levam a posições compradas, fechamentos de baixa levam a posições vendidas). Isso reflete o comportamento de teste original usado pelo script MQL e fornece um fluxo contínuo de posições para o motor de trailing gerenciar.

## Como funciona
1. Subscrever ao tipo de vela configurado e processar apenas velas concluídas.
2. Quando não há posição aberta, entrar comprado em velas de alta ou vendido em velas de baixa (respeitando o filtro de tipo de posição).
3. Em uma nova posição, inicializar as distâncias de stop-loss e take-profit usando `InitialStopLossPoints`/`InitialTakeProfitPoints`. Se forem zero, as distâncias de trailing são usadas em seu lugar.
4. Em cada fechamento de vela, calcular os alvos de trailing atualizados:
   - Os stops se aproximam do preço apenas depois que o mercado avança pelo passo de trailing.
   - Os take-profits se aproximam quando o preço recua pelo menos o passo de trailing.
   - A proteção de break-even evita mover os níveis para uma zona de perda quando `AllowTrailingLoss` está desabilitado.
5. Quando o preço cruza um trailing stop ou nível de take-profit, sair com ordem de mercado e redefinir todos os níveis armazenados.

## Lógica de trailing
### Posições compradas
- O stop inicial está limitado a pelo menos `SpreadMultiplier * PriceStep` de distância da entrada.
- O take-profit inicial é posicionado pelo menos a mesma distância mínima acima da entrada.
- O trailing stop segue o preço de fechamento para baixo em `TrailingStopLossPoints` respeitando o passo de trailing e o filtro de break-even opcional.
- O trailing take-profit aperta quando o preço recua, nunca se movendo abaixo do nível de break-even quando o trailing em perdas está desabilitado.

### Posições vendidas
- O stop inicial é definido acima da entrada, não mais próximo que a distância do multiplicador de spread.
- O take-profit inicial começa abaixo da entrada com a mesma regra de distância mínima.
- O trailing stop cai quando o preço cai, mas não se moverá mais alto que o break-even a menos que o trailing de perda seja permitido.
- O trailing take-profit sobe em direção ao preço nos recuos, limitado ao break-even quando necessário.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Agregação de velas usada para avaliação de preços. |
| `Volume` | Volume de ordem padrão para entradas e saídas. |
| `PositionType` | Restringe o motor a gerenciar posições compradas, vendidas ou ambas. |
| `InitialStopLossPoints` | Tamanho inicial do stop-loss em pontos de preço (usa distância de trailing se zero). |
| `InitialTakeProfitPoints` | Tamanho inicial do take-profit em pontos de preço (usa distância de trailing se zero). |
| `TrailingStopLossPoints` | Distância entre o preço e o trailing stop. |
| `TrailingTakeProfitPoints` | Distância entre o preço e o trailing take-profit. |
| `TrailingStepPoints` | Movimento mínimo em pontos necessário antes de ajustar stops ou alvos. |
| `AllowTrailingLoss` | Habilita o trailing enquanto a operação ainda está abaixo do break-even. |
| `BreakevenPoints` | Deslocamento em pontos adicionado ao preço de entrada para formar a barreira de break-even. |
| `SpreadMultiplier` | Multiplicador para a aproximação de distância mínima do stop (simula o `StopLevel` do MQL). |

## Notas
- Os stops e alvos são executados com ordens de mercado quando acionados, o que mantém a implementação simples e reflete as modificações de stop originais.
- `SpreadMultiplier` aproxima o comportamento do MQL onde os níveis de stop não podem ser colocados mais perto que o spread atual. Ajuste este valor para corresponder ao local de execução.
- A estratégia evita intencionalmente uma versão Python e se concentra exclusivamente na implementação C#, conforme solicitado.
- Considere combinar o motor de trailing com seu próprio filtro de entrada desabilitando as entradas integradas e injetando ordens externas se necessário.
