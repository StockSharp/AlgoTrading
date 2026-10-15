# Estratégia de Absorção
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o expert advisor Absorption para MetaTrader. Procura velas "engulfing" que absorvem o range da barra anterior e formam um extremo dentro de um curto período de pesquisa. Quando tal barra de absorção aparece, o algoritmo posiciona ordens stop dos dois lados do mercado e gerencia a posição resultante com uma combinação de objetivos fixos, lógica de breakeven e um trailing stop.

## Lógica de Trading

1. **Detecção de padrão**
   - As últimas duas velas completadas são inspecionadas.
   - Uma vela é tratada como *barra de absorção* quando sua máxima está acima da máxima da vela anterior e sua mínima está abaixo da mínima da vela anterior.
   - A barra é validada verificando se sua máxima ou mínima é o valor mais extremo dentro das últimas `MaxSearch` velas.
   - Prioridade é dada à vela mais antiga (duas barras atrás). Se ambas as barras satisfazem a condição de absorção, a barra mais antiga é usada; caso contrário, a barra mais recente pode acionar a configuração.
2. **Colocação de ordens**
   - Uma ordem de compra stop é colocada na máxima da barra mais o `Indent` configurado.
   - Uma ordem de venda stop é colocada na mínima da barra menos o mesmo `Indent`.
   - Ambas as ordens usam o volume de estratégia comum.
   - Cada ordem pendente armazena seu próprio nível de stop protetor e objetivo de take profit opcional. As ordens expiram automaticamente após `OrderExpirationHours` se permanecerem não preenchidas.
3. **Gestão de posições**
   - Quando um lado é preenchido, a ordem pendente oposta é cancelada.
   - O stop inicial está localizado no lado oposto da vela de absorção menos/mais o indent.
   - Um take profit fixo opcional fecha a operação assim que a distância configurada em passos de preço é alcançada.
   - O módulo de breakeven move o stop-loss para `Entrada + Breakeven` (comprado) ou `Entrada - Breakeven` (vendido) após o preço avançar `BreakevenProfit` passos.
   - O trailing stop mantém o stop-loss a distância `TrailingStop` do melhor preço, atualizando apenas quando o preço se move pelo menos `TrailingStep` passos na direção rentável.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Tipo de dados de vela para assinar (padrão: período de 1 hora). |
| `MaxSearch` | Número de velas recentes usadas para confirmar extremos de máxima/mínima. |
| `TakeProfitBuy` | Distância em passos de preço para a ordem de take profit comprado. `0` desabilita o objetivo. |
| `TakeProfitSell` | Distância em passos de preço para a ordem de take profit vendido. `0` desabilita o objetivo. |
| `TrailingStop` | Distância do trailing stop em passos de preço. `0` desabilita o trailing. |
| `TrailingStep` | Movimento mínimo para frente necessário antes de avançar o trailing stop. Deve ser positivo quando o trailing está habilitado. |
| `Indent` | Deslocamento em passos de preço adicionado acima/abaixo da barra de absorção para definir os níveis de entrada stop. |
| `OrderExpirationHours` | Tempo de vida de ordens pendentes. Após este período as ordens são canceladas se não acionadas. |
| `Breakeven` | Deslocamento aplicado ao stop-loss quando a regra de breakeven é acionada. `0` desabilita o breakeven. |
| `BreakevenProfit` | Limiar de lucro (em passos de preço) que deve ser alcançado antes de mover o stop-loss para breakeven. |

Todas as entradas baseadas em distância são expressas como múltiplos do passo de preço do instrumento. O volume de estratégia padrão está definido em `0.1`.

## Gerenciamento de Risco

A estratégia usa apenas ordens de mercado para saídas. As regras de stop-loss, take-profit, breakeven e trailing monitoram as máximas e mínimas das velas para detectar toques de nível dentro da barra. Uma vez que uma ordem de saída é enviada, nenhuma solicitação de saída adicional é gerada até que a posição atual esteja flat.
