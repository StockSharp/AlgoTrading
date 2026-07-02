# Estratégia JK BullP AutoTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
JK BullP AutoTrader é um consultor especialista em acompanhamento de impulso originalmente escrito para MetaTrader 4. Ele monitora o Elder Bulls Power
indicador e reage quando a pressão de alta enfraquece ou fica negativa. A porta StockSharp mantém a lógica direta do
original, ao mesmo tempo que fornece parâmetros explícitos, gerenciamento detalhado de rastreamento e controles de risco amigáveis à plataforma.

## Lógica de negociação
1. A estratégia assina uma série de velas configuráveis (velas de 1 hora por padrão) e calcula um valor exponencial de 13 períodos.
média móvel (EMA) para replicar a linha de base do Bulls Power.
2. Para cada vela concluída, o Bulls Power é medido como a diferença entre a máxima da vela e o valor EMA.
3. Duas leituras consecutivas do Bulls Power são comparadas:
   - Se o valor anterior estiver acima do valor mais recente e o valor mais recente permanecer positivo, a estratégia abre uma posição curta.
   - Se o último valor do Bulls Power cair abaixo de zero, a estratégia abre uma posição longa.
4. Apenas uma posição pode estar ativa por vez, espelhando o especialista MQL original que bloqueou novas ordens enquanto as negociações estavam abertas.

## Gestão de riscos e saídas
- **Stop-loss/take-profit inicial:** As distâncias são configuradas em pips e convertidas em unidades de preço usando a etapa de preço do título.
Ambas as proteções são habilitadas por meio do auxiliar `StartProtection` do StockSharp, mantendo o comportamento próximo às entradas do MetaTrader.
- **Trailing stop:** Quando o lucro flutuante excede a distância final especificada, o nível de stop é movido vela por vela.
Em vez de modificar as ordens stop existentes (como em MetaTrader), a porta emite uma ordem de mercado para sair da posição quando o preço
fecha além do limite final. Isto garante saídas oportunas mesmo quando as ordens de proteção não são apoiadas pelo local.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `OrderVolume` | Tamanho da ordem de mercado usado para entradas. | 8,5 |
| `TakeProfitPips` | Distância de take-profit em pips (convertida em unidades de preço). | 500 |
| `StopLossPips` | Distância de stop-loss em pips. | 20 |
| `TrailingStopPips` | Distância de lucro em pips que ativa e mantém o trailing stop. | 10 |
| `EmaPeriod` | Comprimento do EMA usado pelo cálculo do Bulls Power. | 13 |
| `CandleType` | Tipo de dados das velas que orientam a estratégia (período padrão de 1 hora). | Velas de 1 hora |

## Notas de implementação
- As entradas não utilizadas (`Patr`, `Prange`, `Kstop`, `kts`, `Vts`) do script original foram omitidas intencionalmente porque tinham
nenhum efeito na lógica MetaTrader.
- As distâncias pip dependem do instrumento `PriceStep`. Se os dados da etapa não estiverem disponíveis, um valor de `1` será usado como padrão conservador.
- A estratégia usa StockSharp `Bind` API de alto nível, processa apenas velas concluídas e mantém o estado interno (`_previousBullsPower`)
para corresponder aos cálculos baseados em turnos MT4.
- A lógica móvel é redefinida automaticamente após cada saída para evitar níveis de parada obsoletos quando uma nova posição é aberta.
