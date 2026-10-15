# Alligator Estratégia de Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de volatilidade Alligator é uma versão StockSharp de alto nível do consultor especialista "Alligator vol 1.1" MetaTrader. Ele combina o indicador Bill Williams' Alligator com confirmação opcional de quebra de fractal, pedidos médios no estilo martingale e gerenciamento de risco de rastreamento. O módulo é destinado a traders discricionários que desejam automatizar o fluxo de trabalho original, mantendo ao mesmo tempo o controle granular sobre o tamanho e os filtros da posição.

## Visão geral da lógica

- Assina as velas do período de tempo selecionado e calcula três médias móveis suavizadas (mandíbula, dentes, lábios) que formam o indicador Alligator.
- Detecta fases de alta quando os lábios ficam acima da mandíbula pelo menos no `EntryGap` configurado e permanecem acima dos dentes em `ExitGap`. As fases de baixa exigem que a mandíbula domine os lábios enquanto permanece acima dos dentes.
- Rastreia os fractais Williams de Bill nas últimas `FractalBars` velas. O filtro de fuga fractal é opcional e garante novos máximos para posições compradas ou mínimos recentes para posições vendidas.
- Coloca uma ordem de mercado inicial assim que um novo estado Alligator aparece. Quando o martingale está ativado, as ordens de limite de média adicionais são distribuídas em torno de múltiplos da distância de stop-loss com dimensionamento de posição exponencial.
- Gerencia saídas de posição por meio de take-profit, stop-loss, trailing stop opcional e reversão de estado Alligator opcional.

## Regras de entrada

1. A estratégia aguarda o término das velas e ignora os dados parciais.
2. Uma configuração longa requer um dos seguintes:
   - Entrada Alligator habilitada, o estado de alta muda de falso para verdadeiro e (se habilitado) um fractal superior válido está a pelo menos `FractalDistancePips` de distância do fechamento atual.
   - Entrada Alligator desabilitada, mas (se habilitada) a condição de quebra fractal ainda passa.
3. Uma configuração curta reflete as condições longas usando o estado de baixa Alligator e fractais inferiores.
4. O parâmetro `ManualMode` bloqueia entradas automáticas, permitindo o envio discricionário de pedidos por meio da IU.
5. Quando `OnlyOnePosition` for verdadeiro, a estratégia se recusa a abrir uma nova posição se já existir uma exposição oposta.

## Regras de saída

- As paradas iniciais e os alvos são anexados imediatamente após o aumento da posição. As distâncias são calculadas a partir do preço médio de entrada usando `StopLossPips` e `TakeProfitPips` convertidos com a etapa de preço do instrumento.
- Se `EnableTrailing` for verdadeiro, o stop segue o preço após a negociação ganhar pelo menos `TrailingActivationPips` de lucro. Os longos seguem abaixo do fechamento/máxima da vela mais alta, os shorts seguem acima do fechamento/mínima mais baixo.
- Quando `UseAlligatorExit` está ativo, a posição fecha assim que o estado Alligator entra em colapso (o estado de alta desaparece para posições longas ou o estado de baixa desaparece para posições curtas).
- Atingir o preço de take-profit ou stop-loss fecha a posição e cancela as ordens de média pendentes desse lado.

## grade Martingale

- `EnableMartingale` ativa uma escada de ordens limitadas após a entrada no mercado.
- Cada etapa multiplica o volume executado anteriormente por `2 * MartingaleMultiplier` (limitado a `MaxVolume`).
- Os preços limite são espaçados pela distância do stop loss (`StopLossPips`) e deslocados em `GridSpreadPips` para compensar o spread do corretor.
- As ordens pendentes são canceladas sempre que um novo sinal é processado, a posição é achatada ou ocorre uma saída manual.

## Gestão de dinheiro

- O volume do pedido é calculado a partir do patrimônio da conta usando `RiskPerThousand`: `volume = equity / 1000 * RiskPerThousand`.
- `MinVolume` atua como substituto quando as informações de patrimônio não estão disponíveis. `MaxVolume` limita as etapas iniciais de negociação e martingale.
- Todos os preços são arredondados para o tick de câmbio mais próximo antes do envio dos pedidos.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Tipo de dados usado para assinatura de velas. | Período de 15 minutos |
| `ManualMode` | Desabilite entradas automáticas quando verdadeiro. | `false` |
| `UseAlligatorEntry` | Requer expansão Alligator antes de entrar. | `true` |
| `UseFractalFilter` | Aplicar confirmação de quebra fractal. | `false` |
| `UseAlligatorExit` | Feche as negociações quando o Alligator entrar em colapso. | `false` |
| `OnlyOnePosition` | Permitir apenas uma única posição aberta. | `true` |
| `EnableMartingale` | Adicione pedidos com limite médio. | `true` |
| `EnableTrailing` | Ative o gerenciamento de trailing stop. | `true` |
| `RiskPerThousand` | Multiplicador de volume baseado em patrimônio. | `0.04` |
| `MaxVolume` | Tamanho máximo permitido do pedido. | `0.5` |
| `MinVolume` | Tamanho do pedido substituto. | `0.01` |
| `StopLossPips` / `TakeProfitPips` | Distância para parar e mirar em pips. | `80` |
| `TrailingStopPips` | Distância de parada final em pips. | `30` |
| `TrailingActivationPips` | Lucro necessário antes dos ajustes finais. | `20` |
| `EntryGap` | Folga mínima entre lábios e mandíbula (unidades de preço). | `0.0005` |
| `ExitGap` | Separação mínima dos dentes (unidades de preço). | `0.0001` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Comprimentos SMMA para as linhas Alligator. | `13 / 8 / 5` |
| `JawShift`, `TeethShift`, `LipsShift` | Mudança de barra aplicada ao avaliar sinais. | `8 / 5 / 3` |
| `FractalBars` | Número de velas verificadas em busca de fractais. | `10` |
| `FractalDistancePips` | Distância necessária entre preço e fractal. | `30` |
| `MartingaleDepth` | Número de pedidos com limite médio. | `10` |
| `MartingaleMultiplier` | Multiplicador adicional para calcular o volume médio. | `1.3` |
| `GridSpreadPips` | Deslocamento de spread aplicado à grade. | `10` |

## Notas

- O indicador Alligator é processado nas medianas das velas e usa atrasos de uma barra para evitar trabalhar com valores inacabados.
- `EntryGap` e `ExitGap` são expressos em unidades de preço absoluto. Ajuste-os para corresponder ao tamanho do tick do instrumento, se necessário.
- A detecção fractal reflete o padrão Bill Williams de cinco barras. Quando o filtro está ativo, ele ignora as configurações até que um histórico suficiente seja coletado.
- A estratégia não cria ordens de proteção stop ou take-profit na bolsa. Todas as saídas são tratadas internamente pela lógica da estratégia.
- São suportadas alterações manuais em pedidos pendentes ou ativos; a estratégia limpa suas grades internas quando os pedidos são atendidos ou cancelados.
