# ABH_BH_MFI Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **ABH_BH_MFI Estratégia** é uma versão StockSharp de alto nível do MetaTrader consultor especialista "Expert_ABH_BH_MFI". O algoritmo combina padrões de velas Harami de alta e baixa com confirmação do Money Flow Index (MFI). As negociações longas são acionadas quando um Harami altista se forma dentro de um mercado em queda, enquanto a IMF permanece deprimida. As negociações a descoberto exigem um Harami baixista dentro de um mercado em ascensão e uma IMF elevada. A implementação original do MQL dependia da infraestrutura de sinal do MetaTrader; esta conversão mantém a lógica de decisão, mas a expressa com assinaturas de velas, vinculação de indicadores e auxiliares de gerenciamento de posição de StockSharp.

## Lógica de negociação
### 1. Detecção de padrão Harami
- A estratégia armazena as duas velas concluídas mais recentemente.
- Um **Harami otimista** requer:
  - Duas velas atrás havia uma longa vela preta (de baixa) cujo corpo é maior que o comprimento médio do corpo.
  - A vela mais recente é de alta e sua abertura/fechamento é engolfada pelo corpo da vela de baixa anterior.
  - O ponto médio da vela mais antiga está abaixo da média móvel simples de fechamentos, sinalizando uma tendência de baixa predominante.
- Um **Harami baixista** reflete esses requisitos com cores invertidas e o ponto médio acima da média móvel para confirmar uma tendência de alta.

### 2. Confirmação do Índice de Fluxo de Dinheiro
- A MFI usa o configurável `MfiPeriod` (padrão **37**) para replicar as configurações originais do oscilador.
- Entradas longas exigem que o último valor da IMF concluída permaneça abaixo de `BullishThreshold` (padrão **40**) para garantir o esgotamento do fluxo de capital.
- As entradas curtas exigem que a IMF permaneça acima de `BearishThreshold` (padrão **60**) para mostrar o esgotamento da pressão de compra.

### 3. Regras de saída através de cruzamentos de IFM
- As posições longas ativas são fechadas quando a IMF ultrapassa `ExitLowerLevel` (padrão **30**) ou `ExitUpperLevel` (padrão **70**), correspondendo às condições MetaTrader `MFI(1) > level && MFI(2) < level`.
- As posições curtas ativas são fechadas quando a IMF desce da zona de sobrecompra ou sobe abaixo do nível de sobrevenda, refletindo as cláusulas de saída curtas originais.

### 4. Gestão de riscos
- A estratégia aplica-se opcionalmente `StartProtection` com compensações de stop-loss e take-profit expressas em etapas de preço. Definir o parâmetro correspondente como zero desativa a distância de proteção, reproduzindo os padrões MetaTrader.
- O dimensionamento da posição usa a propriedade base `Volume`; a reversão de posições adiciona automaticamente contratos suficientes para estabilizar e reabrir na nova direção, assim como o especialista fonte.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Período de 1 hora | Série de velas primárias analisadas para padrões e IMF. |
| `MfiPeriod` | 37 | Lookback para o indicador Money Flow Index. |
| `BodyAveragePeriod` | 11 | Comprimento das médias móveis simples que medem o tamanho do corpo e a tendência de fechamento. |
| `BullishThreshold` | 40 | Valor máximo da IMF permitido antes de abrir negociações longas. |
| `BearishThreshold` | 60 | Valor mínimo da IMF exigido antes de abrir negociações curtas. |
| `ExitLowerLevel` | 30 | Nível de cruzamento mais baixo da MFI para saídas de posição. |
| `ExitUpperLevel` | 70 | Nível superior de cruzamento da MFI para saídas de posição. |
| `StopLossPoints` | 0 | Distância de stop-loss opcional em etapas de preço (0 desativa). |
| `TakeProfitPoints` | 0 | Distância opcional de take-profit em etapas de preço (0 desativa). |

## Notas de implementação
- Os dados da vela são recebidos via `SubscribeCandles(CandleType)` e processados somente quando o estado da vela é `Finished`, garantindo o alinhamento com a lógica de barra fechada do especialista MQL.
- O indicador MFI é vinculado diretamente a `.Bind(_mfi, ProcessCandle)` para que o manipulador receba valores decimais prontos para uso sem chamar `GetValue`.
- Duas médias móveis simples auxiliares replicam as funções auxiliares `AvgBody` e `CloseAvg` do código MetaTrader. Seus resultados são armazenados em cache para evitar consultas de indicadores históricos.
- As decisões de saída e entrada chamam `IsFormedAndOnlineAndAllowTrading()` antes de enviar ordens, permanecendo consistentes com as verificações de segurança comercial recomendadas por StockSharp.

## Diferenças do especialista MetaTrader
- A gestão do dinheiro é simplificada para o volume da estratégia básica. O módulo original de "lote fixo" traduzido para o auxiliar de dimensionamento de posição de StockSharp, que cobre a mesma funcionalidade sem classes separadas.
- O componente de parada móvel MetaTrader (`TrailingNone`) não tinha lógica; a versão StockSharp, portanto, omite quaisquer ações finais, mas mantém metas de risco fixas opcionais.
- O registro em log é mínimo por padrão; você pode estendê-lo com chamadas `LogInfo` se precisar de diagnósticos comerciais detalhados.

## Dicas de uso
1. Configure a segurança desejada e atribua o `CandleType` antes de iniciar a estratégia.
2. Opcionalmente, ajuste os limites da IMF e de saída para se adequar aos diferentes regimes de volatilidade.
3. Forneça `StopLossPoints`/`TakeProfitPoints` diferente de zero quando o corretor exigir ordens de proteção explícitas; caso contrário, deixe-os em zero para negociar sem metas rígidas.
4. Monitore os painéis do gráfico criados pela estratégia para visualizar velas, o indicador MFI e as negociações executadas.
