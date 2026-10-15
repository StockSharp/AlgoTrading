# Estratégia de DVD de 100-50 centavos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia DVD 100-50 cent é um sistema contrário de ordem limite portado do consultor especialista MT4 original. A lógica avalia o mercado em quatro períodos de tempo (M1, M30, H1, D1) e pontua configurações potenciais antes de estacionar ordens de compra ou venda com limite em torno da grade de preços de “nível 100” mais próxima. Quando a ordem limite é preenchida, a estratégia gerencia a posição com níveis pré-calculados de stop-loss e take-profit.

## Indicadores e Dados
- **RAVI (Range Action Verification Index)** em H1 e D1, calculado com SMA(2) e SMA(24) no preço de abertura.
- **Dados brutos de velas** em M1, M30 e H1 para filtros de padrão, como rejeição de picos, verificações de consolidação e testes de impulso.
- **Arredondamento da grade de preços** que ajusta o preço atual para o nível 100 mais próximo usando um arredondamento de duas casas decimais e um deslocamento configurável de 0,1 pip.

## Lógica de entrada
1. Calcule o preço arredondado do "Nível 100" arredondando o último M1 para perto de duas casas decimais e deslocando-o em `PointFromLevelGoPips` (padrão 50 → 5 pips).
2. Inicialize uma pontuação interna (BAL) em 0 e adicione/subtraia pontos de acordo com:
   - **Filtro de tendência:** adicione 10 pontos quando H1 RAVI estiver abaixo de zero para configurações longas ou acima de zero para curtas.
   - **Confirmação de pico por hora:** adicione 7 pontos quando os dois máximos/mínimos do primeiro semestre anteriores ultrapassarem a grade em `RiseFilterPips`.
   - **Alinhamento da estrutura:** adicione 45 pontos quando o fechamento atual do M1 cruzar o nível e os últimos três mínimos/máximos H1 permanecerem acima/abaixo do buffer de segurança (`PointFromLevelGoPips ± 30 * 0.1 pip`).
   - **Protetores de volatilidade:** subtraia 50 pontos se os máximos/mínimos recentes do M1 excederem `HighLevelPips` (padrão 600 → 60 pips) ou se aparecerem explosões de impulso rápido enquanto o D1 RAVI confirma um forte regime direcional.
   - **Confirmação de breakout:** subtraia 50 pontos se as últimas 15 velas H1 nunca ultrapassaram o limite de `LowLevel2Pips`.
   - **Filtro de consolidação:** subtraia 50 pontos se as últimas oito velas M30 permanecerem dentro da banda `LowLevelPips`.
3. Faça uma ordem com limite somente quando a pontuação final for de pelo menos 50 e não existir outra exposição (posição ou ordem pendente).

## Colocação de pedidos
- **Limite de compra:** 10 pips abaixo do último fechamento do M1. Stop-loss está `StopLossPips` abaixo do preço limite, take-profit está `TakeProfitPips` acima dele. Quando o D1 RAVI mostra uma escada ascendente entre -1 e +5 nos últimos quatro dias, o take-profit recebe uma extensão extra de 25 pips.
- **Limite de venda:** 7 pips acima do último fechamento do M1 com regras de stop e meta simétricas. Quando o D1 RAVI mostra uma escada em queda entre -5 e -1, o alvo é estendido em 25 pips.
- Os pedidos pendentes expiram automaticamente após `OrderExpiryMinutes` (padrão 20 minutos). Quando um pedido é cancelado, os níveis de proteção armazenados são redefinidos.

## Gerenciamento de posição
- Uma vez preenchida, a estratégia mantém os valores de stop-loss e take-profit armazenados internamente e emite ordens de saída do mercado quando o preço atinge qualquer um dos níveis.
- Nenhum trailing stop é aplicado na versão portada; o EA original desativou a lógica final por padrão.
- Novas negociações são bloqueadas enquanto existir uma posição ativa ou ordem com limite pendente.

## Gestão de capital
- Quando `UseMoneyManagement` está ativado, o tamanho do lote imita a implementação MT4: ele aumenta em `TradeSizePercent` do patrimônio atual, ajusta para mini contas e fixa o resultado para `[0.1, MaxVolume]` (mini) ou `[1, MaxVolume]` (padrão).
- Desativar o gerenciamento de dinheiro força um volume fixo controlado pelo parâmetro `FixedVolume`.
- A negociação é interrompida quando o patrimônio do portfólio cai abaixo de `MarginCutoff`.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `AccountIsMini` | Use regras de arredondamento de volume de minicontas | `true` |
| `UseMoneyManagement` | Ativar dimensionamento de lote adaptável | `true` |
| `TradeSizePercent` | Percentual de patrimônio alocado por negociação | `10` |
| `FixedVolume` | Volume usado quando o gerenciamento de dinheiro está desativado | `0.01` |
| `MaxVolume` | Volume máximo de negociação permitido | `4` |
| `StopLossPips` | Distância de stop-loss em pips | `210` |
| `TakeProfitPips` | Distância de lucro em pips | `18` |
| `PointFromLevelGoPips` | Mudança de nível base em 0,1 pips | `50` |
| `RiseFilterPips` | Distância de confirmação de pico por hora (0,1 pips) | `700` |
| `HighLevelPips` | Limite de rejeição de pico de um minuto (0,1 pips) | `600` |
| `LowLevelPips` | Banda de consolidação de 30 minutos (0,1 pips) | `250` |
| `LowLevel2Pips` | Distância de confirmação de rompimento por hora (0,1 pips) | `450` |
| `MarginCutoff` | Piso de capital desabilitando novas negociações | `300` |
| `OrderExpiryMinutes` | Vida útil do pedido pendente em minutos | `20` |

## Notas de uso
- A conversão depende de velas finalizadas de cada período; garanta que o fluxo de dados históricos forneça velas M1, M30, H1 e D1 sincronizadas.
- O stop e o alvo de proteção são executados com ordens de mercado para espelhar o comportamento MT4 dos valores SL/TP anexados.
- Como a lógica é sensível ao tamanho do pip, verifique se as propriedades `PriceStep` e `Decimals` do instrumento descrevem corretamente o formato de cotação.
