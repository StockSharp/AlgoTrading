# Estratégia de ruptura Expert610
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Expert610 Breakout é uma porta C# do MetaTrader 4 consultor especialista `Expert610.mq4`. O robô original espera por um
vela larga e, em seguida, estaciona uma ordem de compra e uma ordem de venda em torno da barra anterior. O tamanho da posição é derivado do
percentagem de capital livre que o trader está disposto a arriscar, e as distâncias stop-loss/take-profit são expressas em pips. Isto
A versão StockSharp espelha esse comportamento usando o API de alto nível enquanto expõe cada botão de ajuste como um parâmetro de estratégia.

## Lógica de negociação
1. **Coleta de dados**
   - A estratégia assina um tipo de vela configurável e armazena a barra finalizada mais recente.
   - As atualizações do livro de pedidos são monitoradas para estimar o spread atual de compra/venda. Quando não há profundidade disponível, a contribuição do spread
o padrão é zero, reproduzindo o comportamento original EA em corretoras sem spreads ao vivo.
2. **Filtro de volatilidade**
   - A máxima da vela anterior menos o fechamento atual e o fechamento atual menos a mínima anterior devem ambos exceder
`ThresholdPips` (convertido em unidades de preço absoluto).
   - A vela aberta atual deve estar estritamente abaixo da máxima anterior para permitir uma configuração de compra e estritamente acima da mínima anterior para
permitir uma configuração de venda. Quando ambas as condições são mantidas, o algoritmo organiza ordens pendentes simétricas.
3. **Colocação de pedido**
   - As paradas de compra são colocadas em `previous high + BreakoutOffset + spread`, correspondendo ao código MT4 onde o preço de venda é usado.
   - As paradas de venda são colocadas em `previous low - BreakoutOffset`, também permanecendo fiéis ao script original que ignora o
spread no lado da oferta.
   - Apenas um par de ordens pendentes pode estar ativo a qualquer momento. Se um pedido já estiver funcionando, os novos sinais serão ignorados.
4. **Gerenciamento de riscos**
   - O tamanho do lote é derivado do capital livre (`Portfolio.CurrentValue - Portfolio.BlockedValue`) multiplicado por
`RiskPercent / 100`. O valor é arredondado para `RoundingDigits` e convertido em lotes usando a mesma heurística do MT4
código: `lot = risk / stopPips * 0.1`, que assume que um pip de um lote de 0,1 é igual a uma unidade de moeda da conta.
   - O lote computado é alinhado aos limites de troca e ao parâmetro `MinimumVolume` antes de ser enviado ao local.
   - `StartProtection` anexa limites e metas baseados em preço a cada posição resultante, para que os preenchimentos recebam imediatamente o
deslocamentos `StopLossPips` e `TakeProfitPips` configurados.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `RoundingDigits` | Casas decimais usadas ao arredondar cálculos de risco e volume. | `2` | Deve ser não negativo. |
| `RiskPercent` | Percentual de capital livre arriscado em cada entrada. | `1` | Defina como `0` para desativar o dimensionamento dinâmico e voltar para `MinimumVolume`. |
| `MinimumVolume` | Limite inferior rígido para o volume de ordens pendentes. | `0.1` | Também respeita os `MinVolume` e `VolumeStep` de segurança. |
| `ThresholdPips` | Distância mínima do último próximo aos extremos da vela anterior. | `5` | Medido em pips e convertido com o tamanho de pip detectado. |
| `BreakoutOffsetPips` | Buffer adicionado além da máxima/mínima anterior ao preparar pedidos. | `2` | Aplicado simetricamente em ambos os lados. |
| `StopLossPips` | Distância stop-loss associada a pedidos atendidos. | `5` | Expressado em pips e enviado para `StartProtection`. |
| `TakeProfitPips` | Distância de lucro associada a pedidos atendidos. | `10` | Expresso em pips; definido como `0` para desativar o alvo. |
| `CandleType` | Série de velas usada para avaliar o rompimento. | `1 hour` período de tempo | Aceita qualquer `DataType` compatível com StockSharp. |

## Notas de implementação
- O tamanho do pip é derivado do `PriceStep` e `Decimals` do instrumento (símbolos Forex de 5 e 3 dígitos recebem um ×10
ajuste) para manter a conversão idêntica à fórmula MQL4.
- O arredondamento do tamanho do pedido honra `VolumeStep`, limita `MinVolume`/`MaxVolume` e, finalmente, impõe o nível de estratégia
`MinimumVolume` para que as solicitações resultantes sejam sempre negociáveis.
- A compensação de spread utiliza a melhor oferta/venda extraída da carteira de ofertas subscrita. Isso produz o mesmo preço de entrada que o
Implementação MT4 quando a plataforma fornece spreads ao vivo e, caso contrário, degrada normalmente.
- Os pedidos pendentes são eliminados do estado interno assim que StockSharp os reporta como atendidos, cancelados ou com falha, permitindo que o
lógica para enviar novos pedidos na próxima vela qualificada.

## Diferenças versus a versão MQL
- O EA original arredondou o risco e o volume usando `Digits2Round`. A porta mantém esse recurso, mas também alinha o
resultado para etapas de volume específicas da troca.
- Em vez de anexar preços de proteção diretamente às ordens pendentes, a estratégia StockSharp depende de `StartProtection` então
cada posição preenchida recebe automaticamente ordens de stop-loss e take-profit.
- As informações do portfólio substituem as funções MT4 `AccountBalance()` e `AccountMargin()` para obter capital livre; se esses dados
não está disponível, a estratégia volta normalmente para o dimensionamento `MinimumVolume`.
- Todos os cálculos operam apenas em velas finalizadas, evitando a repintura intra-barra e combinando o loop baseado em ticks `start()`
assim que o bar fechar.
