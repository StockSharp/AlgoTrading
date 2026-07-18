# Estratégia do bot KA-Gold
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia KA-Gold Bot** é uma conversão StockSharp de alto nível do consultor especialista original MetaTrader 4 "KA-Gold Bot". Ele combina um canal estilo Keltner com filtros de tendência e gerenciamento de risco agressivo que inclui stop-loss fixo, take-profit e proteção de rastreamento em vários estágios. A negociação é permitida apenas durante uma janela intradiária configurável e novas posições são bloqueadas quando o spread ao vivo excede um limite.

## Lógica de negociação

1. **Preparação de indicadores**
   - Uma média móvel exponencial (EMA) com comprimento `KeltnerPeriod` constrói a linha média do canal.
   - Uma média móvel simples dos intervalos das velas (máxima menos mínima) com o mesmo período estima a meia largura do canal.
   - As médias móveis exponenciais de curto e longo prazo (`EmaShortPeriod` e `EmaLongPeriod`) acompanham o impulso rápido e a tendência de período de tempo mais alto, respectivamente.
   - Todos os valores dos indicadores são registrados para as duas velas concluídas mais recentemente para espelhar os cálculos baseados em turnos do MT4.

2. **Condições de entrada**
   - Os cálculos são executados somente quando a vela atual fecha e a estratégia está conectada ao mercado com permissões de negociação concedidas.
   - As bandas superior e inferior do canal são derivadas adicionando/subtraindo o intervalo médio da linha média EMA para a vela anterior (`shift = 1`) e a anterior (`shift = 2`).
   - **Configuração longa:**
     - O fechamento anterior ultrapassa a banda superior mais recente.
     - O mesmo fechamento está acima da compra EMA, confirmando uma tendência de alta.
     - O EMA curto cruza abaixo da faixa superior mais antiga para acima da mais recente (`EMA_short[2] < Upper[2]` e `EMA_short[1] > Upper[1]`).
   - **Configuração curta:**
     - O fechamento anterior fica abaixo da banda inferior recente.
     - O mesmo fechamento está abaixo da compra EMA, confirmando uma tendência de baixa.
     - O EMA curto cruza acima da banda inferior mais antiga para abaixo da mais recente (`EMA_short[2] > Lower[2]` e `EMA_short[1] < Lower[1]`).
   - Apenas uma posição é permitida por vez. Se uma negociação já estiver aberta, o sinal será ignorado.

3. **Filtros de tempo e propagação**
   - Quando `UseTimeFilter` está ativado, novas entradas são restritas à janela `[StartHour:StartMinute, EndHour:EndMinute)` usando o horário local do Exchange. Sessões noturnas serão suportadas se o horário de término for anterior ao horário de início.
   - As assinaturas de cotação de nível 1 acompanham os melhores preços de compra/venda. Antes de fazer um pedido, a estratégia converte o spread atual em pontos de instrumento e o compara com `MaxSpreadPoints`. Os pedidos são ignorados, com registro, sempre que o limite é violado.

4. **Gerenciamento de riscos**
   - O tamanho padrão da posição é `FixedVolume`. Se `UseRiskPercent` for `true`, o tamanho da negociação será recalculado a partir do patrimônio do portfólio como `RiskPercent% / (riskPips * PipValue)`, onde `riskPips` é igual a `StopLossPips` (substituição para `TrailingStopPips` quando nenhum stop fixo for definido). O resultado final é normalizado para o passo de volume do instrumento e fixado entre os limites de troca mínimo e máximo.
   - Quando uma posição longa é aberta, a estratégia armazena:
     - Stop-loss inicial em `entry - StopLossPips * pipSize` (se definido).
     - Take-profit inicial em `entry + TakeProfitPips * pipSize` (se definido).
     - Sinalizadores de estado final, que redefinem os rastreadores do lado curto.
   - As negociações curtas refletem a mesma lógica com direções de preços invertidas.

5. **Proteção de rastreamento**
   - As atualizações de oferta/venda ao vivo alimentam dois mecanismos finais:
     - Assim que o lucro flutuante exceder `TrailingTriggerPips`, o trailing se torna ativo.
     - O trailing stop está posicionado a `TrailingStopPips` de distância do preço favorável atual e só avança quando o movimento excede `TrailingStopPips + TrailingStepPips` além do nível de stop anterior.
     - Para posições longas, o trailing stop nunca cai abaixo do stop de proteção original e, para posições curtas, nunca sobe acima dele.
   - O monitoramento de saída é realizado tanto nas cotações recebidas quanto nas velas finalizadas:
     - Uma posição é fechada imediatamente quando o preço atinge o stop ativo (original ou móvel).
     - Os lucros também são bloqueados quando a máxima/mínima da vela atinge o nível de lucro armazenado.
   - Após fechar uma posição, o estado de proteção é totalmente redefinido para evitar dados obsoletos.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Tipo de dados que descreve o prazo de execução. | Período de 1 minuto |
| `KeltnerPeriod` | Período para a linha média EMA e a média do intervalo do canal. | 50 |
| `EmaShortPeriod` | Comprimento EMA rápido usado para confirmação de cruzamento. | 10 |
| `EmaLongPeriod` | Comprimento EMA lento atuando como filtro de tendência. | 200 |
| `FixedVolume` | Volume do pedido substituto quando o dimensionamento percentual está desativado. | 1 |
| `UseRiskPercent` | Ative o dimensionamento de posição com base em porcentagem. | `true` |
| `RiskPercent` | Porcentagem de patrimônio arriscado por negociação. | 1 |
| `StopLossPips` | Distância do stop loss fixo em pips (0 desabilita). | 500 |
| `TakeProfitPips` | Distância do take-profit fixo em pips (0 desabilita). | 500 |
| `TrailingTriggerPips` | Lucro em pips necessário para ativar o trailing stop. | 300 |
| `TrailingStopPips` | Distância entre o preço e o trailing stop, uma vez ativo. | 300 |
| `TrailingStepPips` | Lucro adicional mínimo (em pips) antes do trailing stop ser avançado. | 100 |
| `UseTimeFilter` | Alterne para o filtro da sessão de negociação. | `true` |
| `StartHour` / `StartMinute` | A sessão começa no horário local do Exchange. | 02:30 |
| `EndHour` / `EndMinute` | A sessão termina no horário local do Exchange. | 21:00 |
| `MaxSpreadPoints` | Spread máximo permitido nos pontos do instrumento (0 desativa a verificação). | 65 |
| `PipValue` | Valor monetário de um pip, utilizado para dimensionamento de posições com base no risco. | 1 |

## Notas adicionais

- A conversão do pip segue os decimais do instrumento de câmbio: uma cotação de cinco dígitos (número ímpar de decimais) multiplica o passo do preço por 10 para emular a lógica do tamanho do pip MT4.
- A estratégia assina velas e dados de nível 1, mas **não** registra indicadores adicionais no gráfico, em conformidade com as diretrizes de alto nível API.
- As saídas protetoras dependem de ordens de mercado emitidas pela estratégia; nenhuma ordem stop ou limite separada é colocada na bolsa.
- O suporte Python não está incluído nesta entrega, correspondendo à solicitação original.
