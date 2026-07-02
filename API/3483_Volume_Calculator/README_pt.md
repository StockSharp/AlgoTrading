# Estratégia de calculadora de volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia da Calculadora de Volume** reproduz a lógica do consultor especialista original MetaTrader que calcula um volume de negociação recomendado com base nos níveis de stop-loss e take-profit. Quando a estratégia é iniciada, ela lê os preços stop configurados, avalia o preço de mercado atual do título selecionado e deriva as métricas de risco usando o capital disponível do portfólio.

A estratégia não faz pedidos. Seu único objetivo é fornecer estatísticas detalhadas de gerenciamento de dinheiro no log e expor os valores computados por meio de propriedades somente leitura. Isto o torna útil para traders manuais que desejam validar as regras de dimensionamento de posição antes de enviar uma negociação.

## Parâmetros
- **Preço de Stop Loss** – nível de preço absoluto do stop de proteção utilizado para a posição planejada.
- **Take Profit Price** – nível de preço absoluto da meta de take-profit.
- **Perda Máxima%** – parcela máxima do valor do portfólio que pode ser arriscada em uma única negociação. A estratégia multiplica esta percentagem pelo capital da carteira para obter a perda máxima aceitável em termos cambiais.
- **É posição longa** – determina se a posição planejada é longa (`true`) ou curta (`false`). A direção é necessária para calcular a distância entre o preço atual e os níveis de stop/alvo.

Todos os parâmetros, exceto *Max Loss %*, são excluídos da otimização para mantê-los como entradas estritamente manuais, refletindo o comportamento do especialista original.

## Detalhes do cálculo
1. **Valor do portfólio** – a estratégia busca `Portfolio.CurrentValue` (voltando para `Portfolio.BeginValue`) para estimar o capital disponível. Se o valor não for fornecido, o cálculo será interrompido com um aviso.
2. **Validação da etapa de preço** – os valores `Security.PriceStep` e `Security.StepPrice` devem ser definidos porque convertem distâncias de preço em etapas de contrato e valores em dinheiro. A falta de metadados impede o cálculo.
3. **Detecção de preço atual** – a estratégia procura o último preço de negociação. Quando indisponível, aproxima o preço calculando a média das melhores cotações de compra/venda, voltando finalmente ao último preço conhecido.
4. **Distância em etapas** – tanto as distâncias de stop-loss quanto de take-profit são medidas em etapas de preço. As distâncias são arredondadas (`decimal.Ceiling`) para permanecerem conservadoras, da mesma forma que o script MetaTrader depende de `MathCeil`.
5. **Dinheiro em risco** – a perda máxima aceitável é igual a `PortfolioValue * MaxLoss% / 100`.
6. **Volume sugerido** – a perda por etapa é de `MaxLoss / StopSteps`. A divisão deste valor por `StepPrice` produz o volume da posição que mantém a perda sob controle.
7. **Lucro esperado** – multiplicar as etapas de obtenção de lucro por `StepPrice` e o volume sugerido produz o ganho de caixa projetado se a meta for atingida.
8. **Relação risco-recompensa** – relação entre as contagens de etapas de take-profit e stop-loss, equivalente ao cálculo original baseado em pip.

Cada valor calculado é armazenado dentro da estratégia e impresso no log com mensagens informativas em inglês. Se a relação risco-recompensa for maior ou igual a 3, a estratégia sinaliza “Você pode negociar”; caso contrário, imprime um aviso de que a negociação é muito arriscada.

## Fluxo de trabalho de uso
1. Anexe a estratégia à segurança e ao portfólio desejados no ambiente StockSharp.
2. Configure os preços de stop-loss e take-profit que correspondam à negociação manual planejada.
3. Defina a porcentagem de risco aceitável e a direção pretendida.
4. Inicie a estratégia – a saída com todas as métricas aparecerá imediatamente no log.
5. Revise o volume sugerido e a relação risco-recompensa antes de executar a negociação manualmente.

## Notas
- Se algum dos campos de metadados de segurança obrigatórios (etapa de preço ou preço escalonado) estiver faltando, solicite-os à bolsa ou ajuste as configurações de segurança manualmente.
- O cálculo é estático; ele não é atualizado automaticamente após o início. Reinicie a estratégia se as condições de mercado ou os parâmetros de risco mudarem.
- Como a estratégia não envia ordens, é seguro executá-la tanto em backtesting quanto em ambientes ao vivo, exclusivamente para fins analíticos.
