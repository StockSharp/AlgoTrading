# Estratégia FXF Fast in Fast out
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **FXF Fast in Fast out** é um sistema de breakout orientado à volatilidade que converte o consultor especialista MetaTrader 4 original em uma estratégia de alto nível StockSharp. Ele observa um período configurável para velas grandes, mede o spread e reage colocando ordens de stop pendentes que tentam capturar a continuação imediata do impulso. A lógica usa apenas velas finalizadas para geração de sinal, enquanto cotações (dados de Nível 1) são usadas para filtros de spread, colocação de pedidos e gerenciamento de trailing stop.

Quando a vela atual se expande além do limite de volatilidade, a estratégia avalia o preço médio em relação à vela aberta. Se o preço médio fechar acima da abertura, um stop de compra é colocado acima da melhor venda; se fechar abaixo, um stop de venda é colocado sob a melhor oferta. Os níveis protetores de stop-loss e take-profit são anexados às ordens pendentes, e a lógica de rastreamento opcional protege as posições abertas assim que elas são preenchidas. A gestão de dinheiro pode dimensionar pedidos dinamicamente com base no valor do portfólio e na distância de parada.

## Lógica de negociação
- **Detecção de sinal** – Em cada vela finalizada, a estratégia verifica se o intervalo da vela expresso em etapas de preço excede `VolatilitySizePoints`. Se o intervalo for grande o suficiente, ele calcula o preço médio usando o melhor instantâneo de compra/venda mais recente.
- **Viés direcional** – Um preço médio acima da abertura da vela produz um viés de alta (ordem stop de compra), enquanto um preço médio abaixo da abertura produz um viés de baixa (ordem stop de venda). Nenhuma ordem será colocada se o preço médio for igual ao de abertura ou se o requisito de volatilidade não for atendido.
- **Filtro de spread** – As cotações são monitoradas continuamente. As ordens pendentes são criadas apenas quando o spread atual está abaixo de `MaxSpreadPoints`. Se o spread aumentar além desse limite, quaisquer ordens pendentes existentes serão canceladas até que o spread retorne a níveis aceitáveis.
- **Gerenciamento de ordens pendentes** – Apenas uma ordem pendente pode estar ativa por barra. Cada pedido é compensado da melhor cotação em `EnterOffsetPoints`. As distâncias de stop-loss e take-profit são definidas em pontos e automaticamente convertidas em preços.
- **Controle de risco** – Com `UseMoneyManagement` ativado, o volume do pedido é dimensionado a partir do valor do portfólio, porcentagem de risco e distância de stop-loss usando o preço escalonado do instrumento. Caso contrário, a propriedade padrão `Volume` será usada.
- **Trailing stop** – Quando `EnableTrailing` é verdadeiro, a estratégia mantém um trailing stop interno para a posição ativa com base em `TrailingStopPoints` mais o spread atual. Se o preço de mercado ultrapassar o trailing stop, a posição será fechada no mercado.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `EnterOffsetPoints` | Distância em etapas de preço entre a melhor cotação e o preço da ordem stop pendente. |
| `MaxSpreadPoints` | Spread máximo permitido (em etapas de preço). Spread acima deste limite bloqueia novas entradas e cancela ordens pendentes ativas. |
| `TakeProfitPoints` | Distância de take-profit em etapas de preço aplicadas a ordens pendentes. Defina como zero para pular a colocação com fins lucrativos. |
| `StopLossPoints` | Distância de stop-loss em etapas de preço. Necessário para dimensionamento de gerenciamento de dinheiro. Defina como zero para desativar a colocação de stop loss. |
| `VolatilitySizePoints` | Faixa mínima de vela (em etapas de preço) necessária para gerar um novo sinal de rompimento. |
| `EnableTrailing` | Ativa ou desativa a lógica de trailing stop para posições abertas. |
| `TrailingStopPoints` | Distância final básica em etapas de preço. O nível final real também inclui o spread atual para imitar o comportamento original EA. |
| `UseMoneyManagement` | Permite o dimensionamento de posição baseado em portfólio usando o valor `RiskPercent`. |
| `RiskPercent` | Percentagem de risco por negociação utilizada quando a gestão de dinheiro está ativa. |
| `MaxOrdersPerBar` | Número máximo de ordens pendentes permitidas durante uma única barra. Normalmente definido como 1 para espelhar o consultor especialista original. |
| `CandleType` | O período de velas usado para cálculos de sinal. O padrão é 15 minutos. |

## Fluxo de trabalho do pedido
1. **Detecção** – Uma vela finalizada que atenda ao critério de volatilidade define a direção de negociação desejada.
2. **Validação** – As cotações devem estar disponíveis, a negociação deve ser permitida, não deve existir nenhuma posição aberta e nenhuma outra ordem ativa deve estar presente.
3. **Colocação** – A estratégia coloca um stop de compra ou um stop de venda com a compensação calculada, anexando níveis de stop-loss e take-profit.
4. **Trailing e Exit** – Depois que um pedido é atendido, o módulo de rastreamento monitora as cotações mais recentes. Quebrar o nível final fecha a posição com uma ordem de mercado. As ordens take-profit e stop-loss permanecem anexadas à posição para execução automática pela corretora ou simulador.

## Notas
- A estratégia requer assinaturas de dados de vela e de nível 1 para funcionar corretamente.
- O dimensionamento baseado em risco volta ao `Volume` configurado se os parâmetros de stop-loss ou metadados de segurança (etapa de preço ou preço escalonado) não estiverem disponíveis.
- Os trailing stops são gerenciados internamente por meio de saídas de mercado para corresponder ao comportamento MetaTrader, garantindo compatibilidade entre diferentes locais de execução.
