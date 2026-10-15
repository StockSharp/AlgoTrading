# Hedger EES (avançado)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia reflete o comportamento do clássico consultor especialista MetaTrader "EES Hedger". Sempre que um trader externo, operador discricionário ou outro sistema automatizado abre uma posição na mesma conta, a estratégia cria imediatamente uma cobertura oposta utilizando um volume configurável. Em seguida, ele gerencia o hedge com regras de stop-loss, take-profit, ponto de equilíbrio e trailing stop, para que a exposição seja neutralizada enquanto os lucros do hedge são protegidos.

Ao contrário das estratégias tradicionais baseadas em sinais, este módulo assume que as entradas são produzidas em outro lugar. Sua única responsabilidade é observar as negociações da conta, reagir aos tickets correspondentes e proteger a posição de hedge até que ela seja fechada pelas ordens de proteção ou manualmente.

## Lógica de negociação

1. **Detecção de negociações externas** – o fluxo do conector de negociações da conta é monitorado. As negociações cujo comentário corresponde a `OriginalOrderComment` (ou todas as negociações quando o campo está vazio) são tratadas como a fonte que deve ser protegida. As negociações produzidas pela própria estratégia são filtradas armazenando seus identificadores de transação.
2. **Ordens espelhadas** – assim que uma negociação qualificada é recebida, a estratégia envia uma ordem de mercado imediata na direção oposta com volume `HedgeVolume`. Um `HedgerOrderComment` opcional ajuda as ferramentas de back-office a separar as ordens de hedge de outras atividades.
3. **Gerenciamento de risco** – após o preenchimento do hedge a estratégia coloca ordens de stop-loss e take-profit em distâncias definidas pelos parâmetros pip. Quando as condições de equilíbrio são atendidas, o stop é movido para o preço de entrada mais um pip. Se o trailing estiver habilitado, o stop avança ainda mais à medida que o mercado continua a se mover a favor do hedge.
4. **Limpeza de estado** – quando a posição chega a zero (por exemplo, após um fechamento manual) todas as ordens de proteção são canceladas e os sinalizadores internos são redefinidos para que a próxima negociação externa possa ser protegida do zero.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `HedgeVolume` | Volume utilizado para abrir a posição de hedge oposta. |
| `StopLossPips` | Distância do preço de entrada até a ordem de stop loss de proteção. |
| `TakeProfitPips` | Distância do preço de entrada até a ordem de take-profit. |
| `TrailingStopPips` | Distância mantida pelo trailing stop quando o limite de ativação é excedido. Defina como zero para desativar o rastreamento. |
| `TrailingActivationPips` | Lucro mínimo (em pips) necessário antes que o trailing stop comece a se mover. |
| `BreakEvenPips` | Limite de lucro (em pips) após o qual o stop loss é movido para o preço de entrada mais um pip. |
| `OriginalOrderComment` | Filtro de comentários opcional que seleciona quais negociações externas devem ser protegidas. Deixe em branco para proteger todas as negociações do instrumento. |
| `HedgerOrderComment` | Comentário anexado às ordens de hedge e stops de proteção gerados pela estratégia. |

## Notas práticas

- Atribua à estratégia a mesma carteira/conta que o trader externo. Todas as posições criadas nessa conta ficarão visíveis para o conector e poderão, portanto, ser cobertas.
- Quando usado com pontes MetaTrader, configure o consultor especialista ou ponte para copiar o comentário do pedido original para que a filtragem funcione conforme o esperado.
- O tamanho do pip é derivado da etapa de preço do instrumento. Para símbolos FX de cinco dígitos, a distância traduz automaticamente os valores de pip especificados em compensações de preço corretas.
- A lógica do ponto de equilíbrio e do trailing nunca afasta o stop do preço de entrada. Apenas melhorias são aplicadas, garantindo que, uma vez atingido o ponto de equilíbrio, o stop nunca volte a um nível deficitário.
- A estratégia não gerencia a posição original. Fechar ou modificá-lo continua sendo responsabilidade do sistema de negociação principal.

## Fluxo de trabalho de uso

1. Configure os parâmetros da estratégia, prestando especial atenção aos filtros de comentários e ao volume do hedge.
2. Inicie a estratégia e confirme se ela está conectada ao feed da corretora. Permanecerá ocioso até que chegue um comércio externo.
3. Assim que aparecer uma negociação qualificada, observe como a ordem de hedge é criada e como as ordens de proteção são colocadas no DOM.
4. Monitore o ponto de equilíbrio e o comportamento de trilha para garantir que as distâncias de pip configuradas correspondam às especificações do contrato do corretor.
5. Pare a estratégia quando o hedge não for mais necessário. Todas as ordens de proteção em funcionamento são canceladas durante o desligamento.

## Limitações

- O módulo pressupõe acesso ao fluxo de negociação da conta. Ele não pode proteger negociações que sejam completamente invisíveis para o conector.
- As regras de arredondamento de volume são específicas da corretora. Certifique-se de que o `HedgeVolume` configurado seja compatível com a etapa do lote do instrumento.
- Como a estratégia coloca ordens de mercado imediatamente, a derrapagem em mercados rápidos pode resultar em coberturas imperfeitas. Aumente as distâncias de stop-loss para compensar isso quando necessário.
