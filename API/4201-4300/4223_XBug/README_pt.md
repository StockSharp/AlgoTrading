# Estratégia de Bug X
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia X Bug** é um sistema cruzado de média móvel convertido do consultor especialista MQL4 com o mesmo nome. Ele compara duas médias móveis simples calculadas sobre o preço médio da vela. Quando a média rápida cruza acima ou abaixo da média lenta, a estratégia abre uma posição na direção do cruzamento. A implementação reproduz os recursos originais do Expert Advisor, incluindo reversão de sinal opcional, fechamento automático de posição em sinais opostos e ordens de proteção baseadas em pip.

## Lógica de negociação
1. Assine o tipo de vela configurado (velas de um minuto por padrão) e calcule duas médias móveis simples: uma linha rápida e uma linha lenta. As médias utilizam o preço mediano e respeitam as mudanças do indicador configuradas.
2. Detecte um cruzamento de alta quando o valor rápido atual estiver acima do valor lento, enquanto o valor rápido duas barras anteriores estiver abaixo do valor lento. Detecte um cruzamento de baixa usando a condição oposta.
3. Opcionalmente, inverta o sinal de cruzamento quando **ReverseSignals** estiver habilitado para negociar na direção oposta.
4. Quando **CloseOnSignal** estiver habilitado, feche imediatamente qualquer posição oposta antes de inserir uma nova no novo sinal.
5. Insira posições longas em sinais de alta e posições curtas em sinais de baixa. A estratégia evita empilhar posições na mesma direção; ele só negocia quando a posição atual é plana ou alinhada com o sinal.

## Gestão de risco
- **StopLossPips** – define um stop de proteção absoluto em pips. O stop é expresso em pips inteiros; o preço fracionário (cotações de 5 ou 3 dígitos) é tratado automaticamente pela conversão do valor do pip usando a etapa de preço do título.
- **TakeProfitPips** – configura a distância alvo de lucro em pips.
- **TrailingStopPips** – quando **UseTrailingStop** está habilitado, ativa um trailing stop que começa na distância do pip configurada assim que a posição se move para lucro. A etapa final corresponde à distância final, replicando a lógica MetaTrader original.
- Todas as ordens de proteção são gerenciadas por meio de `StartProtection` com saídas de mercado para manter a paridade com o especialista MQL4.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Volume base de negociação usado para entradas no mercado. | `0.1` |
| `StopLossPips` | Distância de stop-loss medida em pips; defina como `0` para desativar. | `70` |
| `TakeProfitPips` | Distância de take-profit medida em pips; defina como `0` para desativar. | `5000` |
| `UseTrailingStop` | Ativa ou desativa o gerenciamento de trailing stop. | `true` |
| `TrailingStopPips` | Distância final em pips. | `90` |
| `FastPeriod` | Período da média móvel rápida. | `1` |
| `FastShift` | Barras para mudar a média móvel rápida antes de avaliar os sinais. | `0` |
| `SlowPeriod` | Período da média móvel lenta. | `14` |
| `SlowShift` | Barras para mudar a média móvel lenta antes de avaliar os sinais. | `10` |
| `CloseOnSignal` | Feche uma posição oposta imediatamente quando um novo sinal aparecer. | `true` |
| `ReverseSignals` | Inverta a direção do sinal para negociar contra o cruzamento. | `false` |
| `AppliedPrice` | Fonte de preços de velas fornecida às médias móveis. | `Median` |
| `CandleType` | Tipo de dados Candle para geração de sinal. | `1 minute` período de tempo |

## Notas
- A conversão do pip multiplica o passo do preço por 10 para símbolos cotados com 5 ou 3 casas decimais, correspondendo ao comportamento original do Expert Advisor.
- Nenhuma porta Python é fornecida; apenas a estratégia C# está incluída neste diretório.
- Paradas finais, paradas e alvos são opcionais. Defina os valores pip correspondentes como zero para desativá-los.
